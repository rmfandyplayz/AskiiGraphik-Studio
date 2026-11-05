// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// What a window's menu and close button do. One action serves every window, with the window's id as its command
/// parameter, because the windows are registered by id and more can be added later.
/// </summary>
internal sealed class WindowFrameActions(ShellDocking docking) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return new AppAction(WindowFrameActionIds.Float, nameof(Strings.ActionWindowFloat),
            new RelayCommand<string>(Float));

        // Docks arrive in step 2.5f, and it's greyed out until then.
        yield return new AppAction(WindowFrameActionIds.TurnIntoDock, nameof(Strings.ActionWindowTurnIntoDock),
            new RelayCommand<string>(_ => { }, _ => false));

        var turnIntoWidget = new RelayCommand<string>(TurnIntoWidget, _ => docking.CanTurnWindowIntoWidget());
        docking.LayoutChanged += (_, _) => turnIntoWidget.NotifyCanExecuteChanged();
        yield return new AppAction(WindowFrameActionIds.TurnIntoWidget, nameof(Strings.ActionWindowTurnIntoWidget), turnIntoWidget);

        yield return new AppAction(WindowFrameActionIds.Close, nameof(Strings.ActionWindowClose),
            new RelayCommand<string>(Close));
    }

    private void Float(string? windowId)
    {
        if (windowId is not null)
            docking.FloatWindow(windowId);
    }

    private void TurnIntoWidget(string? windowId)
    {
        if (windowId is not null)
            docking.TurnWindowIntoWidget(windowId);
    }

    private void Close(string? windowId)
    {
        if (windowId is not null)
            docking.CloseWindow(windowId);
    }
}
