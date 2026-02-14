using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class MistakesPage : Page
    {
        public ObservableCollection<MistakeDisplayItem> Mistakes { get; } = new();

        public MistakesPage()
        {
            this.InitializeComponent();
            LoadMistakes();
        }

        private void LoadMistakes()
        {
            try
            {
                var projectsRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ClaudeDevStudio",
                    "Projects");

                if (!Directory.Exists(projectsRoot))
                {
                    Mistakes.Add(new MistakeDisplayItem 
                    { 
                        Mistake = "No projects found. Initialize a project to track mistakes.",
                        Fix = "Run: claudedev init <project_path>",
                        Lesson = "ClaudeDevStudio learns from every mistake to avoid repeating them.",
                        Severity = "info"
                    });
                    return;
                }

                var firstProject = Directory.GetDirectories(projectsRoot).FirstOrDefault();
                if (firstProject == null)
                {
                    Mistakes.Add(new MistakeDisplayItem 
                    { 
                        Mistake = "No projects found.",
                        Fix = "Initialize a project first.",
                        Lesson = "Start using ClaudeDevStudio to build knowledge over time.",
                        Severity = "info"
                    });
                    return;
                }

                var mistakesPath = Path.Combine(firstProject, "MISTAKES.jsonl");
                if (!File.Exists(mistakesPath))
                {
                    Mistakes.Add(new MistakeDisplayItem 
                    { 
                        Mistake = "No mistakes recorded yet.",
                        Fix = "As development continues, mistakes and their fixes will be recorded here.",
                        Lesson = "Learning from mistakes is key to improvement.",
                        Severity = "info"
                    });
                    return;
                }

                foreach (var line in File.ReadLines(mistakesPath).Reverse().Take(20))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;

                        var severity = "medium";
                        if (root.TryGetProperty("severity", out var severityProp))
                        {
                            severity = severityProp.GetString() ?? "medium";
                        }

                        Mistakes.Add(new MistakeDisplayItem
                        {
                            Mistake = root.GetProperty("mistake").GetString() ?? "",
                            Fix = root.GetProperty("fix").GetString() ?? "",
                            Lesson = root.GetProperty("lesson").GetString() ?? "",
                            Severity = severity
                        });
                    }
                    catch { }
                }

                if (Mistakes.Count == 0)
                {
                    Mistakes.Add(new MistakeDisplayItem 
                    { 
                        Mistake = "No valid mistakes found in file.",
                        Fix = "File may be corrupted or empty.",
                        Lesson = "Ensure mistakes are recorded in proper JSON format.",
                        Severity = "info"
                    });
                }
            }
            catch (Exception ex)
            {
                Mistakes.Add(new MistakeDisplayItem 
                { 
                    Mistake = $"Error loading mistakes: {ex.Message}",
                    Fix = "Check file permissions and format.",
                    Lesson = "Always handle errors gracefully.",
                    Severity = "high"
                });
            }
        }
    }

    public class MistakeDisplayItem
    {
        public string Mistake { get; set; } = string.Empty;
        public string Fix { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
        public string Severity { get; set; } = "medium";

        public string SeverityText => Severity.ToUpper() switch
        {
            "LOW" => "LOW SEVERITY",
            "MEDIUM" => "MEDIUM SEVERITY",
            "HIGH" => "HIGH SEVERITY",
            "CRITICAL" => "CRITICAL",
            _ => "INFO"
        };

        public SolidColorBrush SeverityColor => Severity.ToLower() switch
        {
            "low" => new SolidColorBrush(Microsoft.UI.Colors.Orange),
            "medium" => new SolidColorBrush(Microsoft.UI.Colors.OrangeRed),
            "high" => new SolidColorBrush(Microsoft.UI.Colors.Red),
            "critical" => new SolidColorBrush(Microsoft.UI.Colors.DarkRed),
            _ => new SolidColorBrush(Microsoft.UI.Colors.Gray)
        };

        public SolidColorBrush BorderColor => SeverityColor;
    }
}
