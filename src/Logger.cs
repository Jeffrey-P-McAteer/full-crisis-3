using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FullCrisis3;

public static class Logger
{
    private static string? _logFile;
    private static int _verbosity = 0;
    private static readonly object _lock = new();

    public static void Initialize(string? logFile, int verbosity)
    {
        _logFile = logFile;
        _verbosity = verbosity;
        if (!string.IsNullOrEmpty(_logFile))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFile)!);
                File.WriteAllText(_logFile, $"=== FullCrisis3 Log Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            }
            catch { }
        }
    }

    public static void Info(string message) => Log(1, "INFO", message);
    public static void Debug(string message) => Log(2, "DEBUG", message);
    public static void Trace(string message) => Log(3, "TRACE", message);

    public static void LogMethod([CallerMemberName] string methodName = "", params object[] args)
    {
        if (_verbosity >= 3)
        {
            var argsStr = args.Length > 0 ? $"({string.Join(", ", args)})" : "()";
            Log(3, "METHOD", $"{methodName}{argsStr}");
        }
    }

    private static void Log(int level, string type, string message)
    {
        if (_verbosity < level) return;

        var threadName = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();
        var logLine = $"{DateTime.Now:HH:mm:ss.fff} [{threadName}] [{type}] {message}";
        
        lock (_lock)
        {
            try
            {
                Console.WriteLine(logLine);
                if (!string.IsNullOrEmpty(_logFile))
                    File.AppendAllText(_logFile, logLine + Environment.NewLine);
            }
            catch { }
        }
    }
}