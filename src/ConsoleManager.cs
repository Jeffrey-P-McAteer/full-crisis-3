using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Management;

namespace FullCrisis3;

/// <summary>
/// Manages console attachment for applications based on how they were launched
/// </summary>
public static class ConsoleManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    private const int ATTACH_PARENT_PROCESS = -1;

    /// <summary>
    /// Determines if the application was launched from a command line interface (vs Explorer)
    /// </summary>
    /// <returns>True if launched from command line, false if launched from Explorer/GUI</returns>
    public static bool WasLaunchedFromCommandLine()
    {
        try
        {
            // Method 1: Check parent process name
            var parentProcessName = GetParentProcessName();
            Logger.LogMethod("WasLaunchedFromCommandLine", $"Parent process: {parentProcessName ?? "unknown"}");
            
            if (!string.IsNullOrEmpty(parentProcessName))
            {
                var parentName = parentProcessName.ToLowerInvariant();
                
                // If parent is explorer, we were launched from GUI
                if (parentName == "explorer")
                {
                    Logger.LogMethod("WasLaunchedFromCommandLine", "Detected GUI launch (explorer parent)");
                    return false;
                }
                
                // If parent is a known command line interface, we were launched from CLI
                if (parentName == "cmd" || parentName == "powershell" || 
                    parentName == "pwsh" || parentName == "bash" || 
                    parentName == "wt" || parentName == "windowsterminal" ||
                    parentName == "dotnet" || parentName == "konsole" || 
                    parentName == "gnome-terminal" || parentName == "xterm" ||
                    parentName == "timeout" || parentName == "sh" || parentName == "zsh")
                {
                    Logger.LogMethod("WasLaunchedFromCommandLine", $"Detected CLI launch ({parentName} parent)");
                    return true;
                }
            }

            // Method 2: Fallback - check console cursor position (works on .NET 5+)
            try
            {
                var position = Console.GetCursorPosition();
                // If cursor is at (0,0), likely launched from GUI
                // If cursor is elsewhere, likely launched from command line
                return position.Left != 0 || position.Top != 0;
            }
            catch
            {
                // GetCursorPosition might not work in all environments
            }

            // Method 3: Final fallback - check if we already have a console (Windows only)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetConsoleWindow() != IntPtr.Zero;
            }
            
            // Default fallback - assume command line on non-Windows platforms
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogMethod("WasLaunchedFromCommandLine", $"Error detecting launch method: {ex.Message}");
            // When in doubt, assume GUI launch (safer default)
            return false;
        }
    }

    /// <summary>
    /// Gets the name of the parent process
    /// </summary>
    private static string? GetParentProcessName()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use WMI on Windows
                var myId = currentProcess.Id;
                var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {myId}";
                
                using var search = new ManagementObjectSearcher("root\\CIMV2", query);
                using var results = search.Get();
                
                var parentProcessId = results.OfType<ManagementObject>()
                    .FirstOrDefault()?["ParentProcessId"];
                    
                if (parentProcessId != null)
                {
                    var parentId = Convert.ToInt32(parentProcessId);
                    using var parent = Process.GetProcessById(parentId);
                    return parent.ProcessName;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("GetParentProcessName", $"Error getting parent process: {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Handles console attachment/detachment based on launch method and user preferences
    /// On Windows: Releases console if launched from explorer.exe, keeps it otherwise
    /// On Linux: Always keeps console (Linux handles this automatically)
    /// </summary>
    /// <param name="forceAttach">Force console attachment regardless of launch method</param>
    /// <param name="preventAttach">Prevent console attachment even if launched from command line</param>
    /// <returns>True if console is attached or should remain attached, false if detached</returns>
    public static bool AttachToParentConsoleIfNeeded(bool forceAttach = false, bool preventAttach = false)
    {
        // On non-Windows platforms, console is automatically handled by the OS
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.LogMethod("AttachToParentConsoleIfNeeded", "Console handling automatic on this platform");
            return true;
        }

        try
        {
            // If explicitly told not to attach, respect that
            if (preventAttach)
            {
                Logger.LogMethod("AttachToParentConsoleIfNeeded", "Console attachment prevented by --no-console flag");
                return false;
            }

            // If we already have a console window, no need to attach
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Logger.LogMethod("AttachToParentConsoleIfNeeded", "Console already exists");
                return true;
            }

            // Check if we were launched from explorer.exe and should release the console
            var parentProcessName = GetParentProcessName();
            bool launchedFromExplorer = !string.IsNullOrEmpty(parentProcessName) && 
                                      parentProcessName.ToLowerInvariant() == "explorer";
            
            // If launched from explorer and not forced to attach, release the console
            if (launchedFromExplorer && !forceAttach)
            {
                Logger.LogMethod("AttachToParentConsoleIfNeeded", "Launched from explorer.exe - releasing console");
                DetachConsole();
                return false;
            }
            
            // Determine if we should attach based on launch method or force flag
            bool shouldAttach = forceAttach || WasLaunchedFromCommandLine();
            
            if (!shouldAttach)
            {
                Logger.LogMethod("AttachToParentConsoleIfNeeded", "Not launched from command line and not forced - skipping console attachment");
                return false;
            }

            // Try to attach to parent console first
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                Logger.LogMethod("AttachToParentConsoleIfNeeded", "Successfully attached to parent console");
                return true;
            }
            else
            {
                // If attach fails and we're forcing attachment, try allocating a new console
                if (forceAttach && AllocConsole())
                {
                    Logger.LogMethod("AttachToParentConsoleIfNeeded", "Created new console window (forced)");
                    return true;
                }
                else
                {
                    Logger.LogMethod("AttachToParentConsoleIfNeeded", "Failed to attach or allocate console");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("AttachToParentConsoleIfNeeded", $"Error during console attachment: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Detaches from the current console
    /// </summary>
    public static void DetachConsole()
    {
        try
        {
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                FreeConsole();
                Logger.LogMethod("DetachConsole", "Detached from console");
            }
        }
        catch (Exception ex)
        {
            Logger.LogMethod("DetachConsole", $"Error detaching console: {ex.Message}");
        }
    }
}