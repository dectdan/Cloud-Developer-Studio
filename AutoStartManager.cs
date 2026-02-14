using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Manages Windows startup integration
    /// </summary>
    public static class AutoStartManager
    {
        private const string AppName = "ClaudeDevStudio";
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Check if auto-start is enabled
        /// </summary>
        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                var value = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enable auto-start on Windows login
        /// </summary>
        public static bool Enable()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                if (exePath.EndsWith(".dll"))
                {
                    // Running as dotnet app, get the exe
                    exePath = Path.ChangeExtension(exePath, ".exe");
                }

                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to enable auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disable auto-start
        /// </summary>
        public static bool Disable()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
                key?.DeleteValue(AppName, false);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to disable auto-start: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Toggle auto-start on/off
        /// </summary>
        public static bool Toggle()
        {
            return IsEnabled() ? Disable() : Enable();
        }
    }
}
