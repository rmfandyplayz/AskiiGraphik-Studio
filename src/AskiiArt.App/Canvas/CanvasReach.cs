// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The cells the canvas view can scroll to (ADR 0072): the canvas and the zone around it, and when the selection goes
/// past that zone, the selection with as much room around it.
/// </summary>
internal static class CanvasReach
{
    /// <summary>
    /// The reach for a canvas of <paramref name="canvasSize"/> with <paramref name="selection"/>, or nothing selected,
    /// and cells <paramref name="cellWidthToHeight"/> as wide as they're tall.
    /// </summary>
    public static CellRect Of(CanvasSize canvasSize, SelectionMask? selection, double cellWidthToHeight)
    {
        var zone = SelectionZone.Around(canvasSize, cellWidthToHeight);
        if (selection is not { IsEmpty: false } || zone.Contains(selection.Bounds))
            return zone;

        return zone.Union(SelectionZone.Around(selection.Bounds, cellWidthToHeight));
    }
}
