// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Canvas;

public sealed class PanCursorTests
{
    [Fact]
    public void OnAMac_ItsTheOpenHand_AndElsewhereTheMoveArrows()
    {
        Assert.Equal(StandardCursorType.DragMove, PanCursor.TypeFor(isMacOS: true));
        Assert.Equal(StandardCursorType.SizeAll, PanCursor.TypeFor(isMacOS: false));
    }
}
