// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Input;

namespace AskiiArt.App.Layers;

/// <summary>
/// The box a layer's name is typed into. An arrow key it can't use, such as Up in a box of one line or Left at its
/// start, does nothing. Otherwise it would go on to the list, which would pick another row and end the rename.
/// </summary>
internal sealed class LayerNameBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right)
            e.Handled = true;
    }
}
