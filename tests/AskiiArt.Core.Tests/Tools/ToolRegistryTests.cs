// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.Core.Tests.Tools;

public sealed class ToolRegistryTests
{
    private static readonly ToolDefinition[] _builtInTools =
    [
        HandTool.Definition,
        MoveTool.Definition,
        RectangleSelectTool.Definition,
        EllipseSelectTool.Definition,
        LassoTool.Definition,
        PolygonalLassoTool.Definition,
        MagicWandTool.Definition,
        TextTool.Definition,
        BrushTool.Definition,
        ColorBrushTool.ForegroundDefinition,
        ColorBrushTool.BackgroundDefinition,
        CharacterBrushTool.Definition,
        EraserTool.Definition,
        SoftEraserTool.Definition,
        EraserTool.BackgroundDefinition,
        LineTool.Definition
    ];

    [Fact]
    public void TheBuiltInTools_AreGrouped_WithTheBrushesTogether_TheErasersTogether_AndTheShapesTogether()
    {
        var registry = new ToolRegistry(_builtInTools, BuiltInToolGroups.All);

        Assert.Equal(["", "V", "M", "T", "B", "E", "U"],
            registry.Groups.Select(group => group.ShortcutLetter.ToString()));
        Assert.Equal([MoveTool.Id], registry.GroupOf(MoveTool.Id).ToolIds);
        Assert.Equal(
            [RectangleSelectTool.Id, EllipseSelectTool.Id, LassoTool.Id, PolygonalLassoTool.Id, MagicWandTool.Id],
            registry.GroupOf(LassoTool.Id).ToolIds);
        Assert.Equal(MagicWandTool.Id, BuiltInToolGroups.Select.ToolLetters['W']);
        Assert.Equal(
            [BrushTool.Id, ColorBrushTool.ForegroundId, ColorBrushTool.BackgroundId, CharacterBrushTool.Id],
            registry.GroupOf(ColorBrushTool.BackgroundId).ToolIds);
        Assert.Equal(
            [EraserTool.Id, SoftEraserTool.Id, EraserTool.BackgroundId],
            registry.GroupOf(SoftEraserTool.Id).ToolIds);
        Assert.Same(BuiltInToolGroups.Eraser, registry.GetGroup(BuiltInToolGroups.Eraser.Id));
        Assert.Equal([LineTool.Id], registry.GroupOf(LineTool.Id).ToolIds);
    }

    [Fact]
    public void TheSelectionTools_ShareTheirMode()
    {
        Assert.All(
            [RectangleSelectTool.Definition, EllipseSelectTool.Definition, LassoTool.Definition,
                PolygonalLassoTool.Definition],
            tool => Assert.Same(SelectionToolOptions.Group, Assert.Single(tool.OptionGroups)));
    }

    [Fact]
    public void TheEraserGroupsTools_ShareTheirOwnTip()
    {
        Assert.All(
            [EraserTool.Definition, SoftEraserTool.Definition, EraserTool.BackgroundDefinition],
            tool => Assert.Same(EraserTool.OptionGroup, tool.OptionGroups[0]));
        Assert.NotSame(BrushTool.TipOptions, EraserTool.OptionGroup);
        Assert.Single(EraserTool.BackgroundDefinition.OptionGroups);
    }

    [Fact]
    public void TheBrushGroupsTools_ShareTheTip()
    {
        Assert.All(
            [BrushTool.Definition, ColorBrushTool.ForegroundDefinition, ColorBrushTool.BackgroundDefinition],
            tool => Assert.Same(BrushTool.TipOptions, tool.OptionGroups[0]));
        Assert.Single(ColorBrushTool.ForegroundDefinition.OptionGroups);
    }

    [Fact]
    public void AToolInNoGroup_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ToolRegistry(_builtInTools, [BuiltInToolGroups.Hand]));
    }

    [Fact]
    public void AToolInTwoGroups_IsRejected()
    {
        var again = new ToolGroup("again", "ToolHand", "tool.hand", null, [HandTool.Id]);

        Assert.Throws<ArgumentException>(() => new ToolRegistry(_builtInTools, [.. BuiltInToolGroups.All, again]));
    }

    [Fact]
    public void AGroupNamingAToolThatIsntRegistered_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new ToolRegistry([HandTool.Definition], [BuiltInToolGroups.Brush]));
    }

    [Fact]
    public void AnEmptyGroup_IsRejected()
    {
        var empty = new ToolGroup("empty", "ToolHand", "tool.hand", null, []);

        Assert.Throws<ArgumentException>(() => new ToolRegistry([], [empty]));
    }

    [Fact]
    public void TwoGroupsWithOneIdOrOneLetter_AreRejected()
    {
        var sameId = BuiltInToolGroups.Text with { ToolIds = [TextTool.Id] };
        var sameLetter = BuiltInToolGroups.Text with { Id = "other", ShortcutLetter = 'V' };

        Assert.Throws<ArgumentException>(() =>
            new ToolRegistry([HandTool.Definition, TextTool.Definition], [BuiltInToolGroups.Text, sameId]));
        Assert.Throws<ArgumentException>(() =>
            new ToolRegistry([MoveTool.Definition, TextTool.Definition], [BuiltInToolGroups.Move, sameLetter]));
    }

    [Fact]
    public void AToolLetterThatIsAnotherGroupsLetter_IsRejected()
    {
        var text = BuiltInToolGroups.Text with
        {
            ToolLetters = new Dictionary<char, string> { ['V'] = TextTool.Id }
        };

        Assert.Throws<ArgumentException>(() =>
            new ToolRegistry([MoveTool.Definition, TextTool.Definition], [BuiltInToolGroups.Move, text]));
    }

    [Fact]
    public void AToolLetterForAnotherGroupsTool_IsRejected()
    {
        var text = BuiltInToolGroups.Text with
        {
            ToolLetters = new Dictionary<char, string> { ['W'] = HandTool.Id }
        };

        Assert.Throws<ArgumentException>(() =>
            new ToolRegistry([HandTool.Definition, TextTool.Definition], [BuiltInToolGroups.Hand, text]));
    }

    [Fact]
    public void AnUnknownId_IsntFound()
    {
        var registry = new ToolRegistry([HandTool.Definition], [BuiltInToolGroups.Hand]);

        Assert.Throws<KeyNotFoundException>(() => registry.GroupOf("nothing"));
        Assert.Throws<KeyNotFoundException>(() => registry.GetGroup("nothing"));
    }
}
