using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Resolves conflicts when syncing memory across multiple machines
    /// Uses intelligent merge strategies for different file types
    /// </summary>
    public class ConflictResolver
    {
        private readonly string _projectPath;

        public ConflictResolver(string projectPath)
        {
            _projectPath = projectPath;
        }

        /// <summary>
        /// Detect conflicts between local and remote versions
        /// </summary>
        public ConflictDetectionResult DetectConflicts(string backupPath)
        {
            var conflicts = new List<FileConflict>();

            try
            {
                // Extract backup to temp location
                var tempDir = Path.Combine(Path.GetTempPath(), $"cds_conflict_{Guid.NewGuid()}");
                System.IO.Compression.ZipFile.ExtractToDirectory(backupPath, tempDir);

                // Check each critical file
                var filesToCheck = new[]
                {
                    "FACTS.md",
                    "PATTERNS.jsonl",
                    "MISTAKES.jsonl",
                    "DECISIONS.jsonl"
                };

                foreach (var file in filesToCheck)
                {
                    var localPath = Path.Combine(_projectPath, file);
                    var remotePath = Path.Combine(tempDir, file);

                    if (File.Exists(localPath) && File.Exists(remotePath))
                    {
                        if (!FilesAreIdentical(localPath, remotePath))
                        {
                            conflicts.Add(new FileConflict
                            {
                                FileName = file,
                                LocalPath = localPath,
                                RemotePath = remotePath,
                                ConflictType = DetermineConflictType(file),
                                LocalModified = File.GetLastWriteTimeUtc(localPath),
                                RemoteModified = File.GetLastWriteTimeUtc(remotePath)
                            });
                        }
                    }
                }

                Directory.Delete(tempDir, recursive: true);

                return new ConflictDetectionResult
                {
                    HasConflicts = conflicts.Any(),
                    Conflicts = conflicts.ToArray(),
                    Message = conflicts.Any() 
                        ? $"Found {conflicts.Count} conflict(s)" 
                        : "No conflicts detected"
                };
            }
            catch (Exception ex)
            {
                return new ConflictDetectionResult
                {
                    HasConflicts = false,
                    Message = $"Conflict detection failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Resolve conflicts using specified strategy
        /// </summary>
        public ResolveResult ResolveConflicts(FileConflict[] conflicts, MergeStrategy strategy)
        {
            var resolved = 0;
            var failed = 0;

            foreach (var conflict in conflicts)
            {
                try
                {
                    bool success = strategy switch
                    {
                        MergeStrategy.KeepLocal => KeepLocal(conflict),
                        MergeStrategy.KeepRemote => KeepRemote(conflict),
                        MergeStrategy.KeepBoth => KeepBoth(conflict),
                        MergeStrategy.Smart => SmartMerge(conflict),
                        MergeStrategy.Newest => KeepNewest(conflict),
                        _ => false
                    };

                    if (success)
                        resolved++;
                    else
                        failed++;
                }
                catch
                {
                    failed++;
                }
            }

            return new ResolveResult
            {
                Success = failed == 0,
                Resolved = resolved,
                Failed = failed,
                Message = $"Resolved {resolved} conflict(s), {failed} failed"
            };
        }

        /// <summary>
        /// Smart merge - combines data intelligently based on file type
        /// </summary>
        private bool SmartMerge(FileConflict conflict)
        {
            return conflict.ConflictType switch
            {
                ConflictType.AppendableLog => MergeJsonLines(conflict),
                ConflictType.MarkdownDocument => MergeMarkdown(conflict),
                ConflictType.JsonState => MergeJsonState(conflict),
                _ => KeepNewest(conflict)
            };
        }

        /// <summary>
        /// Merge JSONL files (PATTERNS, MISTAKES, DECISIONS)
        /// </summary>
        private bool MergeJsonLines(FileConflict conflict)
        {
            try
            {
                // Read both files
                var localLines = File.ReadAllLines(conflict.LocalPath).ToHashSet();
                var remoteLines = File.ReadAllLines(conflict.RemotePath).ToHashSet();

                // Combine unique entries
                var merged = new HashSet<string>();

                // Parse and deduplicate by ID
                var entries = new Dictionary<string, string>();

                foreach (var line in localLines.Concat(remoteLines))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var id = doc.RootElement.GetProperty("id").GetString();
                        if (id != null && !entries.ContainsKey(id))
                        {
                            entries[id] = line;
                        }
                    }
                    catch
                    {
                        // If no ID, add line as-is
                        merged.Add(line);
                    }
                }

                // Add all unique entries
                foreach (var entry in entries.Values)
                {
                    merged.Add(entry);
                }

                // Sort by timestamp if possible
                var sorted = merged.OrderBy(line =>
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        return doc.RootElement.GetProperty("timestamp").GetDateTime();
                    }
                    catch
                    {
                        return DateTime.MinValue;
                    }
                }).ToList();

                // Write merged result
                File.WriteAllLines(conflict.LocalPath, sorted);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Merge markdown files (FACTS.md)
        /// </summary>
        private bool MergeMarkdown(FileConflict conflict)
        {
            try
            {
                var local = File.ReadAllText(conflict.LocalPath);
                var remote = File.ReadAllText(conflict.RemotePath);

                // Simple line-based merge
                var localLines = local.Split('\n').Select(l => l.Trim()).ToHashSet();
                var remoteLines = remote.Split('\n').Select(l => l.Trim()).ToHashSet();

                // Combine unique lines
                var merged = localLines.Union(remoteLines)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .OrderBy(l => l)
                    .ToList();

                File.WriteAllText(conflict.LocalPath, string.Join("\n", merged));

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Merge JSON state files (session_state.json)
        /// </summary>
        private bool MergeJsonState(FileConflict conflict)
        {
            try
            {
                var localJson = File.ReadAllText(conflict.LocalPath);
                var remoteJson = File.ReadAllText(conflict.RemotePath);

                var local = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(localJson);
                var remote = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(remoteJson);

                if (local == null || remote == null)
                    return false;

                // Merge strategy: keep newest for each field
                var merged = new Dictionary<string, JsonElement>();

                foreach (var key in local.Keys.Union(remote.Keys))
                {
                    if (local.ContainsKey(key) && remote.ContainsKey(key))
                    {
                        // Both have the key - keep the one from most recent file
                        merged[key] = conflict.LocalModified > conflict.RemoteModified
                            ? local[key]
                            : remote[key];
                    }
                    else if (local.ContainsKey(key))
                    {
                        merged[key] = local[key];
                    }
                    else
                    {
                        merged[key] = remote[key];
                    }
                }

                var mergedJson = JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(conflict.LocalPath, mergedJson);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool KeepLocal(FileConflict conflict)
        {
            // Do nothing - local is already in place
            return true;
        }

        private bool KeepRemote(FileConflict conflict)
        {
            try
            {
                File.Copy(conflict.RemotePath, conflict.LocalPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool KeepBoth(FileConflict conflict)
        {
            try
            {
                // Rename local to .local
                var localBackup = conflict.LocalPath + ".local";
                File.Copy(conflict.LocalPath, localBackup, overwrite: true);

                // Copy remote to .remote
                var remoteBackup = conflict.LocalPath + ".remote";
                File.Copy(conflict.RemotePath, remoteBackup, overwrite: true);

                // Keep local as primary
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool KeepNewest(FileConflict conflict)
        {
            try
            {
                if (conflict.RemoteModified > conflict.LocalModified)
                {
                    File.Copy(conflict.RemotePath, conflict.LocalPath, overwrite: true);
                }
                // Otherwise keep local (do nothing)
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool FilesAreIdentical(string path1, string path2)
        {
            var bytes1 = File.ReadAllBytes(path1);
            var bytes2 = File.ReadAllBytes(path2);

            if (bytes1.Length != bytes2.Length)
                return false;

            for (int i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i])
                    return false;
            }

            return true;
        }

        private ConflictType DetermineConflictType(string fileName)
        {
            return fileName.ToLowerInvariant() switch
            {
                var f when f.EndsWith(".jsonl") => ConflictType.AppendableLog,
                var f when f.EndsWith(".md") => ConflictType.MarkdownDocument,
                var f when f.EndsWith(".json") => ConflictType.JsonState,
                _ => ConflictType.Unknown
            };
        }
    }

    public class ConflictDetectionResult
    {
        public bool HasConflicts { get; set; }
        public FileConflict[] Conflicts { get; set; } = Array.Empty<FileConflict>();
        public string? Message { get; set; }
    }

    public class FileConflict
    {
        public string FileName { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public string RemotePath { get; set; } = string.Empty;
        public ConflictType ConflictType { get; set; }
        public DateTime LocalModified { get; set; }
        public DateTime RemoteModified { get; set; }
    }

    public enum ConflictType
    {
        Unknown,
        AppendableLog,      // JSONL files - can merge entries
        MarkdownDocument,   // Markdown - can merge lines
        JsonState,          // JSON state - merge fields
        Binary              // Binary files - must choose one
    }

    public enum MergeStrategy
    {
        KeepLocal,      // Keep local version, discard remote
        KeepRemote,     // Keep remote version, discard local
        KeepBoth,       // Keep both as .local and .remote
        Smart,          // Intelligent merge based on file type
        Newest,         // Keep whichever is newer
        Manual          // User will resolve manually
    }

    public class ResolveResult
    {
        public bool Success { get; set; }
        public int Resolved { get; set; }
        public int Failed { get; set; }
        public string? Message { get; set; }
    }
}
