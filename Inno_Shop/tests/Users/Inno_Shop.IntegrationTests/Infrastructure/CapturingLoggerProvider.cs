using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Inno_Shop.IntegrationTests.Infrastructure;

/// <summary>
/// Keeps the application's log lines (including unhandled exceptions) in memory.
/// The API hides exception details outside Development, so without this a failing
/// integration test would only report "500" with no explanation.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries;

    public CapturingLoggerProvider(ConcurrentQueue<string> entries) => _entries = entries;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries, categoryName);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _entries;
        private readonly string _category;

        public CapturingLogger(ConcurrentQueue<string> entries, string category)
        {
            _entries = entries;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Warning && exception is null)
                return;

            var line = $"[{logLevel}] {_category}: {formatter(state, exception)}";
            if (exception is not null)
                line += $" :: {exception.GetType().Name}: {exception.Message}";

            _entries.Enqueue(line);
        }
    }
}
