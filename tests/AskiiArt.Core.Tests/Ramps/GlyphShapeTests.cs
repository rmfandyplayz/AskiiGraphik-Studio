// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class GlyphShapeTests
{
    [Fact]
    public void AShape_KnowsEachPartsInk_RowByRowFromTheTop()
    {
        var ink = Enumerable.Range(0, GlyphShape.PartCount).Select(part => part / 100.0).ToArray();

        var shape = new GlyphShape(ink);

        Assert.Equal(0.00, shape.InkAt(0, 0));
        Assert.Equal(0.03, shape.InkAt(3, 0));
        Assert.Equal(0.04, shape.InkAt(0, 1));
        Assert.Equal(0.31, shape.InkAt(3, 7));
        Assert.Equal(ink, shape.Parts.ToArray());
    }

    [Fact]
    public void AShapesInk_IsHowMuchOfTheWholeCellItCovers()
    {
        var shape = TestShapes.Bands(top: 0, bottom: 0.5);

        Assert.Equal(0.25, shape.Ink, 1e-12);
    }

    [Fact]
    public void AnEvenShape_CoversEveryPartAlike()
    {
        var shape = GlyphShape.Even(0.4);

        Assert.All([.. shape.Parts], part => Assert.Equal(0.4, part));
        Assert.Equal(0.4, shape.Ink, 1e-12);
    }

    [Fact]
    public void TheEmptyShape_HasNoInk()
    {
        Assert.Equal(0, GlyphShape.Empty.Ink);
        Assert.All([.. GlyphShape.Empty.Parts], part => Assert.Equal(0, part));
    }

    [Theory]
    [InlineData(GlyphShape.PartCount - 1)]
    [InlineData(GlyphShape.PartCount + 1)]
    public void AShapeWithTheWrongNumberOfParts_IsRejected(int partCount)
    {
        Assert.Throws<ArgumentException>(() => new GlyphShape(new double[partCount]));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void InkOutsideZeroToOne_IsRejected(double ink)
    {
        var parts = new double[GlyphShape.PartCount];
        parts[5] = ink;

        Assert.Throws<ArgumentException>(() => new GlyphShape(parts));
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphShape.Even(ink));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(GlyphShape.Columns, 0)]
    [InlineData(0, -1)]
    [InlineData(0, GlyphShape.Rows)]
    public void APartOutsideTheCell_IsRejected(int column, int row)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GlyphShape.Empty.InkAt(column, row));
    }
}
