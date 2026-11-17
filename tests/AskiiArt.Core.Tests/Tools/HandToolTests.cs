// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Hand;

namespace AskiiArt.Core.Tests.Tools;

public sealed class HandToolTests
{
    private readonly EditingSession _session = new(TestCells.CreateDocument(4, 2));
    private readonly HandTool _tool = new();

    [Fact]
    public void ItPansWithThePrimaryButtonAndWithSpace_AndTakesNoText()
    {
        Assert.True(_tool.PansWithPrimaryButton);
        Assert.True(_tool.AllowsSpacePanning);
        Assert.False(_tool.IsTakingText);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void Input_NeverChangesTheDocument_AndUsesNoKeysOrText()
    {
        var input = new ToolPointerInput(new CellPoint(1, 1), 0.5, 0.5, ToolModifiers.None);

        _tool.PointerPressed(input);
        _tool.PointerMoved(input with { Cell = new CellPoint(2, 1) });
        _tool.PointerReleased(input);

        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Delete, ToolModifiers.None));
        Assert.False(_tool.TextEntered("a"));
        Assert.False(_session.History.CanUndo);
    }

    [Fact]
    public void ItsShortcutIsV_AndTheRegistryCreatesOne()
    {
        var registry = new ToolRegistry([HandTool.Definition], [BuiltInToolGroups.Hand]);

        var tool = registry.Get(HandTool.Id).Create(TestTools.Context(_session));

        Assert.IsType<HandTool>(tool);
        Assert.Equal('V', registry.GroupOf(HandTool.Id).ShortcutLetter);
    }
}
