// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.Core.Tools;

/// <summary>The groups of the built-in tools (ADR 0049), which register the way a plugin's would.</summary>
public static class BuiltInToolGroups
{
    /// <summary>The Hand tool, on V.</summary>
    public static ToolGroup Hand { get; } = new("askiiart.group.hand", "ToolHand", "tool.hand", 'V', [HandTool.Id]);

    /// <summary>The Text tool, on T.</summary>
    public static ToolGroup Text { get; } = new("askiiart.group.text", "ToolText", "tool.text", 'T', [TextTool.Id]);

    /// <summary>The Brush, which paints ramps, then the FG Brush and the BG Brush, on B.</summary>
    public static ToolGroup Brush { get; } = new("askiiart.group.brush", "ToolGroupBrush", "tool.brush", 'B',
        [BrushTool.Id, ColorBrushTool.ForegroundId, ColorBrushTool.BackgroundId]);

    /// <summary>The Eraser, on E.</summary>
    public static ToolGroup Eraser { get; } =
        new("askiiart.group.eraser", "ToolEraser", "tool.eraser", 'E', [EraserTool.Id]);

    /// <summary>Every built-in group, in the order the Tools toolbar shows them.</summary>
    public static IReadOnlyList<ToolGroup> All { get; } = [Hand, Text, Brush, Eraser];
}
