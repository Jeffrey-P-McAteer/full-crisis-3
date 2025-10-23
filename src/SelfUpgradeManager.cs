using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FullCrisis3;

/// <summary>
/// Manages self-upgrade functionality for the application
/// </summary>
public static class SelfUpgradeManager
{
    private const string BASE_URL = "https://full-crisis-3.jmcateer.com";
    private const string WINDOWS_BINARY_URL = $"{BASE_URL}/FullCrisis3.win.x64.exe";
    private const string LINUX_BINARY_URL = $"{BASE_URL}/FullCrisis3.linux.x64";

    /// <summary>
    /// Performs a self-upgrade of the current executable
    /// </summary>
    /// <returns>True if upgrade was successful, false otherwise</returns>
    public static async Task<bool> PerformSelfUpgradeAsync()
    {
        try
        {
            Logger.Info("Starting self-upgrade process...");
            
            // Determine current executable path
            var currentExecutablePath = GetCurrentExecutablePath();
            if (string.IsNullOrEmpty(currentExecutablePath))
            {
                Logger.LogMethod("PerformSelfUpgradeAsync", "Error: Could not determine current executable path");
                return false;
            }

            Logger.Info($"Current executable: {currentExecutablePath}");

            // Determine download URL based on platform
            var downloadUrl = GetDownloadUrlForCurrentPlatform();
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Logger.LogMethod("PerformSelfUpgradeAsync", "Error: Unsupported platform for self-upgrade");
                return false;
            }

            Logger.Info($"Download URL: {downloadUrl}");

            // Create temporary file for download
            var tempFilePath = Path.GetTempFileName();
            var backupFilePath = currentExecutablePath + ".backup";

            try
            {
                // Download new version
                Logger.Info("Downloading new version...");
                if (!await DownloadFileAsync(downloadUrl, tempFilePath))
                {
                    Logger.LogMethod("PerformSelfUpgradeAsync", "Error: Failed to download new version");
                    return false;
                }

                Logger.Info("Download completed successfully");

                // Verify downloaded file is executable
                if (!IsValidExecutable(tempFilePath))
                {
                    Logger.LogMethod("PerformSelfUpgradeAsync", "Error: Downloaded file is not a valid executable");
                    return false;
                }

                // Create backup of current executable
                Logger.Info("Creating backup of current executable...");
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
                File.Copy(currentExecutablePath, backupFilePath);

                // Replace current executable with new version
                Logger.Info("Replacing current executable with new version...");
                if (!ReplaceExecutable(currentExecutablePath, tempFilePath))
                {
                    Logger.LogMethod("PerformSelfUpgradeAsync", "Error: Failed to replace executable");
                    
                    // Restore from backup
                    try
                    {
                        File.Copy(backupFilePath, currentExecutablePath, true);
                        Logger.Info("Restored original executable from backup");
                    }
                    catch (Exception restoreEx)
                    {
                        Logger.LogMethod("PerformSelfUpgradeAsync", $"Error restoring backup: {restoreEx.Message}");
                    }
                    
                    return false;
                }

                // Set executable permissions on Linux
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    SetExecutablePermissions(currentExecutablePath);
                }

                Logger.Info("Self-upgrade completed successfully!");
                Logger.Info("The application has been updated. Please restart to use the new version.");
                
                // Clean up backup file
                try
                {
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    Logger.LogMethod("PerformSelfUpgradeAsync", $"Warning: Could not clean up backup file: {cleanupEx.Message}");
                }

                return true;
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    Logger.LogMethod("PerformSelfUpgradeAsync", $"Warning: Could not clean up temp file: {cleanupEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("PerformSelfUpgradeAsync", $"Error during self-upgrade: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the path to the current executable
    /// </summary>
    private static string? GetCurrentExecutablePath()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule?.FileName;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("GetCurrentExecutablePath", $"Error getting executable path: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the download URL for the current platform
    /// </summary>
    private static string? GetDownloadUrlForCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WINDOWS_BINARY_URL;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return LINUX_BINARY_URL;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads a file from the specified URL with progress reporting
    /// </summary>
    private static async Task<bool> DownloadFileAsync(string url, string destinationPath)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for download
            
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogMethod("DownloadFileAsync", $"HTTP error: {response.StatusCode} - {response.ReasonPhrase}");
                return false;
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue)
            {
                Logger.Info($"Download size: {contentLength.Value / 1024 / 1024:F1} MB");
            }
            else
            {
                Logger.Info("Download size: Unknown");
            }

            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var downloadStream = await response.Content.ReadAsStreamAsync();
            
            await CopyStreamWithProgressAsync(downloadStream, fileStream, contentLength);
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("DownloadFileAsync", $"Error downloading file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Copies data from source stream to destination with progress reporting
    /// </summary>
    private static async Task CopyStreamWithProgressAsync(Stream source, Stream destination, long? totalBytes)
    {
        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int lastReportedPercentage = -1;
        var startTime = DateTime.UtcNow;
        
        Console.Write("Progress: 0%");
        
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            
            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                var percentage = (int)((totalBytesRead * 100) / totalBytes.Value);
                
                // Report progress every 5%
                if (percentage >= lastReportedPercentage + 5)
                {
                    lastReportedPercentage = percentage - (percentage % 5); // Round down to nearest 5%
                    
                    // Calculate ETA
                    var elapsed = DateTime.UtcNow - startTime;
                    var eta = "Unknown";
                    
                    if (percentage > 0)
                    {
                        var totalEstimatedTime = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * 100 / percentage);
                        var remaining = totalEstimatedTime - elapsed;
                        
                        if (remaining.TotalSeconds > 0)
                        {
                            if (remaining.TotalMinutes >= 1)
                            {
                                eta = $"{remaining.Minutes}m {remaining.Seconds}s";
                            }
                            else
                            {
                                eta = $"{remaining.Seconds}s";
                            }
                        }
                        else
                        {
                            eta = "Almost done";
                        }
                    }
                    
                    Console.Write($"\rProgress: {lastReportedPercentage}% (ETA: {eta})");
                }
            }
        }
        
        Console.WriteLine("\rProgress: 100% - Download complete!");
    }

