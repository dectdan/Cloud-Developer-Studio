using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class ActivityPage : Page
    {
        public ObservableCollection<ActivityItem> Activities { get; } = new();

        public ActivityPage()
        {
            this.InitializeComponent();
            LoadRecentActivities();
        }

        private void LoadRecentActivities()
        {
            try
            {
                var projectsRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ClaudeDevStudio",
                    "Projects");

                if (!Directory.Exists(projectsRoot))
                    return;

                // Load activities from all projects
                foreach (var projectDir in Directory.GetDirectories(projectsRoot))
                {
                    var activityDir = Path.Combine(projectDir, "Activity");
                    if (!Directory.Exists(activityDir))
                        continue;

                    // Get today's activity file
                    var todayFile = Path.Combine(activityDir, $"{DateTime.Now:yyyy-MM-dd}.jsonl");
                    if (File.Exists(todayFile))
                    {
                        LoadActivityFile(todayFile, Path.GetFileName(projectDir));
                    }
                }

                // Sort by time, most recent first
                var sorted = Activities.OrderByDescending(a => a.Timestamp).Take(50).ToList();
                Activities.Clear();
                foreach (var item in sorted)
                {
                    Activities.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading activities: {ex.Message}");
            }
        }

        private void LoadActivityFile(string filePath, string projectName)
        {
            foreach (var line in File.ReadLines(filePath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    var activity = new ActivityItem
                    {
                        Description = root.GetProperty("action").GetString() ?? "Unknown",
                        Details = $"{projectName}: {root.GetProperty("description").GetString()}",
                        Time = FormatTime(root.GetProperty("timestamp").GetDateTime()),
                        Timestamp = root.GetProperty("timestamp").GetDateTime()
                    };

                    // Set icon based on action type
                    var action = activity.Description.ToLower();
                    if (action.Contains("build"))
                        activity.Icon = "\uE8E1"; // Build icon
                    else if (action.Contains("exception"))
                    {
                        activity.Icon = "\uE783"; // Error icon
                        activity.IconColor = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    }
                    else if (action.Contains("file"))
                        activity.Icon = "\uE8A5"; // Document icon
                    else
                        activity.Icon = "\uE8B4"; // Activity icon

                    Activities.Add(activity);
                }
                catch
                {
                    continue;
                }
            }
        }

        private string FormatTime(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return time.ToString("HH:mm");
        }
    }

    public class ActivityItem
    {
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Icon { get; set; } = "\uE8B4";
        public SolidColorBrush IconColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        public DateTime Timestamp { get; set; }
    }
}
