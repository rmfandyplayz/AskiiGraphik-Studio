// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Tests.Fakes;

internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

/// <summary>An <see cref="ILogger{T}"/> that keeps every entry in memory for assertions.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }
}
