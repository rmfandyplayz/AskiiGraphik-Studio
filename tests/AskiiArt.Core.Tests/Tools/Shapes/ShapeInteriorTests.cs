// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Shapes;

namespace AskiiArt.Core.Tests.Tools.Shapes;

// The cells a closed outline closes in, which a filled rectangle or ellipse fills (ADR 0080).
public sealed class ShapeInteriorTests
{
    [Fact]
    public void ARectanglesOutline_ClosesInEverythingInsideIt()
    {
        var inside = Inside(
            "####",
            "#··#",
            "#··#",
            "####");

        Assert.Equal(
            ["····",
                "·##·",
                "·##·",
                "····"],
            inside);
    }

    // A step from cell to cell across a corner can't be crossed by stepping across, up or down.
    [Fact]
    public void AnOutlineThatStepsDiagonally_StillClosesItsInsideIn()
    {
        var inside = Inside(
            "··#··",
            "·#·#·",
            "#···#",
            "·#·#·",
            "··#··");

        Assert.Equal(
            ["·····",
                "··#··",
                "·###·",
                "··#··",
                "·····"],
            inside);
    }

    [Fact]
    public void AnOutlineWithAGap_ClosesNothingIn()
    {
        var inside = Inside(
            "####",
            "#···",
            "#··#",
            "####");

        Assert.All(inside, row => Assert.Equal("····", row));
    }

    // '#' marks the outline; the result marks the cells inside it.
    private static string[] Inside(params string[] rows)
    {
        var box = new CellRect(0, 0, rows[0].Length, rows.Length);
        var outline = new List<CellPoint>();
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == '#')
                    outline.Add(new CellPoint(x, y));
            }
        }

        var inside = ShapeInterior.Of(box, outline).ToHashSet();
        return
        [
            .. Enumerable.Range(0, box.Height).Select(y => new string(Enumerable.Range(0, box.Width)
                .Select(x => inside.Contains(new CellPoint(x, y)) ? '#' : '·').ToArray()))
        ];
    }
}
