using Avalonia;
using System;
using System.Linq;

namespace FullCrisis3;

public sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var (logFile, verbosity, avaloniaArgs) = ParseArgs(args);
        Logger.Initialize(logFile, verbosity);
        Logger.LogMethod(nameof(Main), string.Join(" ", args));
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(avaloniaArgs);
    }

    private static (string? logFile, int verbosity, string[] avaloniaArgs) ParseArgs(string[] args)
    {
        string? logFile = null;
        int verbosity = 0;
        var remainingArgs = new System.Collections.Generic.List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--log-file" when i + 1 < args.Length:
                    logFile = args[++i];
                    break;
                case "-v":
                    verbosity = 1;
                    break;
                case "-vv":
                    verbosity = 2;
                    break;
                case "-vvv":
                    verbosity = 3;
                    break;
                default:
                    remainingArgs.Add(args[i]);
                    break;
            }
        }

        return (logFile, verbosity, remainingArgs.ToArray());
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}