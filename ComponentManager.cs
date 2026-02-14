using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Manages restarting ClaudeDevStudio components without system reboot
    /// </summary>
    public static class ComponentManager
    {
        /// <summary>
        /// Restart the tray application
        /// </summary>
        public static bool RestartTrayApp()
        {
            try
            {
                // Find running tray app
                var trayProcess = Process.GetProcessesByName("ClaudeDevStudio.TrayApp").FirstOrDefault();
                
                // Get path before killing
                var exePath = trayProcess?.MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    // Try default location
                    exePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "ClaudeDevStudio.TrayApp.exe");
                }

                // Kill existing instance
                if (trayProcess != null)
                {
                    trayProcess.Kill();
                    trayProcess.WaitForExit(3000);
                }

                // Start new instance
                if (File.Exists(exePath))
                {
                    Process.Start(exePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restart the dashboard (if running)
        /// </summary>
        public static bool RestartDashboard()
        {
            try
            {
                var dashboardProcess = Process.GetProcessesByName("ClaudeDevStudio.Dashboard").FirstOrDefault();
                
                var exePath = dashboardProcess?.MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "ClaudeDevStudio.Dashboard.exe");
                }

                if (dashboardProcess != null)
                {
                    dashboardProcess.Kill();
                    dashboardProcess.WaitForExit(3000);
                }

                if (File.Exists(exePath))
                {
                    Process.Start(exePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restart MCP server (Node.js)
        /// </summary>
        public static bool RestartMcpServer()
        {
            try
            {
                // Kill existing Node.js MCP server
                var nodeProcesses = Process.GetProcessesByName("node")
                    .Where(p => p.MainModule?.FileName?.Contains("claudedevstudio") == true);

                foreach (var proc in nodeProcesses)
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                }

                // Start new instance (tray app will handle this)
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restart all ClaudeDevStudio components
        /// </summary>
        public static void RestartAll()
        {
            RestartMcpServer();
            RestartDashboard();
            RestartTrayApp(); // Do this last since it manages the others
        }

        /// <summary>
        /// Check if tray app is running
        /// </summary>
        public static bool IsTrayAppRunning()
        {
            return Process.GetProcessesByName("ClaudeDevStudio.TrayApp").Any();
        }

        /// <summary>
        /// Check if dashboard is running
        /// </summary>
        public static bool IsDashboardRunning()
        {
            return Process.GetProcessesByName("ClaudeDevStudio.Dashboard").Any();
        }

        /// <summary>
        /// Get status of all components
        /// </summary>
        public static ComponentStatus GetStatus()
        {
            return new ComponentStatus
            {
                TrayAppRunning = IsTrayAppRunning(),
                DashboardRunning = IsDashboardRunning(),
                McpServerRunning = Process.GetProcessesByName("node").Any()
            };
        }
    }

    public class ComponentStatus
    {
        public bool TrayAppRunning { get; set; }
        public bool DashboardRunning { get; set; }
        public bool McpServerRunning { get; set; }

        public bool AllRunning => TrayAppRunning && DashboardRunning && McpServerRunning;
    }
}