    /// <summary>
    /// Validates that the downloaded file is a valid executable
    /// </summary>
    private static bool IsValidExecutable(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Check file size (should be reasonable for an executable)
            if (fileInfo.Length < 1024 * 1024) // Less than 1MB seems too small
            {
                Logger.LogMethod("IsValidExecutable", $"File too small: {fileInfo.Length} bytes");
                return false;
            }

            // Check for executable headers
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var buffer = new byte[4];
            fileStream.Read(buffer, 0, 4);

            // Check for PE header (Windows) or ELF header (Linux)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // PE files start with "MZ"
                return buffer[0] == 0x4D && buffer[1] == 0x5A;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // ELF files start with 0x7F followed by "ELF"
                return buffer[0] == 0x7F && buffer[1] == 0x45 && buffer[2] == 0x4C && buffer[3] == 0x46;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("IsValidExecutable", $"Error validating executable: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Replaces the current executable with the new version
    /// </summary>
    private static bool ReplaceExecutable(string currentPath, string newPath)
    {
        try
        {
            // On Windows, we might need to handle file locking differently
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Try to replace the file directly
                File.Copy(newPath, currentPath, true);
            }
            else
            {
                // On Linux, use atomic move operation
                File.Move(newPath, currentPath, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("ReplaceExecutable", $"Error replacing executable: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets executable permissions on Linux
    /// </summary>
    private static void SetExecutablePermissions(string filePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Logger.Info("Set executable permissions successfully");
            }
            else
            {
                Logger.LogMethod("SetExecutablePermissions", $"chmod failed with exit code: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("SetExecutablePermissions", $"Error setting executable permissions: {ex.Message}");
        }
    }
}