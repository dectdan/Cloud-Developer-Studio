using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CdsVsBridge
{
    /// <summary>
    /// Writes VS state snapshots and error lists to JSON files in the CDS VSBridge directory.
    /// These files are read by the CDS MCP server to give Claude visibility into VS state.
    /// </summary>
    internal static class VsStateWriter
    {
        // Where all bridge files land — synced via OneDrive so CDS can read them
        public static readonly string BridgeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ClaudeDevStudio", "VSBridge");

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        static VsStateWriter()
        {
            Directory.CreateDirectory(BridgeDir);
        }

        // ── State snapshot ────────────────────────────────────────────────────

        public static void WriteState(VsStateSnapshot state)
        {
            try
            {
                var path = Path.Combine(BridgeDir, "vs_state.json");
                File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOpts));
            }
            catch { /* never crash VS */ }
        }

        // ── Error list ────────────────────────────────────────────────────────

        public static void WriteErrors(VsErrorSnapshot errors)
        {
            try
            {
                var path = Path.Combine(BridgeDir, "vs_errors.json");
                File.WriteAllText(path, JsonSerializer.Serialize(errors, JsonOpts));
            }
            catch { }
        }

        // ── Build output ──────────────────────────────────────────────────────

        public static void AppendBuildOutput(string text)
        {
            try
            {
                var path = Path.Combine(BridgeDir, "vs_build_output.txt");
                File.AppendAllText(path, text);
            }
            catch { }
        }

        public static void ClearBuildOutput()
        {
            try
            {
                var path = Path.Combine(BridgeDir, "vs_build_output.txt");
                File.WriteAllText(path, $"=== Build started {DateTime.UtcNow:O} ===\n");
            }
            catch { }
        }
    }

    // ── Data models ───────────────────────────────────────────────────────────

    internal class VsStateSnapshot
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

        [JsonPropertyName("solution")]
        public string? Solution { get; set; }

        [JsonPropertyName("activeFile")]
        public string? ActiveFile { get; set; }

        [JsonPropertyName("activeLine")]
        public int? ActiveLine { get; set; }

        [JsonPropertyName("debugMode")]
        public string DebugMode { get; set; } = "design";

        [JsonPropertyName("breakReason")]
        public string? BreakReason { get; set; }

        [JsonPropertyName("currentThread")]
        public string? CurrentThread { get; set; }

        [JsonPropertyName("exceptionMessage")]
        public string? ExceptionMessage { get; set; }
    }

    internal class VsErrorSnapshot
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

        [JsonPropertyName("buildResult")]
        public string BuildResult { get; set; } = "unknown";

        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        [JsonPropertyName("warningCount")]
        public int WarningCount { get; set; }

        [JsonPropertyName("errors")]
        public VsErrorItem[] Errors { get; set; } = Array.Empty<VsErrorItem>();

        [JsonPropertyName("warnings")]
        public VsErrorItem[] Warnings { get; set; } = Array.Empty<VsErrorItem>();
    }

    internal class VsErrorItem
    {
        [JsonPropertyName("file")]
        public string? File { get; set; }

        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("col")]
        public int Col { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "error";

        [JsonPropertyName("project")]
        public string? Project { get; set; }
    }
}
