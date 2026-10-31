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

    public required IReadOnlyList<GlyphBatch> Glyphs { get; init; }

    /// <summary>Cells drawn with a fallback font (ADR 0013); flagged until the owner designs the real marker.</summary>
    public required IReadOnlyList<PixelRect> FallbackGlyphCells { get; init; }

    /// <summary>Cells whose glyph no font has; flagged until the owner designs the real marker.</summary>
    public required IReadOnlyList<PixelRect> PlaceholderGlyphCells { get; init; }

    public required bool IsGridVisible { get; init; }

    public required PixelRect? HoveredCell { get; init; }

    public void Dispose()
    {
        foreach (var batch in Glyphs)
            batch.Dispose();
    }
}
