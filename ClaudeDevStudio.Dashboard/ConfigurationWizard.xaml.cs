using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ClaudeDevStudio.Dashboard
{
    public sealed partial class ConfigurationWizard : Window
    {
        private int _currentPage = 0;
        private string _projectsFolder = string.Empty;

        public ConfigurationWizard()
        {
            this.InitializeComponent();
            
            // Set window size
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 600, Height = 500 });

            Title = "ClaudeDevStudio Setup";
            ShowPage(0);
        }

        private void ShowPage(int page)
        {
            _currentPage = page;

            // Clear content
            var stack = new StackPanel { Padding = new Thickness(40), Spacing = 24 };

            switch (page)
            {
                case 0:
                    ShowWelcomePage(stack);
                    break;
                case 1:
                    ShowProjectsFolderPage(stack);
                    break;
                case 2:
                    ShowPermissionsPage(stack);
                    break;
                case 3:
                    ShowCompletePage(stack);
                    break;
            }

            Content = stack;
        }

        private void ShowWelcomePage(StackPanel stack)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Welcome to ClaudeDevStudio!",
                Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = "ClaudeDevStudio provides persistent memory and development tools for Claude AI. " +
                       "This wizard will help you get started.",
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Features:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0,24,0,0)
            });

            stack.Children.Add(new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    CreateBullet("Persistent memory across sessions"),
                    CreateBullet("Approval system for user control"),
                    CreateBullet("Activity tracking and logging"),
                    CreateBullet("Build automation and monitoring")
                }
            });

            var nextButton = new Button
            {
                Content = "Get Started",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0,24,0,0)
            };
            nextButton.Click += (s, e) => ShowPage(1);
            stack.Children.Add(nextButton);
        }

        private void ShowProjectsFolderPage(StackPanel stack)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Choose Projects Folder",
                Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = "ClaudeDevStudio will store memory and activity data for your projects.",
                TextWrapping = TextWrapping.Wrap
            });

            _projectsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClaudeDevStudio",
                "Projects");

            var pathBox = new TextBox
            {
                Text = _projectsFolder,
                IsReadOnly = true,
                Margin = new Thickness(0,16,0,0)
            };

            var browseButton = new Button
            {
                Content = "Browse...",
                Margin = new Thickness(0,8,0,0)
            };
            browseButton.Click += async (s, e) =>
            {
                var picker = new FolderPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    _projectsFolder = folder.Path;
                    pathBox.Text = _projectsFolder;
                }
            };

            stack.Children.Add(pathBox);
            stack.Children.Add(browseButton);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0,32,0,0)
            };

            var backButton = new Button { Content = "Back" };
            backButton.Click += (s, e) => ShowPage(0);
            buttonsPanel.Children.Add(backButton);

            var nextButton = new Button
            {
                Content = "Next",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"]
            };
            nextButton.Click += (s, e) => ShowPage(2);
            buttonsPanel.Children.Add(nextButton);

            stack.Children.Add(buttonsPanel);
        }

        private void ShowPermissionsPage(StackPanel stack)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Permission Settings",
                Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Choose which actions require your approval:",
                TextWrapping = TextWrapping.Wrap
            });

            var options = new StackPanel { Spacing = 12, Margin = new Thickness(0,16,0,0) };

            options.Children.Add(new CheckBox
            {
                Content = "File deletions (Recommended)",
                IsChecked = true
            });

            options.Children.Add(new CheckBox
            {
                Content = "Build commands",
                IsChecked = false
            });

            options.Children.Add(new CheckBox
            {
                Content = "System changes",
                IsChecked = true
            });

            stack.Children.Add(options);

            stack.Children.Add(new TextBlock
            {
                Text = "You can change these settings later in the dashboard.",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0,16,0,0)
            });

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0,32,0,0)
            };

            var backButton = new Button { Content = "Back" };
            backButton.Click += (s, e) => ShowPage(1);
            buttonsPanel.Children.Add(backButton);

            var finishButton = new Button
            {
                Content = "Finish",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"]
            };
            finishButton.Click += (s, e) =>
            {
                SaveConfiguration();
                ShowPage(3);
            };
            buttonsPanel.Children.Add(finishButton);

            stack.Children.Add(buttonsPanel);
        }

        private void ShowCompletePage(StackPanel stack)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Setup Complete!",
                Style = (Style)Application.Current.Resources["TitleTextBlockStyle"]
            });

            stack.Children.Add(new TextBlock
            {
                Text = "ClaudeDevStudio is ready to use. The application will start with Windows " +
                       "and run in the system tray.",
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new InfoBar
            {
                Title = "Next Steps",
                Message = "Click the tray icon to open the dashboard and view your projects.",
                Severity = InfoBarSeverity.Success,
                IsOpen = true,
                IsClosable = false,
                Margin = new Thickness(0,16,0,0)
            });

            var closeButton = new Button
            {
                Content = "Close",
                Style = (Style)Application.Current.Resources["AccentButtonStyle"],
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0,24,0,0)
            };
            closeButton.Click += (s, e) => this.Close();
            stack.Children.Add(closeButton);
        }

        private TextBlock CreateBullet(string text)
        {
            return new TextBlock
            {
                Text = "• " + text
            };
        }

        private void SaveConfiguration()
        {
            var config = new
            {
                projectsFolder = _projectsFolder,
                firstRunComplete = true,
                approvalSettings = new
                {
                    requireForDeletions = true,
                    requireForBuilds = false,
                    requireForSystemChanges = true
                }
            };

            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClaudeDevStudio",
                "settings.json");

            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            // Create projects directory
            Directory.CreateDirectory(_projectsFolder);
        }
    }
}
