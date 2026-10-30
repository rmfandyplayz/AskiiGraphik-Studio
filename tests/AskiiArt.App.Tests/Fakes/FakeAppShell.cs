// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Records what the code under test asked the shell to do.</summary>
internal sealed class FakeAppShell : IAppShell
{
    private TaskCompletionSource<UnexpectedErrorChoice> _errorDialog = new();

    public bool CanShowDialogs { get; set; } = true;

    public int ShutdownRequestCount { get; private set; }

    public List<string> OpenedFolders { get; } = [];

    public int AboutShownCount { get; private set; }

    public int ErrorDialogShownCount { get; private set; }

    /// <summary>When set, the error dialog fails with this exception instead of returning a choice.</summary>
    public Exception? ErrorDialogFailure { get; set; }

    public void RequestShutdown() => ShutdownRequestCount++;

    public Task OpenFolderAsync(string folderPath)
    {
        OpenedFolders.Add(folderPath);
        return Task.CompletedTask;
    }

    public Task ShowAboutAsync()
    {
        AboutShownCount++;
        return Task.CompletedTask;
    }

    public Task<UnexpectedErrorChoice> ShowUnexpectedErrorAsync()
    {
        ErrorDialogShownCount++;

        if (ErrorDialogFailure is not null)
            return Task.FromException<UnexpectedErrorChoice>(ErrorDialogFailure);

        _errorDialog = new TaskCompletionSource<UnexpectedErrorChoice>();
        return _errorDialog.Task;
    }

    /// <summary>Simulates the user clicking a button in the open error dialog.</summary>
    public void CloseErrorDialog(UnexpectedErrorChoice choice) => _errorDialog.SetResult(choice);
}
