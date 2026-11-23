// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>
/// One way the Brush and the Soft Eraser can pick edge glyphs, for step 4.10a's comparison (ADR 0074). Temporary: step
/// 4.10b keeps the one the owner picks and removes this.
/// </summary>
/// <param name="Name">What the comparison sheets and the Debug menu call it.</param>
/// <param name="ShapesEdges">
/// Whether edge cells are shaped at all (ADR 0068). Without it, each cell takes the tip's weight at its middle, as
/// before step 4.7.
/// </param>
/// <param name="CountsShapeAgainstFaintness">
/// Idea 1: where the ink lies counts for more the fainter the edge is, so a faint edge keeps its ink on the right side.
/// </param>
/// <param name="LeavesWrongSideEmpty">
/// Idea 2: a glyph whose ink lies on the wrong side of an edge is never picked, so the cell stays as it was or empty.
/// </param>
/// <param name="LooksAtNeighbours">
/// Idea 3: an edge cell is judged with the cells around it, as they're painted or already were, so ink lines up across
/// cells.
/// </param>
/// <param name="FallsBackWhenUnclear">
/// The fix after 4.10a's testing: where no glyph clearly shows an edge cell's edge, the cell is weighed as a whole at
/// its middle, as step 4.6 weighed it, so straight sides and faintly grazed cells don't get stray marks.
/// </param>
public sealed record PatsVariant(
    string Name,
    bool ShapesEdges,
    bool CountsShapeAgainstFaintness,
    bool LeavesWrongSideEmpty,
    bool LooksAtNeighbours,
    bool FallsBackWhenUnclear = false)
{
    /// <summary>Step 4.6's brush: no shaping.</summary>
    public static PatsVariant Unshaped { get; } = new("4.6", false, false, false, false);

    /// <summary>Step 4.7's PATS brush, which is what the Brush paints with unless the Debug menu says otherwise.</summary>
    public static PatsVariant Shipped { get; } = new("4.7", true, false, false, false);

    /// <summary>Every variant the comparison shows, in its order.</summary>
    public static IReadOnlyList<PatsVariant> All { get; } =
    [
        Unshaped,
        Shipped,
        new("1", true, true, false, false),
        new("2", true, false, true, false),
        new("3", true, false, false, true),
        new("1+3", true, true, false, true),
        new("1+2+3", true, true, true, true),
        new("3+fix", true, false, false, true, true),
        new("1+2+3+fix", true, true, true, true, true)
    ];

    /// <summary>The variants the second round of sheets compares, after 4.10a's testing, in its order.</summary>
    public static IReadOnlyList<PatsVariant> SecondRound { get; } = [All[0], All[4], All[7], All[6], All[8]];
}
