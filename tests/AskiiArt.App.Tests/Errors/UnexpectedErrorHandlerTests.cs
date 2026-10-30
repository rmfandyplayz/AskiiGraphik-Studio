// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Errors;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Tests.Errors;

// [AvaloniaFact] runs these on Avalonia's UI thread, like the real handler; RunJobs() lets the
// dialog's continuation run after a simulated button click.
public sealed class UnexpectedErrorHandlerTests
{
    private readonly FakeAppShell _shell = new();
    private readonly RecordingLogger<UnexpectedErrorHandler> _logger = new();
    private readonly UnexpectedErrorHandler _handler;

    public UnexpectedErrorHandlerTests()
    {
        _handler = new UnexpectedErrorHandler(_shell, _logger);
    }

    [AvaloniaFact]
    public void UiThreadException_IsLoggedWithTheException_AndShowsTheDialog()
    {
        var exception = new InvalidOperationException("boom");

        var handled = _handler.HandleUiThreadException(exception);

        Assert.True(handled);
        Assert.Equal(1, _shell.ErrorDialogShownCount);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
    }

    [AvaloniaFact]
    public void ChoosingQuit_ShutsTheAppDown()
    {
        _handler.HandleUiThreadException(new InvalidOperationException());

        _shell.CloseErrorDialog(UnexpectedErrorChoice.Quit);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, _shell.ShutdownRequestCount);
    }

    [AvaloniaFact]
    public void ChoosingContinue_KeepsTheAppRunning()
    {
        _handler.HandleUiThreadException(new InvalidOperationException());

        _shell.CloseErrorDialog(UnexpectedErrorChoice.Continue);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, _shell.ShutdownRequestCount);
    }

    [AvaloniaFact]
    public void ErrorsWhileTheDialogIsOpen_AreLoggedButShowNoSecondDialog()
    {
        _handler.HandleUiThreadException(new InvalidOperationException("first"));
        _handler.HandleUiThreadException(new InvalidOperationException("second"));

        Assert.Equal(1, _shell.ErrorDialogShownCount);
        Assert.Equal(2, _logger.Entries.Count);
    }

    [AvaloniaFact]
    public void AfterTheDialogCloses_TheNextErrorShowsItAgain()
    {
        _handler.HandleUiThreadException(new InvalidOperationException("first"));
        _shell.CloseErrorDialog(UnexpectedErrorChoice.Continue);
        Dispatcher.UIThread.RunJobs();

        _handler.HandleUiThreadException(new InvalidOperationException("second"));

        Assert.Equal(2, _shell.ErrorDialogShownCount);
    }

    [AvaloniaFact]
    public void BeforeTheMainWindowExists_TheExceptionIsLogged_ButNotHandled()
    {
        _shell.CanShowDialogs = false;

        var handled = _handler.HandleUiThreadException(new InvalidOperationException());

        Assert.False(handled);
        Assert.Equal(0, _shell.ErrorDialogShownCount);
        Assert.Single(_logger.Entries);
    }

    [AvaloniaFact]
    public void WhenTheDialogItselfFails_TheFailureIsLogged_AndNothingIsThrown()
    {
        var dialogFailure = new InvalidOperationException("dialog broke");
        _shell.ErrorDialogFailure = dialogFailure;

        _handler.HandleUiThreadException(new InvalidOperationException("original"));
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(_logger.Entries, entry => ReferenceEquals(entry.Exception, dialogFailure));
    }

    [Fact]
    public void FatalException_IsLoggedAsCritical()
    {
        var exception = new InvalidOperationException("fatal");

        _handler.HandleFatalException(exception);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Critical, entry.Level);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    public void UnobservedTaskException_IsLoggedWithoutADialog()
    {
        _handler.HandleUnobservedTaskException(new InvalidOperationException());

        Assert.Equal(LogLevel.Error, Assert.Single(_logger.Entries).Level);
        Assert.Equal(0, _shell.ErrorDialogShownCount);
    }
}
