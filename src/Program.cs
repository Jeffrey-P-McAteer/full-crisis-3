using Avalonia;
using Avalonia.ReactiveUI;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FullCrisis3;

public sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Preprocess arguments to handle -v, -vv, -vvv patterns
        var processedArgs = PreprocessVerbosityArgs(args);
        
        // Parse command line arguments using CommandLineParser
        Parser.Default.ParseArguments<CliArguments>(processedArgs)
            .WithParsed(RunApplication)
            .WithNotParsed(HandleParseError);
    }

    private static string[] PreprocessVerbosityArgs(string[] args)
    {
        var processedArgs = new List<string>();
        
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "-v":
                    processedArgs.Add("--verbosity");
                    processedArgs.Add("1");
                    break;
                case "-vv":
                    processedArgs.Add("--verbosity");
                    processedArgs.Add("2");
                    break;
                case "-vvv":
                    processedArgs.Add("--verbosity");
                    processedArgs.Add("3");
                    break;
                default:
                    processedArgs.Add(arg);
                    break;
            }
        }
        
        return processedArgs.ToArray();
    }

    private static void RunApplication(CliArguments arguments)
    {
        // Store parsed arguments globally
        GlobalArgs.Current = arguments;
        
        // Initialize logger with parsed arguments
        Logger.Initialize(arguments.LogFile, arguments.EffectiveVerbosity);
        Logger.LogMethod(nameof(Main), $"Arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");
        Logger.Info($"Verbosity level: {arguments.EffectiveVerbosity}");
        Logger.Info($"Log file: {arguments.LogFile ?? "Console only"}");
        
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