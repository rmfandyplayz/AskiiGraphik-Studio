// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Everything one frame of the canvas draws, in device pixels relative to the viewport. Built on
/// the UI thread by <see cref="CanvasSceneBuilder"/> and painted on Avalonia's render thread by
/// <see cref="CanvasScenePainter"/>. It holds its own references to what it draws, so the UI
/// thread can carry on changing the document and atlas meanwhile.
/// </summary>
internal sealed class CanvasScene : IDisposable
{
    public required CanvasViewport Viewport { get; init; }

    public required CanvasColors Colors { get; init; }

    /// <summary>The visible cells' backgrounds, or null when none of them has one.</summary>
    public required CellBackgrounds? Backgrounds { get; init; }

    public required IReadOnlyList<GlyphBatch> Glyphs { get; init; }

    /// <summary>Cells drawn with a fallback font (ADR 0013); flagged until the owner designs the real marker.</summary>
    public required IReadOnlyList<PixelRect> FallbackGlyphCells { get; init; }

    /// <summary>Cells whose glyph no font has; flagged until the owner designs the real marker.</summary>
    public required IReadOnlyList<PixelRect> PlaceholderGlyphCells { get; init; }

    public required bool IsGridVisible { get; init; }

    public required PixelRect? HoveredCell { get; init; }

    /// <summary>The text caret's cell, drawn as an outline <see cref="CaretThickness"/> pixels thick.</summary>
    public required PixelRect? Caret { get; init; }

    public required int CaretThickness { get; init; }

    /// <summary>The pieces of the brush tip's outline, each a filled strip along the inside of its cells.</summary>
    public required IReadOnlyList<PixelRect> BrushOutline { get; init; }

    /// <summary>
    /// The visible pieces of the selection's outline, each a strip along the inside of its cells, drawn dashed.
    /// </summary>
    public IReadOnlyList<SelectionStrip> SelectionOutline { get; init; } = [];

    /// <summary>The length of each dash of the selection outline, and of each gap, in device pixels.</summary>
    public int SelectionDashLength { get; init; } = 1;

    /// <summary>How far the selection outline's dashes have run around it, in device pixels.</summary>
    public int SelectionDashOffset { get; init; }

    /// <summary>The points of the line the tool shows, such as a lasso's path, in device pixels.</summary>
    public IReadOnlyList<Point> ToolPath { get; init; } = [];

    /// <summary>The tool's line's thickness, in device pixels.</summary>
    public int ToolPathThickness { get; init; } = 1;

    public void Dispose()
    {
        Backgrounds?.Dispose();
        foreach (var batch in Glyphs)
            batch.Dispose();
    }
}
