// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace AskiiArt.App.Hosting.Logging;

/// <summary>
/// Sets up logging to a rolling file in the log folder (ADR 0006). App code only ever sees
/// Microsoft's <see cref="Microsoft.Extensions.Logging.ILogger"/>; Serilog is just the file writer.
/// </summary>
internal static class FileLogging
{
    public const string FileNamePattern = "log-.txt";
    public const int RetainedFileCount = 7;
    public const long FileSizeLimitBytes = 10 * 1024 * 1024;

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public static IServiceCollection AddFileLogging(this IServiceCollection services, string logFolder,
        LogLevel minimumLevel)
    {
        var serilogLogger = CreateSerilogLogger(logFolder, minimumLevel);

        return services.AddLogging(builder => builder
            .SetMinimumLevel(minimumLevel)
            .AddSerilog(serilogLogger, dispose: true));
    }

    public static Serilog.Core.Logger CreateSerilogLogger(string logFolder, LogLevel minimumLevel)
    {
        Directory.CreateDirectory(logFolder);

        var formatter = new LfLineEndingFormatter(
            new MessageTemplateTextFormatter(OutputTemplate, CultureInfo.InvariantCulture));

        // Unbuffered (the default): every event reaches the disk immediately, so the log still
        // holds the last messages if the process dies.
        return new LoggerConfiguration()
            .MinimumLevel.Is(ToSerilogLevel(minimumLevel))
            .WriteTo.File(
                formatter,
                Path.Combine(logFolder, FileNamePattern),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: FileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: RetainedFileCount)
            .CreateLogger();
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Fatal,
        };
    }
}
