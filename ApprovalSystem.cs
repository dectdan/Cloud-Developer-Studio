using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Manages approval requests between Claude and the user
    /// Thread-safe, file-based queue system
    /// </summary>
    public class ApprovalSystem
    {
        private static readonly string ApprovalsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeDevStudio",
            "approvals");

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> PendingRequests = new();

        static ApprovalSystem()
        {
            Directory.CreateDirectory(ApprovalsPath);
        }

        /// <summary>
        /// Request approval from user (called by Claude/MCP)
        /// Blocks until user approves/denies
        /// </summary>
        public static async Task<bool> RequestApprovalAsync(ApprovalRequest request)
        {
            request.Id = Guid.NewGuid().ToString();
            request.Timestamp = DateTime.Now;
            request.Status = ApprovalStatus.Pending;

            // Save to queue file
            var queueFile = Path.Combine(ApprovalsPath, "pending_queue.jsonl");
            var json = JsonSerializer.Serialize(request);
            await File.AppendAllTextAsync(queueFile, json + Environment.NewLine);

            // Create completion source
            var tcs = new TaskCompletionSource<bool>();
            PendingRequests[request.Id] = tcs;

            // Notify dashboard (via file watcher or SignalR)
            NotifyDashboard();

            // Wait for user response (with 5 minute timeout)
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout - default to deny for safety
                PendingRequests.TryRemove(request.Id, out _);
                await RecordDecision(request.Id, false, "Timeout - no response");
                return false;
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Approve a request (called by Dashboard)
        /// </summary>
        public static async Task ApproveAsync(string approvalId)
        {
            if (PendingRequests.TryRemove(approvalId, out var tcs))
            {
                tcs.SetResult(true);
                await RecordDecision(approvalId, true, "User approved");
                await RemoveFromQueue(approvalId);
            }
        }

        /// <summary>
        /// Deny a request (called by Dashboard)
        /// </summary>
        public static async Task DenyAsync(string approvalId, string reason = "User denied")
        {
            if (PendingRequests.TryRemove(approvalId, out var tcs))
            {
                tcs.SetResult(false);
                await RecordDecision(approvalId, false, reason);
                await RemoveFromQueue(approvalId);
            }
        }

        /// <summary>
        /// Get all pending approvals (called by Dashboard)
        /// </summary>
        public static async Task<ApprovalRequest[]> GetPendingApprovalsAsync()
        {
            var queueFile = Path.Combine(ApprovalsPath, "pending_queue.jsonl");
            if (!File.Exists(queueFile))
                return Array.Empty<ApprovalRequest>();

            var requests = new System.Collections.Generic.List<ApprovalRequest>();
            foreach (var line in await File.ReadAllLinesAsync(queueFile))
            {
                try
                {
                    var request = JsonSerializer.Deserialize<ApprovalRequest>(line);
                    if (request != null && request.Status == ApprovalStatus.Pending)
                    {
                        requests.Add(request);
                    }
                }
                catch { }
            }

            return requests.ToArray();
        }

        private static void NotifyDashboard()
        {
            // Create notification file that dashboard watches
            var notifyFile = Path.Combine(ApprovalsPath, "notify.txt");
            File.WriteAllText(notifyFile, DateTime.Now.Ticks.ToString());
        }

        private static async Task RecordDecision(string id, bool approved, string reason)
        {
            var historyFile = Path.Combine(ApprovalsPath, "approval_history.jsonl");
            var record = new
            {
                id,
                approved,
                reason,
                timestamp = DateTime.Now
            };
            var json = JsonSerializer.Serialize(record);
            await File.AppendAllTextAsync(historyFile, json + Environment.NewLine);
        }

        private static async Task RemoveFromQueue(string approvalId)
        {
            var queueFile = Path.Combine(ApprovalsPath, "pending_queue.jsonl");
            if (!File.Exists(queueFile))
                return;

            var lines = await File.ReadAllLinesAsync(queueFile);
            var filtered = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                try
                {
                    var request = JsonSerializer.Deserialize<ApprovalRequest>(line);
                    if (request != null && request.Id != approvalId)
                    {
                        filtered.Add(line);
                    }
                }
                catch { }
            }

            await File.WriteAllLinesAsync(queueFile, filtered);
        }
    }

    public class ApprovalRequest
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public ApprovalStatus Status { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public RiskLevel Risk { get; set; }
        public string ProjectPath { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = "Claude";
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Denied,
        Timeout
    }

    public enum RiskLevel
    {
        Low,      // Read operations
        Medium,   // Write/create operations
        High,     // Delete/modify operations
        Critical  // System changes, installations
    }
}
