// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// How far the selection's moving lines have gone at one moment (ADR 0061), each in device pixels within one repeat of
/// its pattern: the dashes a whole pixel at a time, and the hatching a step of a pixel at a time, so it moves smoothly
/// (ADR 0062). The canvas repaints only once one of them has moved on.
/// </summary>
/// <param name="DashRun">How far the bright outlines' dashes have run round them.</param>
/// <param name="UnselectedHatchShift">
/// How far the hatching over cells that aren't selected has moved along a row, to the left. For its lines, which rise
/// to the right, that looks like moving to the top left.
/// </param>
/// <param name="LostHatchShift">
/// How far the hatching over cells about to be deselected has moved along a row, to the left. For its lines, which
/// fall to the right, that looks like moving to the bottom left.
/// </param>
internal readonly record struct SelectionMotion(int DashRun, double UnselectedHatchShift, double LostHatchShift)
{
    /// <summary>
    /// The motion of <paramref name="display"/>'s lines once the dashes have run for <paramref name="time"/> and the
    /// hatching over cells that aren't selected for <paramref name="unselectedHatchTime"/>. The dashes run at
    /// <paramref name="dashSpeed"/> device pixels a second, with dashes and gaps <paramref name="dashLength"/> long,
    /// and the hatching moves across its lines at its speed in <paramref name="look"/> times theirs, in its steps.
    /// </summary>
    public static SelectionMotion At(TimeSpan time, TimeSpan unselectedHatchTime, SelectionDisplay? display,
        SelectionLook look, double dashSpeed, int dashLength)
    {
        var dashPeriod = 2 * Math.Max(1, dashLength);
        var dashRun = (int)(time.TotalSeconds * dashSpeed % dashPeriod);
        var unselectedShift = HatchShift(unselectedHatchTime, look.HatchSpeed * dashSpeed, look);
        var lostShift = 0.0;
        if (display is { Lost.IsEmpty: false })
            lostShift = HatchShift(time, look.LostHatchSpeedOf(display.LostStyle) * dashSpeed, look);

        return new SelectionMotion(dashRun, unselectedShift, lostShift);
    }

    // Lines at 45 degrees moving straight across them at a speed move along a row √2 times as fast. The shift is
    // rounded down to a whole step, so the canvas repaints no more often than the hatching takes a step.
    private static double HatchShift(TimeSpan time, double speed, SelectionLook look)
    {
        var shift = time.TotalSeconds * Math.Max(0, speed) * Math.Sqrt(2) % look.HatchTilePeriod;
        var steps = look.HatchSteps;
        return Math.Floor(shift * steps) / steps;
    }
}
