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

/// <summary>An outline around the cells of a mask, in a style: faint and still, or bright and running.</summary>
internal readonly record struct SelectionOutlineLayer(SelectionMask Mask, SelectionLineStyle Style, bool IsFaint);

/// <summary>
/// What the canvas shows of the selection (ADR 0061). With no gesture, it's the selection, with a running outline and
/// hatching over every cell outside it. During a selection tool's gesture, it's what the gesture would leave, with
/// hatching the other way, in the mode's colour, over the cells it would deselect, and the outlines the mode shows.
/// </summary>
/// <param name="Selected">
/// The selection shown. The cells outside it, and outside <paramref name="Lost"/>, are hatched; when it's empty none
/// are, since then nothing is selected.
/// </param>
/// <param name="Lost">The cells the gesture would deselect, or null for none.</param>
/// <param name="LostStyle">The colours of the hatching over <paramref name="Lost"/>.</param>
/// <param name="Outlines">The outlines, drawn in this order, so the bright ones end up on top.</param>
internal sealed record SelectionDisplay(
    SelectionMask Selected,
    SelectionMask? Lost,
    SelectionLineStyle LostStyle,
    IReadOnlyList<SelectionOutlineLayer> Outlines)
{
    /// <summary>True when an outline's dashes run, so the canvas keeps repainting.</summary>
    public bool HasRunningOutline => Outlines.Any(layer => !layer.IsFaint && !layer.Mask.IsEmpty);

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

            return new SelectionDisplay(selection, null, SelectionLineStyle.Plain, [Bright(selection)]);
        }

        // Add: the result's outline is bright, and the lines of the old selection and the shape that end up inside it
        //   stay faint.
        // Subtract: the old selection goes faint, and the shape runs in Subtract's colours.
        // Intersect: the shape is faint and the part of it that stays selected runs, both in Intersect's colours.
        var result = preview.Result;
        return preview.Mode switch
        {
            SelectionCombineMode.Add => new SelectionDisplay(result, null, SelectionLineStyle.Plain,
                [Faint(preview.Before), Faint(preview.Shape), Bright(result)]),
            SelectionCombineMode.Subtract => new SelectionDisplay(result, preview.Lost, SelectionLineStyle.Subtract,
                [Faint(preview.Before), Bright(preview.Shape, SelectionLineStyle.Subtract)]),
            SelectionCombineMode.Intersect => new SelectionDisplay(result, preview.Lost, SelectionLineStyle.Intersect,
                [Faint(preview.Shape, SelectionLineStyle.Intersect), Bright(result, SelectionLineStyle.Intersect)]),
            _ => new SelectionDisplay(result, null, SelectionLineStyle.Plain, [Bright(result)])
        };
    }

    private static SelectionOutlineLayer Bright(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, IsFaint: false);

    private static SelectionOutlineLayer Faint(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, IsFaint: true);
}
