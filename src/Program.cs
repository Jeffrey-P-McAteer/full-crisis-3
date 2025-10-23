using Avalonia;
using Avalonia.ReactiveUI;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FullCrisis3;

public sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Parse command line arguments using CommandLineParser
        Parser.Default.ParseArguments<CliArguments>(args)
            .WithParsed(arguments => RunApplication(arguments).GetAwaiter().GetResult())
            .WithNotParsed(HandleParseError);
    }


    private static async Task RunApplication(CliArguments arguments)
    {
        // Store parsed arguments globally
        GlobalArgs.Current = arguments;
        
        // Attach to console based on launch method
        var attachedToConsole = ConsoleManager.AttachToParentConsoleIfNeeded();
        
        // Initialize logger with parsed arguments
        Logger.Initialize(arguments.LogFile, 1); // Default verbosity level
        Logger.LogMethod(nameof(Main), $"Arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");
        Logger.Info($"Log file: {arguments.LogFile ?? "Console only"}");
        Logger.Info($"Console attached: {attachedToConsole}");
        Logger.Info($"Launched from command line: {ConsoleManager.WasLaunchedFromCommandLine()}");
        
        // Handle self-upgrade if requested
        if (arguments.SelfUpgrade)
        {
            Logger.Info("Self-upgrade requested...");
            var upgradeSuccess = await SelfUpgradeManager.PerformSelfUpgradeAsync();
            
            if (upgradeSuccess)
            {
                Logger.Info("Self-upgrade completed successfully. Exiting...");
                Environment.Exit(0);
            }
            else
            {
                Logger.LogMethod(nameof(RunApplication), "Self-upgrade failed. Continuing with current version...");
                Environment.Exit(1);
            }
            
            return; // Don't start the main application
        }
        
        // Convert remaining args for Avalonia
        var avaloniaArgs = arguments.AvaloniaArgs?.ToArray() ?? Array.Empty<string>();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(avaloniaArgs);
    }

    private static void HandleParseError(IEnumerable<Error> errors)
    {
        // For help and version requests, just exit gracefully
        if (errors.Any(e => e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.VersionRequestedError))
        {
            Environment.Exit(0);
        }
        
        // For other errors, log them and exit with error code
        Console.Error.WriteLine("Error parsing command line arguments:");
        foreach (var error in errors)
        {
            Console.Error.WriteLine($"  {error}");
        }
        Environment.Exit(1);
    }


    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .LogToTrace();
}