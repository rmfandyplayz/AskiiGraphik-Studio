// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class GlyphBatchTests
{
    [Fact]
    public void Add_PastItsFirstArrays_KeepsEveryGlyphAndTheArraysTheSameLength()
    {
        using var batch = new GlyphBatch(pageIndex: 0);
        var count = batch.Sprites.Length + 1;

        for (var index = 0; index < count; index++)
            batch.Add(new SKRectI(index, 0, index + 1, 1), index, 0, new SKColor((uint)index));

        // DrawAtlas throws unless all three arrays are the same length.
        Assert.Equal(count, batch.Count);
        Assert.Equal(batch.Sprites.Length, batch.Transforms.Length);
        Assert.Equal(batch.Sprites.Length, batch.Colors.Length);
        Assert.Equal(new SKRect(0, 0, 1, 1), batch.Sprites[0]);
        Assert.Equal(new SKColor((uint)(count - 1)), batch.Colors[count - 1]);
    }

    [Fact]
    public void Complete_ClearsTheUnusedTailOfReusedArrays()
    {
        // Fill a batch's arrays, then hand them back to the pool for the next batch on this thread.
        using (var first = new GlyphBatch(pageIndex: 0))
        {
            for (var index = 0; index < first.Sprites.Length; index++)
                first.Add(new SKRectI(0, 0, 1, 1), index, 0, SKColors.Red);
        }

        var page = new SharedImage(SKImage.FromPixelCopy(new SKImageInfo(1, 1, SKColorType.Alpha8), new byte[1]));
        using var second = new GlyphBatch(pageIndex: 0);
        second.Add(new SKRectI(0, 0, 1, 1), 0, 0, SKColors.Blue);
        second.Complete(page);
        page.Release();

        Assert.All(second.Sprites.Skip(1), sprite => Assert.True(sprite.IsEmpty));
        Assert.All(second.Colors.Skip(1), color => Assert.Equal(SKColors.Empty, color));
    }
}
