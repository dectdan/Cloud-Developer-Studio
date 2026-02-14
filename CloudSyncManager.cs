using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClaudeDevStudio.Core
{
    /// <summary>
    /// Manages cloud-based backup and sync using Cloudflare R2/KV
    /// Enables real-time cross-machine sync without OneDrive/Git
    /// </summary>
    public class CloudSyncManager
    {
        private readonly string _projectPath;
        private readonly string _projectName;
        private readonly CloudSyncSettings _settings;
        private readonly HttpClient _httpClient;

        public CloudSyncManager(string projectPath)
        {
            _projectPath = projectPath;
            _projectName = Path.GetFileName(projectPath);
            _settings = LoadSettings();
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Configure cloud sync (Cloudflare R2 or KV)
        /// </summary>
        public void Configure(string accountId, string apiToken, string bucketName, CloudProvider provider = CloudProvider.CloudflareR2)
        {
            _settings.Provider = provider;
            _settings.AccountId = accountId;
            _settings.ApiToken = apiToken;
            _settings.BucketName = bucketName;
            _settings.IsConfigured = true;

            SaveSettings();
        }

        /// <summary>
        /// Upload current memory state to cloud
        /// </summary>
        public async Task<CloudSyncResult> UploadAsync()
        {
            try
            {
                if (!_settings.IsConfigured)
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = "Cloud sync not configured. Run 'claudedev cloud configure' first."
                    };
                }

                // Create backup archive
                var backupManager = new BackupManager(_projectPath);
                var backupResult = backupManager.CreateBackup(BackupTrigger.Manual);

                if (!backupResult.Success)
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = $"Failed to create backup: {backupResult.Message}"
                    };
                }

                // Upload to cloud
                var fileBytes = await File.ReadAllBytesAsync(backupResult.BackupPath!);
                var fileName = $"{_projectName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.backup";

                bool uploadSuccess = _settings.Provider switch
                {
                    CloudProvider.CloudflareR2 => await UploadToR2(fileName, fileBytes),
                    CloudProvider.CloudflareKV => await UploadToKV(fileName, fileBytes),
                    _ => false
                };

                if (uploadSuccess)
                {
                    _settings.LastUpload = DateTime.UtcNow;
                    _settings.UploadCount++;
                    SaveSettings();

                    return new CloudSyncResult
                    {
                        Success = true,
                        Message = $"Uploaded to {_settings.Provider}: {fileName}",
                        BytesTransferred = fileBytes.Length
                    };
                }

                return new CloudSyncResult
                {
                    Success = false,
                    Message = "Upload failed"
                };
            }
            catch (Exception ex)
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = $"Upload failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Download latest memory state from cloud
        /// </summary>
        public async Task<CloudSyncResult> DownloadAsync(string fileName = null)
        {
            try
            {
                if (!_settings.IsConfigured)
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = "Cloud sync not configured"
                    };
                }

                // Get latest backup if no filename specified
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = await GetLatestBackupName();
                    if (fileName == null)
                    {
                        return new CloudSyncResult
                        {
                            Success = false,
                            Message = "No backups found in cloud"
                        };
                    }
                }

                // Download from cloud
                byte[]? fileBytes = _settings.Provider switch
                {
                    CloudProvider.CloudflareR2 => await DownloadFromR2(fileName),
                    CloudProvider.CloudflareKV => await DownloadFromKV(fileName),
                    _ => null
                };

                if (fileBytes == null)
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = "Download failed"
                    };
                }

                // Save to temp file and restore
                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllBytesAsync(tempFile, fileBytes);

                var backupManager = new BackupManager(_projectPath);
                var restoreResult = backupManager.RestoreBackup(tempFile);

                File.Delete(tempFile);

                if (restoreResult.Success)
                {
                    _settings.LastDownload = DateTime.UtcNow;
                    _settings.DownloadCount++;
                    SaveSettings();

                    return new CloudSyncResult
                    {
                        Success = true,
                        Message = $"Downloaded and restored: {fileName}",
                        BytesTransferred = fileBytes.Length
                    };
                }

                return new CloudSyncResult
                {
                    Success = false,
                    Message = $"Download succeeded but restore failed: {restoreResult.Message}"
                };
            }
            catch (Exception ex)
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = $"Download failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// List available backups in cloud
        /// </summary>
        public async Task<CloudBackupInfo[]> ListBackupsAsync()
        {
            try
            {
                if (!_settings.IsConfigured)
                    return Array.Empty<CloudBackupInfo>();

                // Implementation depends on cloud provider
                // For now, return empty array - full implementation would query R2/KV
                return Array.Empty<CloudBackupInfo>();
            }
            catch
            {
                return Array.Empty<CloudBackupInfo>();
            }
        }

        /// <summary>
        /// Get sync status
        /// </summary>
        public CloudSyncStatus GetStatus()
        {
            return new CloudSyncStatus
            {
                IsConfigured = _settings.IsConfigured,
                Provider = _settings.Provider.ToString(),
                LastUpload = _settings.LastUpload,
                LastDownload = _settings.LastDownload,
                UploadCount = _settings.UploadCount,
                DownloadCount = _settings.DownloadCount
            };
        }

        // Cloudflare R2 implementation
        private async Task<bool> UploadToR2(string fileName, byte[] data)
        {
            try
            {
                var url = $"https://{_settings.AccountId}.r2.cloudflarestorage.com/{_settings.BucketName}/{fileName}";

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

                var content = new ByteArrayContent(data);
                var response = await _httpClient.PutAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<byte[]?> DownloadFromR2(string fileName)
        {
            try
            {
                var url = $"https://{_settings.AccountId}.r2.cloudflarestorage.com/{_settings.BucketName}/{fileName}";

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        // Cloudflare KV implementation
        private async Task<bool> UploadToKV(string fileName, byte[] data)
        {
            try
            {
                var url = $"https://api.cloudflare.com/client/v4/accounts/{_settings.AccountId}/storage/kv/namespaces/{_settings.BucketName}/values/{fileName}";

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

                var content = new ByteArrayContent(data);
                var response = await _httpClient.PutAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<byte[]?> DownloadFromKV(string fileName)
        {
            try
            {
                var url = $"https://api.cloudflare.com/client/v4/accounts/{_settings.AccountId}/storage/kv/namespaces/{_settings.BucketName}/values/{fileName}";

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> GetLatestBackupName()
        {
            // This would query the cloud provider for the latest backup
            // For now, return null - full implementation would list and sort
            return null;
        }

        private CloudSyncSettings LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(_projectPath, "cloud_sync_settings.json");
                if (!File.Exists(settingsPath))
                    return new CloudSyncSettings();

                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<CloudSyncSettings>(json) ?? new CloudSyncSettings();
            }
            catch
            {
                return new CloudSyncSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settingsPath = Path.Combine(_projectPath, "cloud_sync_settings.json");
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Don't fail if settings save fails
            }
        }
    }

    public class CloudSyncSettings
    {
        public CloudProvider Provider { get; set; } = CloudProvider.CloudflareR2;
        public string? AccountId { get; set; }
        public string? ApiToken { get; set; }
        public string? BucketName { get; set; }
        public bool IsConfigured { get; set; }
        public DateTime? LastUpload { get; set; }
        public DateTime? LastDownload { get; set; }
        public int UploadCount { get; set; }
        public int DownloadCount { get; set; }
    }

    public enum CloudProvider
    {
        CloudflareR2,
        CloudflareKV,
        AWSs3,
        AzureBlob
    }

    public class CloudSyncResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long BytesTransferred { get; set; }
    }

    public class CloudSyncStatus
    {
        public bool IsConfigured { get; set; }
        public string? Provider { get; set; }
        public DateTime? LastUpload { get; set; }
        public DateTime? LastDownload { get; set; }
        public int UploadCount { get; set; }
        public int DownloadCount { get; set; }
    }

    public class CloudBackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
    }
}
