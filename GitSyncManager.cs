using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Manages Git-based backup and sync for ClaudeDevStudio memory
    /// Enables version control and cross-machine sync via private repos
    /// </summary>
    public class GitSyncManager
    {
        private readonly string _projectPath;
        private readonly string _gitRepoPath;

        public GitSyncManager(string projectPath)
        {
            _projectPath = projectPath;
            _gitRepoPath = GetGitRepoPath(projectPath);
        }

        /// <summary>
        /// Initialize Git repository for project memory
        /// </summary>
        public GitResult InitializeRepo(string remoteUrl = null)
        {
            try
            {
                if (IsGitRepo())
                {
                    return new GitResult
                    {
                        Success = true,
                        Message = "Git repository already initialized",
                        AlreadyExists = true
                    };
                }

                // Create .cds-memory directory
                Directory.CreateDirectory(_gitRepoPath);

                // Initialize git
                RunGitCommand("init");

                // Create .gitignore
                CreateGitIgnore();

                // Create initial commit
                RunGitCommand("add .");
                RunGitCommand("commit -m \"Initial ClaudeDevStudio memory\"");

                // Add remote if provided
                if (!string.IsNullOrEmpty(remoteUrl))
                {
                    RunGitCommand($"remote add origin {remoteUrl}");
                }

                return new GitResult
                {
                    Success = true,
                    Message = "Git repository initialized successfully"
                };
            }
            catch (Exception ex)
            {
                return new GitResult
                {
                    Success = false,
                    Message = $"Failed to initialize Git repo: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Commit current memory state to Git
        /// </summary>
        public GitResult CommitChanges(string message = null)
        {
            try
            {
                if (!IsGitRepo())
                {
                    return new GitResult
                    {
                        Success = false,
                        Message = "Git repository not initialized. Run 'claudedev git init' first."
                    };
                }

                // Copy memory files to git repo
                SyncFilesToRepo();

                // Check if there are changes
                var statusOutput = RunGitCommand("status --porcelain");
                if (string.IsNullOrWhiteSpace(statusOutput))
                {
                    return new GitResult
                    {
                        Success = true,
                        Message = "No changes to commit",
                        NoChanges = true
                    };
                }

                // Stage all changes
                RunGitCommand("add .");

                // Create commit message
                var commitMessage = message ?? $"Auto-backup: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                RunGitCommand($"commit -m \"{commitMessage}\"");

                // Get commit hash
                var commitHash = RunGitCommand("rev-parse HEAD").Trim();

                return new GitResult
                {
                    Success = true,
                    Message = $"Changes committed: {commitHash.Substring(0, 7)}",
                    CommitHash = commitHash
                };
            }
            catch (Exception ex)
            {
                return new GitResult
                {
                    Success = false,
                    Message = $"Commit failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Push to remote repository
        /// </summary>
        public GitResult Push(string branch = "main")
        {
            try
            {
                if (!IsGitRepo())
                {
                    return new GitResult
                    {
                        Success = false,
                        Message = "Git repository not initialized"
                    };
                }

                var output = RunGitCommand($"push origin {branch}");

                return new GitResult
                {
                    Success = true,
                    Message = "Pushed to remote successfully"
                };
            }
            catch (Exception ex)
            {
                return new GitResult
                {
                    Success = false,
                    Message = $"Push failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Pull from remote repository
        /// </summary>
        public GitResult Pull(string branch = "main")
        {
            try
            {
                if (!IsGitRepo())
                {
                    return new GitResult
                    {
                        Success = false,
                        Message = "Git repository not initialized"
                    };
                }

                // Create backup before pulling
                var backupManager = new BackupManager(_projectPath);
                backupManager.CreateBackup(BackupTrigger.PreRestore);

                var output = RunGitCommand($"pull origin {branch}");

                // Sync files back to project
                SyncFilesFromRepo();

                return new GitResult
                {
                    Success = true,
                    Message = "Pulled from remote successfully"
                };
            }
            catch (Exception ex)
            {
                return new GitResult
                {
                    Success = false,
                    Message = $"Pull failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get Git status
        /// </summary>
        public GitStatusInfo GetStatus()
        {
            try
            {
                if (!IsGitRepo())
                {
                    return new GitStatusInfo
                    {
                        IsInitialized = false,
                        Message = "Git repository not initialized"
                    };
                }

                var status = RunGitCommand("status --porcelain");
                var hasChanges = !string.IsNullOrWhiteSpace(status);

                var lastCommit = RunGitCommand("log -1 --format=%H|%s|%ci").Trim();
                var parts = lastCommit.Split('|');

                var remoteOutput = RunGitCommand("remote -v");
                var hasRemote = !string.IsNullOrWhiteSpace(remoteOutput);

                return new GitStatusInfo
                {
                    IsInitialized = true,
                    HasChanges = hasChanges,
                    HasRemote = hasRemote,
                    LastCommitHash = parts.Length > 0 ? parts[0] : null,
                    LastCommitMessage = parts.Length > 1 ? parts[1] : null,
                    LastCommitDate = parts.Length > 2 ? DateTime.Parse(parts[2]) : null,
                    Message = hasChanges ? "Uncommitted changes present" : "Working directory clean"
                };
            }
            catch (Exception ex)
            {
                return new GitStatusInfo
                {
                    IsInitialized = false,
                    Message = $"Status check failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// List commit history
        /// </summary>
        public CommitInfo[] GetHistory(int limit = 10)
        {
            try
            {
                if (!IsGitRepo())
                    return Array.Empty<CommitInfo>();

                var output = RunGitCommand($"log -{limit} --format=%H|%s|%ci|%an");
                
                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line =>
                    {
                        var parts = line.Split('|');
                        return new CommitInfo
                        {
                            Hash = parts[0],
                            Message = parts.Length > 1 ? parts[1] : "",
                            Date = parts.Length > 2 ? DateTime.Parse(parts[2]) : DateTime.MinValue,
                            Author = parts.Length > 3 ? parts[3] : ""
                        };
                    })
                    .ToArray();
            }
            catch
            {
                return Array.Empty<CommitInfo>();
            }
        }

        /// <summary>
        /// Check if Git is installed
        /// </summary>
        public static bool IsGitInstalled()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsGitRepo()
        {
            return Directory.Exists(Path.Combine(_gitRepoPath, ".git"));
        }

        private string RunGitCommand(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = _gitRepoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            return output;
        }

        private void SyncFilesToRepo()
        {
            var filesToSync = new[]
            {
                "session_state.json",
                "FACTS.md",
                "PATTERNS.jsonl",
                "MISTAKES.jsonl",
                "DECISIONS.jsonl",
                "UNCERTAINTIES.md",
                "PERFORMANCE.jsonl"
            };

            foreach (var file in filesToSync)
            {
                var source = Path.Combine(_projectPath, file);
                var target = Path.Combine(_gitRepoPath, file);

                if (File.Exists(source))
                {
                    File.Copy(source, target, overwrite: true);
                }
            }

            // Sync Activity directory
            var activitySource = Path.Combine(_projectPath, "Activity");
            var activityTarget = Path.Combine(_gitRepoPath, "Activity");

            if (Directory.Exists(activitySource))
            {
                if (Directory.Exists(activityTarget))
                    Directory.Delete(activityTarget, recursive: true);

                CopyDirectory(activitySource, activityTarget);
            }
        }

        private void SyncFilesFromRepo()
        {
            foreach (var file in Directory.GetFiles(_gitRepoPath, "*", SearchOption.AllDirectories))
            {
                if (file.Contains(".git"))
                    continue;

                var relativePath = Path.GetRelativePath(_gitRepoPath, file);
                var target = Path.Combine(_projectPath, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        private void CreateGitIgnore()
        {
            var gitignore = @"# ClaudeDevStudio Git Ignore
# Ignore temporary files
*.tmp
*.temp
*.bak

# Ignore backup files (handled separately)
*.backup

# Keep all memory files
!session_state.json
!FACTS.md
!PATTERNS.jsonl
!MISTAKES.jsonl
!DECISIONS.jsonl
!UNCERTAINTIES.md
!PERFORMANCE.jsonl
!Activity/
";

            File.WriteAllText(Path.Combine(_gitRepoPath, ".gitignore"), gitignore);
        }

        private void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                var fileName = Path.GetFileName(file);
                File.Copy(file, Path.Combine(target, fileName), overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var dirName = Path.GetFileName(dir);
                CopyDirectory(dir, Path.Combine(target, dirName));
            }
        }

        private string GetGitRepoPath(string projectPath)
        {
            // Store git repo inside project memory directory
            var memoryRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ClaudeDevStudio",
                "Projects",
                Path.GetFileName(projectPath));

            return Path.Combine(memoryRoot, ".cds-memory");
        }
    }

    public class GitResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public bool AlreadyExists { get; set; }
        public bool NoChanges { get; set; }
        public string? CommitHash { get; set; }
    }

    public class GitStatusInfo
    {
        public bool IsInitialized { get; set; }
        public bool HasChanges { get; set; }
        public bool HasRemote { get; set; }
        public string? LastCommitHash { get; set; }
        public string? LastCommitMessage { get; set; }
        public DateTime? LastCommitDate { get; set; }
        public string? Message { get; set; }
    }

    public class CommitInfo
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Author { get; set; } = string.Empty;
    }
}
