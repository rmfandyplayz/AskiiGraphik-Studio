// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;

namespace AskiiArt.App.Tests.Shell.Docking;

// Plain calculations, so these run on xUnit's own threads. They're apart from ToolbarFrameTests because that class's
// Dispose pumps the UI thread's dispatcher, which from any other thread takes jobs from whichever test is running on
// the UI thread at the time, and fails.
public sealed class ToolbarFadesTests
{
    [Theory]
    [InlineData(0, 300, 100, 20, 0, 0.2)]
    [InlineData(100, 300, 100, 20, 0.2, 0.2)]
    [InlineData(200, 300, 100, 20, 0.2, 0)]
    [InlineData(0, 100, 100, 20, 0, 0)]
    [InlineData(0, 50, 100, 20, 0, 0)]
    [InlineData(0, 300, 30, 20, 0, 0.5)]
    [InlineData(0.2, 300, 100, 20, 0, 0.2)]
    [InlineData(199.8, 300, 100, 20, 0.2, 0)]
    [InlineData(0, 300, 0, 20, 0, 0)]
    public void AFade_ShowsOnEachSideWithMoreToScrollTo(double offset, double extent, double viewport, double fadeLength,
        double expectedStart, double expectedEnd)
    {
        var fades = ToolbarFades.For(offset, extent, viewport, fadeLength);

        Assert.Equal(expectedStart, fades.Start, precision: 6);
        Assert.Equal(expectedEnd, fades.End, precision: 6);
    }

    [Fact]
    public void NoFades_MeansNoMask()
    {
        Assert.Null(ToolbarFades.ToMask(default, isHorizontal: true));
    }
}
