// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.InteropServices;
using AskiiArt.App.Errors;
using AskiiArt.App.Hosting;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App;

internal static partial class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Services and logging come up before Avalonia, so a failure while Avalonia starts is
        // still logged. Nothing here may touch Avalonia APIs before BuildAvaloniaApp runs.
        using var services = AppServices.Create(AppDataLocations.ForCurrentUser());
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("AskiiArt.App.Startup");
        var errors = services.GetRequiredService<UnexpectedErrorHandler>();

        AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => errors.HandleFatalException(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            errors.HandleUnobservedTaskException(e.Exception);
            e.SetObserved();
        };

        LogStarting(logger, AppVersion.Informational, RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription);
        var exitCode = BuildAvaloniaApp(services).StartWithClassicDesktopLifetime(args);
        LogExiting(logger, exitCode);

        return exitCode;
    }

    // The IDE previewer looks for a method with exactly this name. It gets an App without
    // services, which skips the startup wiring the previewer doesn't need.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
    {
        return AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .LogToTrace();
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Starting version {Version} on {OperatingSystem}, {Framework}")]
    private static partial void LogStarting(ILogger logger, string version, string operatingSystem, string framework);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exiting with code {ExitCode}")]
    private static partial void LogExiting(ILogger logger, int exitCode);
}
