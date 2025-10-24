using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

/// <summary>
/// Command line arguments for FullCrisis3
/// </summary>
public class CliArguments
{
    [Option('l', "log-file", Required = false, HelpText = "Path to log file for debugging output")]
    public string? LogFile { get; set; }

    [Option("verbosity", Required = false, HelpText = "Set verbosity level (0-3, or use -v/-vv/-vvv)", Default = 0)]
    public int VerbosityLevel { get; set; }


    [Option("windowed", Required = false, HelpText = "Force windowed mode")]
    public bool Windowed { get; set; }

    [Option("fullscreen", Required = false, HelpText = "Force fullscreen mode")]
    public bool Fullscreen { get; set; }

    [Option("width", Required = false, HelpText = "Window width", Default = 1280)]
    public int Width { get; set; }

    [Option("height", Required = false, HelpText = "Window height", Default = 720)]
    public int Height { get; set; }


    [Option("debug-ui", Required = false, HelpText = "Enable UI debugging features")]
    public bool DebugUI { get; set; }


    [Option("self-upgrade", Required = false, HelpText = "Download and install the latest version from full-crisis-3.jmcateer.com")]
    public bool SelfUpgrade { get; set; }

    // Allow pass-through of unknown arguments to Avalonia
    [Value(0, MetaName = "avalonia-args", HelpText = "Additional arguments passed to Avalonia framework")]
    public IEnumerable<string>? AvaloniaArgs { get; set; }

}

/// <summary>
/// Global access to parsed command line arguments
/// </summary>
public static class GlobalArgs
{
    /// <summary>
    /// Parsed command line arguments, available globally throughout the application
    /// </summary>
    public static CliArguments Current { get; set; } = new();
}