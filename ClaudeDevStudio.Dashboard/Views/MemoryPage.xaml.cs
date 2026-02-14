using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class MemoryPage : Page
    {
        public ObservableCollection<PatternItem> Patterns { get; } = new();
        public ObservableCollection<MistakeItem> Mistakes { get; } = new();
        private string? _projectPath;

        public MemoryPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // If a specific project path was passed, use it
            if (e.Parameter is string projectPath)
            {
                _projectPath = projectPath;
            }
            
            LoadMemoryData();
        }

        private void LoadMemoryData()
        {
            try
            {
                string? targetProject = _projectPath;

                // If no specific project was passed, use first project
                if (string.IsNullOrEmpty(targetProject))
                {
                    var projectsRoot = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "ClaudeDevStudio",
                        "Projects");

                    if (!Directory.Exists(projectsRoot))
                    {
                        FactsText.Text = "No projects found. Initialize a project to see memory data.";
                        return;
                    }

                    targetProject = Directory.GetDirectories(projectsRoot).FirstOrDefault();
                    if (targetProject == null)
                    {
                        FactsText.Text = "No projects found.";
                        return;
                    }
                }

                LoadFacts(targetProject);
                LoadPatterns(targetProject);
                LoadMistakes(targetProject);
            }
            catch (Exception ex)
            {
                FactsText.Text = $"Error loading memory: {ex.Message}";
            }
        }

        private void LoadFacts(string projectPath)
        {
            var factsPath = Path.Combine(projectPath, "FACTS.md");
            if (File.Exists(factsPath))
            {
                var facts = File.ReadAllText(factsPath);
                FactsText.Text = string.IsNullOrWhiteSpace(facts) 
                    ? "No facts recorded yet." 
                    : facts;
            }
            else
            {
                FactsText.Text = "No facts file found.";
            }
        }

        private void LoadPatterns(string projectPath)
        {
            var patternsPath = Path.Combine(projectPath, "PATTERNS.jsonl");
            if (!File.Exists(patternsPath))
            {
                Patterns.Add(new PatternItem 
                { 
                    Pattern = "No patterns discovered yet. As you work, patterns will be identified automatically.",
                    Confidence = 0 
                });
                return;
            }

            foreach (var line in File.ReadLines(patternsPath).TakeLast(10))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    Patterns.Add(new PatternItem
                    {
                        Pattern = root.GetProperty("pattern").GetString() ?? "",
                        Confidence = root.GetProperty("confidence").GetInt32()
                    });
                }
                catch { }
            }

            if (Patterns.Count == 0)
            {
                Patterns.Add(new PatternItem 
                { 
                    Pattern = "No patterns found in file.",
                    Confidence = 0 
                });
            }
        }

        private void LoadMistakes(string projectPath)
        {
            var mistakesPath = Path.Combine(projectPath, "MISTAKES.jsonl");
            if (!File.Exists(mistakesPath))
            {
                Mistakes.Add(new MistakeItem 
                { 
                    Mistake = "No mistakes recorded yet.",
                    Lesson = "As development continues, lessons learned will appear here."
                });
                return;
            }

            foreach (var line in File.ReadLines(mistakesPath).TakeLast(10))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    Mistakes.Add(new MistakeItem
                    {
                        Mistake = root.GetProperty("mistake").GetString() ?? "",
                        Lesson = root.GetProperty("lesson").GetString() ?? ""
                    });
                }
                catch { }
            }

            if (Mistakes.Count == 0)
            {
                Mistakes.Add(new MistakeItem 
                { 
                    Mistake = "No mistakes found in file.",
                    Lesson = ""
                });
            }
        }
    }

    public class PatternItem
    {
        public string Pattern { get; set; } = string.Empty;
        public int Confidence { get; set; }
    }

    public class MistakeItem
    {
        public string Mistake { get; set; } = string.Empty;
        public string Lesson { get; set; } = string.Empty;
    }
}
