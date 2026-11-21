// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;

namespace AskiiArt.Core.Tests.Tools;

public sealed class ToolMarkerReachTests
{
    private static readonly CanvasPoint _marker = new(2, 1);

    // Cells about twice as tall as they're wide: a round marker reaches twice as far across, in cells, as down.
    private readonly ToolMarkerReach _reach = new(Across: 1, Down: 0.5);

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(2, 1.5)]
    [InlineData(2.5, 1.25)]
    public void APointOnTheMarker_IsCovered(double x, double y) => Assert.True(_reach.Covers(_marker, new(x, y)));

    [Theory]
    [InlineData(3.1, 1)]
    [InlineData(2, 1.6)]
    [InlineData(2.8, 1.4)]
    public void APointOffTheMarker_IsNot(double x, double y) => Assert.False(_reach.Covers(_marker, new(x, y)));

    [Fact]
    public void WithNoReach_NothingIsCovered() => Assert.False(default(ToolMarkerReach).Covers(_marker, _marker));
}
