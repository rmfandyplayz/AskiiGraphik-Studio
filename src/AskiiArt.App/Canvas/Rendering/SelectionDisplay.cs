// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>Which colours an outline, or the hatching over cells about to be deselected, is drawn in.</summary>
internal enum SelectionLineStyle
{
    /// <summary>The selection outline's own colours.</summary>
    Plain,

    /// <summary>Subtract's colours.</summary>
    Subtract,

    /// <summary>Intersect's colours.</summary>
    Intersect
}

/// <summary>How strongly an outline shows, from the theme's tokens (ADR 0061).</summary>
internal enum SelectionOutlineStrength
{
    /// <summary>In full colour with its dashes running: the lines of what letting go would leave selected.</summary>
    Bright,

    /// <summary>
    /// Faint and still: the shape's lines that won't be kept, and in Subtract the old selection's lines that stay.
    /// </summary>
    Faint,

    /// <summary>Fainter still: the old selection's lines that won't be kept.</summary>
    Vanishing
}

/// <summary>How the outlines' lines past the canvas's edges show (ADR 0071).</summary>
internal enum OffCanvasLines
{
    /// <summary>Weaker than the rest of each outline, and running with it: the selection keeps them.</summary>
    Kept,

    /// <summary>Faint and still: the selection tool in use drops them.</summary>
    Dropped
}

/// <summary>An outline around the cells of a mask, in a style and a strength.</summary>
internal readonly record struct SelectionOutlineLayer(
    SelectionMask Mask,
    SelectionLineStyle Style,
    SelectionOutlineStrength Strength);

/// <summary>
/// What the canvas shows of the selection (ADR 0061). With no gesture, it's the selection, with a running outline and
/// moving hatching over every cell outside it. During a selection tool's gesture, it's what the gesture would leave,
/// with hatching the other way, in the mode's colour, over the cells it would deselect, and the outlines the mode
/// shows. Later outlines are drawn over earlier ones, so where a line is both kept and not, it shows bright. Past the
/// canvas's edges only the outlines show, weaker, and fainter still during a gesture, which drops that part (ADR 0071).
/// </summary>
/// <param name="Selected">The selection shown.</param>
/// <param name="Lost">The cells the gesture would deselect, or null for none.</param>
/// <param name="LostStyle">The colours of the hatching over <paramref name="Lost"/>.</param>
/// <param name="Outlines">The outlines, drawn in this order, so the bright ones end up on top.</param>
/// <param name="HatchesUnselected">
/// True when the cells outside <paramref name="Selected"/> and <paramref name="Lost"/> are hatched: whenever
/// something is selected, or was before the gesture, so the hatching doesn't vanish while a shape would leave nothing.
/// </param>
/// <param name="UnselectedHatchMoves">
/// True when that hatching moves. It stands still during Subtract and Intersect, so only their hatching moves.
/// </param>
/// <param name="OffCanvas">How the outlines' lines past the canvas's edges show.</param>
internal sealed record SelectionDisplay(
    SelectionMask Selected,
    SelectionMask? Lost,
    SelectionLineStyle LostStyle,
    IReadOnlyList<SelectionOutlineLayer> Outlines,
    bool HatchesUnselected,
    bool UnselectedHatchMoves,
    OffCanvasLines OffCanvas = OffCanvasLines.Kept)
{
    /// <summary>True when an outline's dashes run.</summary>
    public bool HasRunningOutline =>
        Outlines.Any(layer => layer.Strength == SelectionOutlineStrength.Bright && !layer.Mask.IsEmpty);

    /// <summary>True when an outline runs or hatching moves, so the canvas keeps repainting.</summary>
    public bool IsMoving =>
        HasRunningOutline || HatchesUnselected && UnselectedHatchMoves || Lost is { IsEmpty: false };

    /// <summary>
    /// What to show of <paramref name="selection"/>, or, during a gesture, of <paramref name="preview"/>. Null when
    /// there's nothing to show.
    /// </summary>
    public static SelectionDisplay? For(SelectionMask? selection, SelectionPreview? preview)
    {
        if (preview is null)
        {
            if (selection is null || selection.IsEmpty)
                return null;

            return new SelectionDisplay(selection, null, SelectionLineStyle.Plain, [Bright(selection)],
                HatchesUnselected: true, UnselectedHatchMoves: true);
        }

        // Lines that letting go would keep are bright. Of the rest, the shape's are faint and the old selection's
        // fainter (Vanishing), except Subtract's old lines that stay, which are faint while the shape's line runs.
        // Replace: the old selection's outline vanishes under the result's.
        // Add: the old selection's and the shape's lines inside the result fade under its outline.
        // Subtract: the shape runs in Subtract's colours over the old outline, faint where it stays.
        // Intersect: what stays runs in Intersect's colours, over the shape's outline and the old one.
        var before = preview.Before;
        var shape = preview.Shape;
        var result = preview.Result;
        var hatchesUnselected = !before.IsEmpty || !result.IsEmpty;
        return preview.Mode switch
        {
            SelectionCombineMode.Add => new SelectionDisplay(result, null, SelectionLineStyle.Plain,
                [Vanishing(before), Faint(shape), Bright(result)], hatchesUnselected, UnselectedHatchMoves: true,
                OffCanvasLines.Dropped),
            SelectionCombineMode.Subtract => new SelectionDisplay(result, preview.Lost, SelectionLineStyle.Subtract,
                [Vanishing(preview.Lost), Faint(result), Bright(shape, SelectionLineStyle.Subtract)],
                hatchesUnselected, UnselectedHatchMoves: false, OffCanvasLines.Dropped),
            SelectionCombineMode.Intersect => new SelectionDisplay(result, preview.Lost, SelectionLineStyle.Intersect,
                [Vanishing(before), Faint(shape, SelectionLineStyle.Intersect),
                    Bright(result, SelectionLineStyle.Intersect)],
                hatchesUnselected, UnselectedHatchMoves: false, OffCanvasLines.Dropped),
            _ => new SelectionDisplay(result, null, SelectionLineStyle.Plain, [Vanishing(before), Bright(result)],
                hatchesUnselected, UnselectedHatchMoves: true, OffCanvasLines.Dropped)
        };
    }

    private static SelectionOutlineLayer Bright(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, SelectionOutlineStrength.Bright);

    private static SelectionOutlineLayer Faint(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, SelectionOutlineStrength.Faint);

    private static SelectionOutlineLayer Vanishing(SelectionMask mask) =>
        new(mask, SelectionLineStyle.Plain, SelectionOutlineStrength.Vanishing);
}
