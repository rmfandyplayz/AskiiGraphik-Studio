// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Dialogs;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Shell;

public sealed class ModalDialogTests : IDisposable
{
    private readonly Window _owner = new();

    public ModalDialogTests()
    {
        _owner.Show();
    }

    public void Dispose() => _owner.Close();

    [AvaloniaFact]
    public void AvaloniasShowDialog_FinishesBeforeTheDialogHasFinishedClosing()
    {
        // Why ModalDialog exists. If this starts failing, Avalonia has changed and ModalDialog may not be needed.
        var dialog = new Window();
        var ownerWhenFinished = WhenFinished(dialog.ShowDialog<int>(_owner), dialog);
        Dispatcher.UIThread.RunJobs();

        dialog.Close(7);

        Assert.Same(_owner, ownerWhenFinished());
    }

    [AvaloniaFact]
    public void ShowAsync_ReturnsTheResult_OnlyOnceTheDialogHasFinishedClosing()
    {
        var dialog = new Window();
        var showing = ModalDialog.ShowAsync<int>(dialog, _owner);
        var ownerWhenFinished = WhenFinished(showing, dialog);
        Dispatcher.UIThread.RunJobs();

        dialog.Close(7);
        Dispatcher.UIThread.RunJobs();

        Assert.True(showing.IsCompleted);
        Assert.Equal(7, showing.Result);
        Assert.Null(ownerWhenFinished());
    }

    // The dialog's owner at the moment the task finishes (Avalonia clears it when the dialog has closed),
    // read on the UI thread inside the completion itself.
    private Func<WindowBase?> WhenFinished(Task task, Window dialog)
    {
        WindowBase? owner = _owner;
        var recorded = false;
        task.ContinueWith(_ =>
        {
            owner = dialog.Owner;
            recorded = true;
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        return () =>
        {
            Assert.True(recorded);
            return owner;
        };
    }
}
