// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Lines;

/// <summary>How heavy one arm of a line character is (ADR 0081).</summary>
internal enum LineWeight : byte
{
    None,
    Light,
    Heavy,
    Double
}

/// <summary>
/// The arms of a line character (ADR 0081): the lines it draws from the middle of its cell to each of the cell's
/// sides, and how heavy each is. <c>─</c> has light arms left and right, <c>┏</c> heavy arms right and down.
/// </summary>
internal readonly record struct LineArms(LineWeight Up, LineWeight Right, LineWeight Down, LineWeight Left)
{
    /// <summary>Whether it has an arm to the left or the right.</summary>
    public bool HasAcross => Left != LineWeight.None || Right != LineWeight.None;

    /// <summary>Whether it has an arm up or down.</summary>
    public bool HasUpright => Up != LineWeight.None || Down != LineWeight.None;

    /// <summary>The weight of its heaviest arm, double counting as the heaviest.</summary>
    public LineWeight Heaviest => (LineWeight)Math.Max(Math.Max((int)Up, (int)Right), Math.Max((int)Down, (int)Left));

    /// <summary>The arms this has, at their own weights, with those it lacks taken from <paramref name="under"/>.</summary>
    public LineArms Over(LineArms under) => new(
        Up != LineWeight.None ? Up : under.Up,
        Right != LineWeight.None ? Right : under.Right,
        Down != LineWeight.None ? Down : under.Down,
        Left != LineWeight.None ? Left : under.Left);

    /// <summary>The same arms, every one at <paramref name="weight"/>.</summary>
    public LineArms At(LineWeight weight) => new(
        Up != LineWeight.None ? weight : LineWeight.None,
        Right != LineWeight.None ? weight : LineWeight.None,
        Down != LineWeight.None ? weight : LineWeight.None,
        Left != LineWeight.None ? weight : LineWeight.None);

    /// <summary>
    /// The arms left once those pointing away from a neighbour <paramref name="across"/> columns and
    /// <paramref name="down"/> rows away are taken off: only the signs count, so a neighbour down and to the right
    /// takes off the arms up and to the left.
    /// </summary>
    public LineArms WithoutArmsAwayFrom(int across, int down) => new(
        down > 0 ? LineWeight.None : Up,
        across < 0 ? LineWeight.None : Right,
        down < 0 ? LineWeight.None : Down,
        across > 0 ? LineWeight.None : Left);
}
