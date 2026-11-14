// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class StrokeSnapTests
{
    [Fact]
    public void Apply_KeepsTheCellsEdgesWhereTheyAre()
    {
        var snap = StrokeSnap.Create([[(6.3f, 7.2f)]], 16);

        Assert.Equal(0, snap.Apply(0));
        Assert.Equal(16, snap.Apply(16));
    }

    [Fact]
    public void Apply_MovesAStrokeOntoTheNearestWholePixels()
    {
        var snap = StrokeSnap.Create([[(6.3f, 7.2f)]], 16);

        Assert.Equal(6, snap.Apply(6.3f));
        Assert.Equal(7, snap.Apply(7.2f));
    }

    [Fact]
    public void Apply_MakesAStrokeThinnerThanAPixelAPixelThick()
    {
        var snap = StrokeSnap.Create([[(6.4f, 6.6f)]], 16);

        Assert.Equal(1, snap.Apply(6.6f) - snap.Apply(6.4f));
    }

    [Fact]
    public void Apply_KeepsTheTwoStrokesOfADoubleLineApart()
    {
        // As in IBM Plex Mono at 5.28 px: the double line's inner edges are the single line's outer edges, the heavy
        // line lies in between, and the whole double line is under two pixels tall.
        var snap = StrokeSnap.Create([[(3.32f, 3.68f)], [(3.13f, 3.87f)], [(2.95f, 3.32f), (3.68f, 4.05f)]], 7);

        float[] doubleLineEdges = [snap.Apply(2.95f), snap.Apply(3.32f), snap.Apply(3.68f), snap.Apply(4.05f)];
        Assert.Equal([2, 3, 4, 5], doubleLineEdges);
        Assert.Equal(1, snap.Apply(3.68f) - snap.Apply(3.32f));
    }

    [Fact]
    public void Apply_StretchesWhatLiesBetweenEdgesEvenly()
    {
        var snap = StrokeSnap.Create([[(6.3f, 7.2f)]], 16);

        Assert.Equal(3, snap.Apply(3.15f), 0.001f);
    }

    [Fact]
    public void Apply_IgnoresShapesThatReachTheCellsEdge()
    {
        var snap = StrokeSnap.Create([[(0, 3.3f)], [(12.7f, 16)]], 16);

        Assert.Equal(3.3f, snap.Apply(3.3f));
        Assert.Equal(12.7f, snap.Apply(12.7f));
    }

    [Fact]
    public void Apply_MovesPositionsOutsideTheCellWithItsNearestEdge()
    {
        var snap = StrokeSnap.Create([[(6.3f, 7.2f)]], 16);

        Assert.Equal(-2, snap.Apply(-2));
        Assert.Equal(18, snap.Apply(18));
    }
}
