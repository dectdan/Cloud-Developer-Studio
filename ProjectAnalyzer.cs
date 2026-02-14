using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// AI-powered analysis of project state and suggestions for next actions
    /// </summary>
    public class ProjectAnalyzer
    {
        private readonly string _projectPath;

        public ProjectAnalyzer(string projectPath)
        {
            _projectPath = projectPath;
        }

        /// <summary>
        /// Analyze project and generate recommendations
        /// </summary>
        public AnalysisResult Analyze()
        {
            var result = new AnalysisResult();

            try
            {
                // Load session state
                var sessionState = LoadSessionState();
                
                // Analyze uncertainties
                result.UrgentItems.AddRange(AnalyzeUncertainties());
                
                // Analyze recent patterns
                result.Recommendations.AddRange(AnalyzePatterns());
                
                // Analyze mistakes to avoid
                result.Warnings.AddRange(AnalyzeMistakes());
                
                // Analyze recent activity
                var activityInsights = AnalyzeActivity();
                result.ActivitySummary = activityInsights;
                
                // Suggest next steps based on context
                result.SuggestedNextSteps.AddRange(SuggestNextSteps(sessionState));
                
                // Calculate priority score
                result.PriorityScore = CalculatePriority(result);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        private dynamic? LoadSessionState()
        {
            var statePath = Path.Combine(_projectPath, "session_state.json");
            if (!File.Exists(statePath))
                return null;

            var json = File.ReadAllText(statePath);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        private List<string> AnalyzeUncertainties()
        {
            var items = new List<string>();
            var uncertaintiesPath = Path.Combine(_projectPath, "UNCERTAINTIES.md");
            
            if (!File.Exists(uncertaintiesPath))
                return items;

            var lines = File.ReadAllLines(uncertaintiesPath);
            var count = lines.Count(l => l.StartsWith("- [ ]"));
            
            if (count > 0)
            {
                items.Add($"Resolve {count} flagged uncertainties before proceeding");
            }

            return items;
        }

        private List<string> AnalyzePatterns()
        {
            var recommendations = new List<string>();
            var patternsPath = Path.Combine(_projectPath, "PATTERNS.jsonl");
            
            if (!File.Exists(patternsPath))
                return recommendations;

            var patterns = new List<dynamic>();
            foreach (var line in File.ReadLines(patternsPath))
            {
                try
                {
                    patterns.Add(JsonSerializer.Deserialize<JsonElement>(line));
                }
                catch { }
            }

            // Find high-confidence patterns
            var highConfidence = patterns
                .Where(p => p.GetProperty("confidence").GetInt32() >= 80)
                .ToList();

            if (highConfidence.Any())
            {
                recommendations.Add($"Apply {highConfidence.Count} proven patterns from past success");
            }

            return recommendations;
        }

        private List<string> AnalyzeMistakes()
        {
            var warnings = new List<string>();
            var mistakesPath = Path.Combine(_projectPath, "MISTAKES.jsonl");
            
            if (!File.Exists(mistakesPath))
                return warnings;

            var recentMistakes = new List<string>();
            foreach (var line in File.ReadLines(mistakesPath).TakeLast(10))
            {
                try
                {
                    var mistake = JsonSerializer.Deserialize<JsonElement>(line);
                    var lesson = mistake.GetProperty("lesson").GetString();
                    if (!string.IsNullOrEmpty(lesson))
                    {
                        recentMistakes.Add(lesson);
                    }
                }
                catch { }
            }

            if (recentMistakes.Any())
            {
                warnings.Add($"Remember {recentMistakes.Count} recent lessons learned");
            }

            return warnings;
        }

        private string AnalyzeActivity()
        {
            var activityDir = Path.Combine(_projectPath, "Activity");
            if (!Directory.Exists(activityDir))
                return "No recent activity";

            var todayFile = Path.Combine(activityDir, $"{DateTime.Now:yyyy-MM-dd}.jsonl");
            if (!File.Exists(todayFile))
                return "No activity today";

            var count = File.ReadLines(todayFile).Count();
            var lastLine = File.ReadLines(todayFile).LastOrDefault();
            
            if (string.IsNullOrEmpty(lastLine))
                return $"{count} activities today";

            try
            {
                var lastActivity = JsonSerializer.Deserialize<JsonElement>(lastLine);
                var action = lastActivity.GetProperty("action").GetString();
                var outcome = lastActivity.GetProperty("outcome").GetString();
                
                return $"{count} activities today. Last: {action} ({outcome})";
            }
            catch
            {
                return $"{count} activities today";
            }
        }

        private List<string> SuggestNextSteps(dynamic? sessionState)
        {
            var suggestions = new List<string>();

            if (sessionState == null)
            {
                suggestions.Add("Initialize project memory with 'claudedev init'");
                return suggestions;
            }

            try
            {
                var currentTask = sessionState.GetProperty("current_task").GetString();
                if (string.IsNullOrEmpty(currentTask))
                {
                    suggestions.Add("Define current task in session state");
                }
                else
                {
                    suggestions.Add($"Continue: {currentTask}");
                }

                // Check for pending decisions
                if (sessionState.TryGetProperty("decisions_pending", out JsonElement decisions))
                {
                    var count = decisions.GetArrayLength();
                    if (count > 0)
                    {
                        suggestions.Add($"Resolve {count} pending decisions");
                    }
                }

                // Check for blockers
                if (sessionState.TryGetProperty("blockers", out JsonElement blockers))
                {
                    var count = blockers.GetArrayLength();
                    if (count > 0)
                    {
                        suggestions.Add($"Address {count} blockers preventing progress");
                    }
                }
            }
            catch { }

            if (!suggestions.Any())
            {
                suggestions.Add("Review patterns and apply proven approaches");
            }

            return suggestions;
        }

        private int CalculatePriority(AnalysisResult result)
        {
            var score = 0;
            
            // Urgent items add high priority
            score += result.UrgentItems.Count * 10;
            
            // Warnings add medium priority
            score += result.Warnings.Count * 5;
            
            // Suggestions add low priority
            score += result.Recommendations.Count * 2;
            
            return Math.Min(score, 100);
        }
    }

    public class AnalysisResult
    {
        public List<string> UrgentItems { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> SuggestedNextSteps { get; set; } = new();
        public string ActivitySummary { get; set; } = string.Empty;
        public int PriorityScore { get; set; }
        public string? Error { get; set; }

        public bool HasUrgentItems => UrgentItems.Any();
        public bool HasRecommendations => Recommendations.Any();
        public bool HasWarnings => Warnings.Any();
    }
}
