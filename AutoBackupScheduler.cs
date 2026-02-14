using System;
using System.Threading;
using System.Threading.Tasks;
using ClaudeDevStudio.Memory;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Automatically triggers backups based on activity and time
    /// Runs in background and ensures data is always protected
    /// </summary>
    public class AutoBackupScheduler
    {
        private readonly string _projectPath;
        private readonly BackupManager _backupManager;
        private readonly ClaudeMemory _memory;
        private Timer? _dailyTimer;
        private Timer? _weeklyTimer;
        private int _activityCountSinceBackup;
        private readonly int _activityThreshold = 50;

        public AutoBackupScheduler(string projectPath)
        {
            _projectPath = projectPath;
            _backupManager = new BackupManager(projectPath);
            _memory = new ClaudeMemory(projectPath);
        }

        /// <summary>
        /// Start automatic backup scheduling
        /// </summary>
        public void Start()
        {
            // Daily backup at 2 AM
            var now = DateTime.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0);
            if (nextRun < now)
                nextRun = nextRun.AddDays(1);

            var timeUntilDaily = nextRun - now;
            _dailyTimer = new Timer(
                DailyBackupCallback,
                null,
                timeUntilDaily,
                TimeSpan.FromDays(1));

            // Weekly backup on Sunday at 3 AM
            var nextSunday = now;
            while (nextSunday.DayOfWeek != DayOfWeek.Sunday)
                nextSunday = nextSunday.AddDays(1);
            
            nextSunday = new DateTime(nextSunday.Year, nextSunday.Month, nextSunday.Day, 3, 0, 0);
            if (nextSunday < now)
                nextSunday = nextSunday.AddDays(7);

            var timeUntilWeekly = nextSunday - now;
            _weeklyTimer = new Timer(
                WeeklyBackupCallback,
                null,
                timeUntilWeekly,
                TimeSpan.FromDays(7));
        }

        /// <summary>
        /// Stop automatic backup scheduling
        /// </summary>
        public void Stop()
        {
            _dailyTimer?.Dispose();
            _weeklyTimer?.Dispose();
        }

        /// <summary>
        /// Record activity and check if threshold backup is needed
        /// Call this after each significant activity
        /// </summary>
        public void OnActivity()
        {
            _activityCountSinceBackup++;

            if (_activityCountSinceBackup >= _activityThreshold)
            {
                Task.Run(() => TriggerBackup(BackupTrigger.ActivityThreshold));
                _activityCountSinceBackup = 0;
            }
        }

        /// <summary>
        /// Trigger backup before session handoff
        /// </summary>
        public void OnHandoff()
        {
            Task.Run(() => TriggerBackup(BackupTrigger.PreHandoff));
        }

        private void DailyBackupCallback(object? state)
        {
            TriggerBackup(BackupTrigger.Daily);
        }

        private void WeeklyBackupCallback(object? state)
        {
            TriggerBackup(BackupTrigger.Weekly);
        }

        private void TriggerBackup(BackupTrigger trigger)
        {
            try
            {
                var result = _backupManager.CreateBackup(trigger);

                if (result.Success && !result.Skipped)
                {
                    // Log backup success
                    _memory.RecordActivity(new Activity
                    {
                        Action = "auto_backup",
                        Description = $"Automatic backup triggered by {trigger}",
                        Outcome = "success"
                    });

                    // Reset activity counter
                    if (trigger == BackupTrigger.ActivityThreshold)
                    {
                        _activityCountSinceBackup = 0;
                    }
                }
            }
            catch
            {
                // Silently fail - don't interrupt user work
            }
        }
    }
}
