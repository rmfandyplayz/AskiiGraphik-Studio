// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>How much of a toolbar's visible length fades at each end, from 0 to 0.5.</summary>
internal readonly record struct ToolbarFadeAmounts(double Start, double End);

/// <summary>
/// The fade on each side of a toolbar that has more to scroll to (docs/DOCKING.md §4.2). It's a mask over the
/// buttons rather than a shape painted on top, because the toolbar is see-through and a painted shape would show.
/// </summary>
internal static class ToolbarFades
{
    // Scrolling lands on fractions of a pixel, and a fade for less than half a pixel would just flicker.
    private const double ScrollTolerance = 0.5;
    private const double MostOfTheLengthAFadeCanTake = 0.5;

    public static ToolbarFadeAmounts For(double offset, double extent, double viewport, double fadeLength)
    {
        if (viewport <= 0 || fadeLength <= 0)
            return default;

        var amount = Math.Min(fadeLength / viewport, MostOfTheLengthAFadeCanTake);
        return new ToolbarFadeAmounts(
            offset > ScrollTolerance ? amount : 0,
            offset + viewport < extent - ScrollTolerance ? amount : 0);
    }

    /// <summary>The mask for these amounts, or null when nothing fades. Only the mask's opacity matters, not its colour.</summary>
    public static LinearGradientBrush? ToMask(ToolbarFadeAmounts fades, bool isHorizontal)
    {
        if (fades == default)
            return null;

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = isHorizontal
                ? new RelativePoint(1, 0, RelativeUnit.Relative)
                : new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(fades.Start > 0 ? Colors.Transparent : Colors.White, 0),
                new GradientStop(Colors.White, fades.Start),
                new GradientStop(Colors.White, 1 - fades.End),
                new GradientStop(fades.End > 0 ? Colors.Transparent : Colors.White, 1),
            },
        };
    }
}
