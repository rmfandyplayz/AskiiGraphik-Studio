// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

public sealed class BitRowTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 61)]
    [InlineData(60, 10)]
    [InlineData(0, 130)]
    [InlineData(127, 2)]
    public void SetRange_SetsExactlyThoseBits(int start, int length)
    {
        var row = new ulong[BitRow.WordsFor(140)];

        BitRow.SetRange(row, start, length);

        for (var index = 0; index < 140; index++)
            Assert.Equal(index >= start && index < start + length, BitRow.IsSet(row, index));

        Assert.Equal(length, BitRow.CountSetBits(row));
    }

    [Fact]
    public void Runs_FindEachStretchOfSetBits_AcrossWords()
    {
        var row = new ulong[BitRow.WordsFor(200)];
        BitRow.SetRange(row, 0, 2);
        BitRow.SetRange(row, 62, 5);
        BitRow.SetRange(row, 190, 10);

        List<(int Start, int Length)> runs = [];
        foreach (var run in BitRow.Runs(row, 200))
            runs.Add(run);

        Assert.Equal([(0, 2), (62, 5), (190, 10)], runs);
    }

    [Fact]
    public void NextSetAndClearBit_StopAtTheLength()
    {
        var row = new ulong[BitRow.WordsFor(70)];
        BitRow.SetRange(row, 65, 5);

        Assert.Equal(65, BitRow.NextSetBit(row, 0, 70));
        Assert.Equal(66, BitRow.NextSetBit(row, 66, 70));
        Assert.Equal(70, BitRow.NextClearBit(row, 65, 70));
        Assert.Equal(70, BitRow.NextSetBit(row, 70, 70));
    }

    [Fact]
    public void WordsFor_RoundsUp()
    {
        Assert.Equal(0, BitRow.WordsFor(0));
        Assert.Equal(1, BitRow.WordsFor(1));
        Assert.Equal(1, BitRow.WordsFor(64));
        Assert.Equal(2, BitRow.WordsFor(65));
    }
}
