// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>A size in logical pixels.</summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct LayoutSize(double Width, double Height);

/// <summary>A rectangle in logical pixels, measured from the top left of the space between the strips.</summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct LayoutRect(double X, double Y, double Width, double Height)
{
    /// <summary>The right edge.</summary>
    public double Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public double Bottom => Y + Height;

    /// <summary>Whether <paramref name="point"/> is inside it.</summary>
    public bool Contains(LayoutPoint point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;

    /// <summary>Whether it shares any area with <paramref name="other"/>. Only touching doesn't count.</summary>
    public bool Intersects(LayoutRect other) =>
        X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;
}

/// <summary>How much is taken from each edge of a rectangle, in logical pixels.</summary>
/// <param name="Left">From the left.</param>
/// <param name="Top">From the top.</param>
/// <param name="Right">From the right.</param>
/// <param name="Bottom">From the bottom.</param>
public readonly record struct LayoutInsets(double Left, double Top, double Right, double Bottom);
