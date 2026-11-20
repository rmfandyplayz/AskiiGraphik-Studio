// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using Avalonia.Controls;

namespace AskiiArt.App.Shell;

/// <summary>
/// A window shortcut that a text box also edits with, such as Select All or Undo. While a text box has the keyboard it
/// can't run, so the key goes on to the text box: Avalonia's window key bindings see a key before the focused control
/// does, and only take it when the command can run.
/// </summary>
internal sealed class YieldToTextBoxCommand(ICommand command, TopLevel window) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => command.CanExecuteChanged += value;
        remove => command.CanExecuteChanged -= value;
    }

    public bool CanExecute(object? parameter) => !IsTextBoxFocused && command.CanExecute(parameter);

    public void Execute(object? parameter) => command.Execute(parameter);

    private bool IsTextBoxFocused => window.FocusManager.GetFocusedElement() is TextBox;
}
