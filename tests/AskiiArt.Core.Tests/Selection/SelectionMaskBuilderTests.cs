// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

public sealed class SelectionMaskBuilderTests
{
    private static readonly CanvasSize _size = new(6, 2);

    [Fact]
    public void RemoveRun_LeavesOutThoseCells_AndOnlyThose()
    {
        var builder = new SelectionMaskBuilder(_size);
        builder.AddRun(0, 0, 6);
        builder.AddRun(1, 0, 6);

        builder.RemoveRun(0, 2, 3);
        builder.RemoveRun(1, -5, 6);

        Assert.Equal(["##···#", "·#####"], TestMasks.Describe(builder.Build()));
    }

    [Fact]
    public void IntersectWith_KeepsTheCellsBothHave()
    {
        var area = new CellRect(1, 0, 4, 2);
        var builder = new SelectionMaskBuilder(_size, area);
        builder.AddRun(0, 1, 3);
        builder.AddRun(1, 1, 4);
        var other = new SelectionMaskBuilder(_size, area);
        other.AddRun(0, 2, 3);
        other.Add(new CellPoint(4, 1));

        builder.IntersectWith(other);

        Assert.Equal(["··##··", "····#·"], TestMasks.Describe(builder.Build()));
    }

    [Fact]
    public void IntersectWith_ABuilderOverAnotherArea_Throws()
    {
        var builder = new SelectionMaskBuilder(_size, new CellRect(0, 0, 3, 2));

        Assert.Throws<ArgumentException>(() => builder.IntersectWith(new SelectionMaskBuilder(_size)));
    }
}
