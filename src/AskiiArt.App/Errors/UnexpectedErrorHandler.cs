// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Errors;

/// <summary>
/// What happens when an exception reaches the top without being caught (ADR 0008, ARCHITECTURE
/// §20). Every case is logged; only UI-thread exceptions get a dialog, because only they can be
/// survived.
/// </summary>
internal sealed partial class UnexpectedErrorHandler(IAppShell shell, ILogger<UnexpectedErrorHandler> logger)
{
    // Guards against a storm of dialogs when the same error repeats (for example on every frame):
    // while the dialog is open, further errors are logged only. UI-thread only, so no locking.
    private bool _isDialogOpen;

    /// <summary>
    /// Handles an exception that escaped a UI-thread job.
    /// </summary>
    /// <returns>
    /// True if the app can keep running. False before the main window exists: there's nothing to
    /// show a dialog in and the app hasn't finished starting, so it's better to let it crash.
    /// </returns>
    public bool HandleUiThreadException(Exception exception)
    {
        LogUiThreadException(exception);

        if (!shell.CanShowDialogs)
            return false;

        if (!_isDialogOpen)
            _ = AskUserAsync();

        return true;
    }

    /// <summary>
    /// Handles an exception on a background thread. The process ends right after this returns,
    /// whatever we do, so this only logs.
    /// </summary>
    public void HandleFatalException(Exception? exception)
    {
        LogFatalException(exception);
    }

    /// <summary>Handles a failed task whose exception nothing ever looked at.</summary>
    public void HandleUnobservedTaskException(Exception exception)
    {
        LogUnobservedTaskException(exception);
    }

    private async Task AskUserAsync()
    {
        _isDialogOpen = true;
        try
        {
            var choice = await shell.ShowUnexpectedErrorAsync();
            if (choice == UnexpectedErrorChoice.Quit)
                shell.RequestShutdown();
        }
        catch (Exception dialogException)
        {
            // Rethrowing would come straight back here; logging is all that's left.
            LogDialogFailed(dialogException);
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception on the UI thread")]
    private partial void LogUiThreadException(Exception exception);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Unhandled exception on a background thread; the app will exit")]
    private partial void LogFatalException(Exception? exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unobserved task exception")]
    private partial void LogUnobservedTaskException(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The unexpected-error dialog itself failed")]
    private partial void LogDialogFailed(Exception exception);
}
