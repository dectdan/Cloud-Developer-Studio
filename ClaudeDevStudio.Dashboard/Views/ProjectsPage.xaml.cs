using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class ProjectsPage : Page
    {
        public ObservableCollection<ProjectInfo> Projects { get; } = new();

        public ProjectsPage()
        {
            this.InitializeComponent();
            LoadProjects();
        }

        private void LoadProjects()
        {
            try
            {
                var projectsRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "ClaudeDevStudio",
                    "Projects");

                if (!Directory.Exists(projectsRoot))
                {
                    Directory.CreateDirectory(projectsRoot);
                    return;
                }

                foreach (var projectDir in Directory.GetDirectories(projectsRoot))
                {
                    var sessionFile = Path.Combine(projectDir, "session_state.json");
                    if (File.Exists(sessionFile))
                    {
                        var projectName = Path.GetFileName(projectDir);
                        var lastWrite = File.GetLastWriteTime(sessionFile);
                        
                        Projects.Add(new ProjectInfo
                        {
                            Name = projectName,
                            Path = projectDir,
                            LastActivity = FormatTimeAgo(lastWrite)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: Show error message
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
            }
        }

        private string FormatTimeAgo(DateTime time)
        {
            var span = DateTime.Now - time;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return time.ToString("MMM d, yyyy");
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
        }

        private void ViewMemory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path)
            {
                // Navigate to memory page
                var mainWindow = (Application.Current as App)?.m_window as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.NavigateToMemory(path);
                }
            }
        }
    }

    public class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string LastActivity { get; set; } = string.Empty;
    }
}
