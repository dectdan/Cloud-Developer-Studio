using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace ClaudeDevStudio.Dashboard.Views
{
    public sealed partial class ApprovalsPage : Page
    {
        public ObservableCollection<ApprovalRequest> PendingApprovals { get; } = new();

        public ApprovalsPage()
        {
            this.InitializeComponent();
            LoadPendingApprovals();
            UpdateEmptyState();
        }

        private void LoadPendingApprovals()
        {
            // TODO: Load from approval queue file
            // For now, add a sample for testing
            if (System.Diagnostics.Debugger.IsAttached)
            {
                PendingApprovals.Add(new ApprovalRequest
                {
                    Id = "test_1",
                    Action = "Delete 3 files from temporary directory",
                    Details = "Files: temp1.txt, temp2.log, cache.dat\nReason: Cleanup old build artifacts",
                    RiskLevel = "⚠️ Medium Risk - File deletion"
                });
            }
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = PendingApprovals.Count == 0 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private async void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string approvalId)
            {
                var approval = PendingApprovals.FirstOrDefault(a => a.Id == approvalId);
                if (approval != null)
                {
                    // TODO: Send approval to approval system
                    await ApprovalSystem.ApproveAsync(approvalId);
                    
                    PendingApprovals.Remove(approval);
                    UpdateEmptyState();

                    // Show success notification
                    var infoBar = new InfoBar
                    {
                        Title = "Action Approved",
                        Message = "Claude can now proceed with this action.",
                        Severity = InfoBarSeverity.Success,
                        IsOpen = true,
                        IsClosable = true
                    };
                    ApprovalsContainer.Children.Insert(0, infoBar);
                }
            }
        }

        private async void Deny_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string approvalId)
            {
                var approval = PendingApprovals.FirstOrDefault(a => a.Id == approvalId);
                if (approval != null)
                {
                    // TODO: Send denial to approval system
                    await ApprovalSystem.DenyAsync(approvalId);
                    
                    PendingApprovals.Remove(approval);
                    UpdateEmptyState();

                    // Show info notification
                    var infoBar = new InfoBar
                    {
                        Title = "Action Denied",
                        Message = "Claude will not proceed with this action.",
                        Severity = InfoBarSeverity.Informational,
                        IsOpen = true,
                        IsClosable = true
                    };
                    ApprovalsContainer.Children.Insert(0, infoBar);
                }
            }
        }
    }

    public class ApprovalRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string Timestamp { get; set; } = System.DateTime.Now.ToString("HH:mm:ss");
    }

    // Placeholder for approval system integration
    public static class ApprovalSystem
    {
        public static async System.Threading.Tasks.Task ApproveAsync(string id)
        {
            // TODO: Implement approval logic
            await System.Threading.Tasks.Task.Delay(100);
        }

        public static async System.Threading.Tasks.Task DenyAsync(string id)
        {
            // TODO: Implement denial logic
            await System.Threading.Tasks.Task.Delay(100);
        }
    }
}
