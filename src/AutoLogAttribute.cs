using MethodBoundaryAspect.Fody.Attributes;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FullCrisis3;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class AutoLogAttribute : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        var methodName = $"{args.Method.DeclaringType?.Name}.{args.Method.Name}";
        var threadId = Thread.CurrentThread.ManagedThreadId;
        
        // Start timing
        args.MethodExecutionTag = Stopwatch.StartNew();
        
        // Log method entry with arguments
        if (args.Arguments.Length > 0)
        {
            var parameters = args.Method.GetParameters();
            var argStrings = args.Arguments
                .Select((arg, i) => $"{parameters[i].Name}={arg ?? "<null>"}")
                .ToArray();
            
            Logger.Trace($"[{threadId}] → {methodName}({string.Join(", ", argStrings)})");
        }
        else
        {
            Logger.Trace($"[{threadId}] → {methodName}()");
        }
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var methodName = $"{args.Method.DeclaringType?.Name}.{args.Method.Name}";
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var stopwatch = (Stopwatch)args.MethodExecutionTag;
        stopwatch?.Stop();
        
        var duration = stopwatch?.ElapsedMilliseconds ?? 0;
        
        if (args.ReturnValue != null)
        {
            Logger.Trace($"[{threadId}] ← {methodName} returned {args.ReturnValue} ({duration}ms)");
        }
        else
        {
            Logger.Trace($"[{threadId}] ← {methodName} ({duration}ms)");
        }
    }

    public override void OnException(MethodExecutionArgs args)
    {
        var methodName = $"{args.Method.DeclaringType?.Name}.{args.Method.Name}";
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var stopwatch = (Stopwatch)args.MethodExecutionTag;
        stopwatch?.Stop();
        
        var duration = stopwatch?.ElapsedMilliseconds ?? 0;
        
        Logger.Trace($"[{threadId}] ✗ {methodName} threw {args.Exception?.GetType().Name}: {args.Exception?.Message} ({duration}ms)");
    }
}