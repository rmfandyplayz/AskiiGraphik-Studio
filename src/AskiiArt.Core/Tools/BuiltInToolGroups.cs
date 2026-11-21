// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.Core.Tools;

/// <summary>The groups of the built-in tools (ADR 0049), which register the way a plugin's would.</summary>
public static class BuiltInToolGroups
{
    /// <summary>The Hand tool, on V.</summary>
    public static ToolGroup Hand { get; } = new("askiiart.group.hand", "ToolHand", "tool.hand", 'V', [HandTool.Id]);

    /// <summary>The Text tool, on T.</summary>
    public static ToolGroup Text { get; } = new("askiiart.group.text", "ToolText", "tool.text", 'T', [TextTool.Id]);

    /// <summary>
    /// The selection tools, on M (ADR 0055, ADR 0060, ADR 0063): Rectangle Select, Ellipse Select, the Lasso, the
    /// Polygonal Lasso and the Magic Wand, which W also picks.
    /// </summary>
    public static ToolGroup Select { get; } = new("askiiart.group.select", "ToolGroupSelect", "tool.select", 'M',
        [RectangleSelectTool.Id, EllipseSelectTool.Id, LassoTool.Id, PolygonalLassoTool.Id, MagicWandTool.Id])
    {
        ToolLetters = new Dictionary<char, string> { ['W'] = MagicWandTool.Id }
    };

    /// <summary>
    /// The Brush, which paints ramps, then the FG Brush, the BG Brush and the Character Brush (ADR 0053), on B.
    /// </summary>
    public static ToolGroup Brush { get; } = new("askiiart.group.brush", "ToolGroupBrush", "tool.brush", 'B',
        [BrushTool.Id, ColorBrushTool.ForegroundId, ColorBrushTool.BackgroundId, CharacterBrushTool.Id]);

    /// <summary>The Eraser, then the Soft Eraser and the BG Eraser, on E (ADR 0050).</summary>
    public static ToolGroup Eraser { get; } = new("askiiart.group.eraser", "ToolGroupEraser", "tool.eraser", 'E',
        [EraserTool.Id, SoftEraserTool.Id, EraserTool.BackgroundId]);

    /// <summary>Every built-in group, in the order the Tools toolbar shows them.</summary>
    public static IReadOnlyList<ToolGroup> All { get; } = [Hand, Select, Text, Brush, Eraser];
}
