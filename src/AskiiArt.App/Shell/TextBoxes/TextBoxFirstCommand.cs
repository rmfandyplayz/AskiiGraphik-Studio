// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;

namespace AskiiArt.App.Shell.TextBoxes;

/// <summary>
/// An app action whose shortcut a text box also edits with, such as Select All or Undo. While a text box has the
/// keyboard it makes the text box's edit instead, whether it came from the shortcut or the menu, so it can always run
/// then (ADR 0057). In the macOS menu bar the menu takes the shortcut before the window sees it, so the menu item has
/// to do this too.
/// </summary>
internal sealed class TextBoxFirstCommand(ICommand command, TextBoxEdit edit, TextBoxShortcuts textBoxes) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            command.CanExecuteChanged += value;
            textBoxes.FocusMoved += value;
        }
        remove
        {
            command.CanExecuteChanged -= value;
            textBoxes.FocusMoved -= value;
        }
    }

    public bool CanExecute(object? parameter) => textBoxes.ActiveTextBox is not null || command.CanExecute(parameter);

    public void Execute(object? parameter)
    {
        if (textBoxes.ActiveTextBox is { } textBox)
            TextBoxShortcuts.Make(edit, textBox);
        else
            command.Execute(parameter);
    }
}
