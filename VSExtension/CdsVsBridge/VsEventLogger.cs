using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CdsVsBridge
{
    /// <summary>
    /// Appends structured events to vs_events.jsonl — one JSON object per line.
    /// CDS tails this file to know what happened without polling.
    /// </summary>
    internal static class VsEventLogger
    {
        private static readonly object FileLock = new object();

        private static string EventsPath =>
            Path.Combine(VsStateWriter.BridgeDir, "vs_events.jsonl");

        public static void Log(string eventName, object? extra = null)
        {
            try
            {
                var entry = new VsEvent
                {
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    Event = eventName,
                    Extra = extra
                };

                var line = JsonSerializer.Serialize(entry,
                    new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }) + "\n";

                lock (FileLock)
                {
                    File.AppendAllText(EventsPath, line);

                    // Keep file from growing unbounded — trim to last 500 lines
                    TrimIfNeeded();
                }
            }
            catch { /* never crash VS */ }
        }

        private static void TrimIfNeeded()
        {
            try
            {
                var lines = File.ReadAllLines(EventsPath);
                if (lines.Length > 500)
                {
                    // Keep last 400 lines with some headroom
                    var trimmed = new string[400];
                    Array.Copy(lines, lines.Length - 400, trimmed, 0, 400);
                    File.WriteAllLines(EventsPath, trimmed);
                }
            }
            catch { }
        }
    }

    internal class VsEvent
    {
        [JsonPropertyName("ts")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

        [JsonPropertyName("event")]
        public string Event { get; set; } = "";

        [JsonPropertyName("extra")]
        public object? Extra { get; set; }
    }
}
