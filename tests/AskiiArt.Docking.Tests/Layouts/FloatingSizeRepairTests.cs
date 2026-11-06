// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

/// <summary>Repairing what a layout remembers about sizes: floating windows' sizes, and widgets' lengths before a squeeze.</summary>
public sealed class FloatingSizeRepairTests
{
    [Fact]
    public void UnusableFloatingSizes_AndOnesForUnknownWindows_AreForgotten()
    {
        var layout = new DockingLayout([], [], [])
        {
            FloatingSizes =
            [
                new FloatingSize("w0", 300, 200),
                new FloatingSize("w0", 400, 400),
                new FloatingSize("w1", double.NaN, 200),
                new FloatingSize("w2", 300, -1),
                new FloatingSize("gone", 300, 200),
            ],
        };

        var repaired = LayoutRepair.Repair(layout, Catalog);

        Assert.Equal([new FloatingSize("w0", 300, 200)], repaired.FloatingSizes);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-5)]
    public void AnUnusableOpenLength_IsForgotten(double openLength)
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["w0"], 1, true, openLength), new WindowGroup(["w1"]));

        var repaired = LayoutRepair.Repair(layout, Catalog);

        Assert.Null(repaired.Side(SideEdge.Right)!.Columns[0].Groups[0].OpenLength);
    }

    [Fact]
    public void AGoodOpenLength_IsKept()
    {
        var layout = Widgets(SideEdge.Right, new WindowGroup(["w0"], 1, true, 250), new WindowGroup(["w1"]));

        var repaired = LayoutRepair.Repair(layout, Catalog);

        Assert.Equal(250, repaired.Side(SideEdge.Right)!.Columns[0].Groups[0].OpenLength);
    }
}
