using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Diagnostics;

namespace Event.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Entity Framework interceptor to track database performance metrics
/// </summary>
public class PerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PerformanceInterceptor> _logger;
    private readonly Dictionary<DbCommand, CommandExecutionContext> _executionContexts = new();

    public PerformanceInterceptor(ILogger<PerformanceInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartCommandExecution(command, "Reader");
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        EndCommandExecution(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartCommandExecution(command, "NonQuery");
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        EndCommandExecution(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        StartCommandExecution(command, "Scalar");
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        EndCommandExecution(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        EndCommandExecution(command, eventData, eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        EndCommandExecution(command, eventData, eventData.Exception);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void StartCommandExecution(DbCommand command, string commandType)
    {
        var context = new CommandExecutionContext
        {
            CommandType = commandType,
            StartTime = DateTime.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        lock (_executionContexts)
        {
            _executionContexts[command] = context;
        }

        // Increment query count in HTTP context if available
        if (System.Threading.Thread.CurrentThread.IsThreadPoolThread)
        {
            var httpContext = GetHttpContext();
            if (httpContext != null)
            {
                var currentCount = httpContext.Items.TryGetValue("DatabaseQueryCount", out var count) ? (int)count! : 0;
                httpContext.Items["DatabaseQueryCount"] = currentCount + 1;
            }
        }
    }

    private void EndCommandExecution(DbCommand command, CommandEventData eventData, Exception? exception = null)
    {
        CommandExecutionContext? context = null;

        lock (_executionContexts)
        {
            if (_executionContexts.TryGetValue(command, out context))
            {
                _executionContexts.Remove(command);
            }
        }

        if (context == null) return;

        context.Stopwatch.Stop();
        var durationMs = context.Stopwatch.Elapsed.TotalMilliseconds;

        // Update HTTP context with database duration
        if (System.Threading.Thread.CurrentThread.IsThreadPoolThread)
        {
            var httpContext = GetHttpContext();
            if (httpContext != null)
            {
                var currentDuration = httpContext.Items.TryGetValue("DatabaseDuration", out var duration) ? (double)duration! : 0.0;
                httpContext.Items["DatabaseDuration"] = currentDuration + durationMs;
            }
        }

        // Log slow queries
        if (durationMs > 1000) // Queries taking more than 1 second
        {
            _logger.LogWarning("Slow database query detected: {Duration}ms - {CommandType} - {CommandText}",
                durationMs, context.CommandType, TruncateCommandText(command.CommandText));
        }

        // Log failed queries
        if (exception != null)
        {
            _logger.LogError(exception, "Database query failed: {Duration}ms - {CommandType} - {CommandText}",
                durationMs, context.CommandType, TruncateCommandText(command.CommandText));
        }

        // Log debug information for all queries in development
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Database query executed: {Duration}ms - {CommandType} - Parameters: {ParameterCount}",
                durationMs, context.CommandType, command.Parameters.Count);
        }
    }

    private string TruncateCommandText(string commandText)
    {
        const int maxLength = 200;
        if (string.IsNullOrEmpty(commandText) || commandText.Length <= maxLength)
        {
            return commandText ?? string.Empty;
        }

        return commandText.Substring(0, maxLength) + "...";
    }

    private Microsoft.AspNetCore.Http.HttpContext? GetHttpContext()
    {
        try
        {
            // Try to get HttpContext from IHttpContextAccessor if available
            var httpContextAccessor = ServiceLocator.Current?.GetService(typeof(Microsoft.AspNetCore.Http.IHttpContextAccessor)) 
                as Microsoft.AspNetCore.Http.IHttpContextAccessor;
            return httpContextAccessor?.HttpContext;
        }
        catch
        {
            // Ignore errors when getting HttpContext
            return null;
        }
    }

    private class CommandExecutionContext
    {
        public string CommandType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public Stopwatch Stopwatch { get; set; } = null!;
    }
}

/// <summary>
/// Simple service locator for getting services in interceptors
/// This is a workaround since interceptors don't have direct access to DI container
/// </summary>
public static class ServiceLocator
{
    public static IServiceProvider? Current { get; set; }
}
