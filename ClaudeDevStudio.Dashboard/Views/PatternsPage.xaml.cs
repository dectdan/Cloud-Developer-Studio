using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class PatternsPage : Page
    {
        public ObservableCollection<PatternDisplayItem> Patterns { get; } = new();

        public PatternsPage()
        {
            this.InitializeComponent();
            LoadPatterns();
        }

        private void LoadPatterns()
        {
            try
            {
                var projectsRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ClaudeDevStudio",
                    "Projects");

                if (!Directory.Exists(projectsRoot))
                {
                    Patterns.Add(new PatternDisplayItem 
                    { 
                        Pattern = "No projects found. Initialize a project to see patterns.",
                        Confidence = 0,
                        Type = "Info",
                        AppliesTo = "N/A"
                    });
                    return;
                }

                var firstProject = Directory.GetDirectories(projectsRoot).FirstOrDefault();
                if (firstProject == null)
                {
                    Patterns.Add(new PatternDisplayItem 
                    { 
                        Pattern = "No projects found.",
                        Confidence = 0,
                        Type = "Info",
                        AppliesTo = "N/A"
                    });
                    return;
                }

                var patternsPath = Path.Combine(firstProject, "PATTERNS.jsonl");
                if (!File.Exists(patternsPath))
                {
                    Patterns.Add(new PatternDisplayItem 
                    { 
                        Pattern = "No patterns discovered yet. As you work, patterns will be identified automatically.",
                        Confidence = 0,
                        Type = "Info",
                        AppliesTo = "Future work"
                    });
                    return;
                }

                foreach (var line in File.ReadLines(patternsPath))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        var isAntipattern = false;
                        if (root.TryGetProperty("isAntipattern", out var antipatternProp))
                        {
                            isAntipattern = antipatternProp.GetBoolean();
                        }

                        var appliesTo = "General";
                        if (root.TryGetProperty("appliesTo", out var appliesToProp))
                        {
                            var items = appliesToProp.EnumerateArray()
                                .Select(x => x.GetString())
                                .Where(x => !string.IsNullOrEmpty(x))
                                .ToList();
                            if (items.Any())
                            {
                                appliesTo = string.Join(", ", items);
                            }
                        }

                        var confidence = root.GetProperty("confidence").GetInt32();
                        var pattern = root.GetProperty("pattern").GetString() ?? "";

                        Patterns.Add(new PatternDisplayItem
                        {
                            Pattern = pattern,
                            Confidence = confidence,
                            Type = isAntipattern ? "❌ Anti-pattern" : "✅ Good pattern",
                            AppliesTo = appliesTo,
                            Icon = isAntipattern ? "\uE711" : "\uE73E",
                            IconColor = isAntipattern 
                                ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                                : new SolidColorBrush(Microsoft.UI.Colors.Green)
                        });
                    }
                    catch { }
                }

                if (Patterns.Count == 0)
                {
                    Patterns.Add(new PatternDisplayItem 
                    { 
                        Pattern = "No valid patterns found in file.",
                        Confidence = 0,
                        Type = "Info",
                        AppliesTo = "N/A"
                    });
                }
            }
            catch (Exception ex)
            {
                Patterns.Add(new PatternDisplayItem 
                { 
                    Pattern = $"Error loading patterns: {ex.Message}",
                    Confidence = 0,
                    Type = "Error",
                    AppliesTo = "N/A"
                });
            }
        }
    }

    public class PatternDisplayItem
    {
        public string Pattern { get; set; } = string.Empty;
        public int Confidence { get; set; }
        public string Type { get; set; } = string.Empty;
        public string AppliesTo { get; set; } = string.Empty;
        public string Icon { get; set; } = "\uE73E";
        public SolidColorBrush IconColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
}
