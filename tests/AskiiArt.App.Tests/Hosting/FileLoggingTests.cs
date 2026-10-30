// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Hosting.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Tests.Hosting;

public sealed class FileLoggingTests : IDisposable
{
    private readonly string _logFolder = Path.Combine(Path.GetTempPath(), "AskiiArt.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Messages_AreWrittenToADatedFileInTheLogFolder()
    {
        WriteLog(logger => logger.LogInformation("Hello from the test"));

        var logFile = Assert.Single(Directory.GetFiles(_logFolder));
        Assert.Matches(@"^log-\d{8}\.txt$", Path.GetFileName(logFile));
        Assert.Contains("Hello from the test", File.ReadAllText(logFile), StringComparison.Ordinal);
    }

    [Fact]
    public void LogFile_UsesOnlyLfLineEndings_EvenForExceptionStackTraces()
    {
        WriteLog(logger => logger.LogError(CreateThrownException(), "Something failed"));

        var contents = File.ReadAllText(Assert.Single(Directory.GetFiles(_logFolder)));
        Assert.Contains(nameof(InvalidOperationException), contents, StringComparison.Ordinal);
        Assert.Contains("\n", contents, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", contents, StringComparison.Ordinal);
    }

    [Fact]
    public void MessagesBelowTheMinimumLevel_AreNotWritten()
    {
        WriteLog(logger =>
        {
            logger.LogDebug("Too detailed");
            logger.LogInformation("Kept");
        }, LogLevel.Information);

        var contents = File.ReadAllText(Assert.Single(Directory.GetFiles(_logFolder)));
        Assert.DoesNotContain("Too detailed", contents, StringComparison.Ordinal);
        Assert.Contains("Kept", contents, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_logFolder))
            Directory.Delete(_logFolder, recursive: true);
    }

    private void WriteLog(Action<ILogger> write, LogLevel minimumLevel = LogLevel.Debug)
    {
        // Disposing the provider closes the log file, so it can be read and deleted.
        using var services = new ServiceCollection().AddFileLogging(_logFolder, minimumLevel).BuildServiceProvider();
        write(services.GetRequiredService<ILoggerFactory>().CreateLogger("Test"));
    }

    private static InvalidOperationException CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("Thrown so it has a multi-line stack trace");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
