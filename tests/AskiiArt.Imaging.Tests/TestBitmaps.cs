// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.Imaging.Tests;

/// <summary>Pixel helpers for rasterizer and atlas tests. "Ink" is any pixel with some alpha.</summary>
internal static class TestBitmaps
{
    public static SKBitmap CreateTransparent(int width, int height, SKColorType colorType = SKColorType.Rgba8888)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, colorType, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);
        return bitmap;
    }

    public static int CountInk(SKBitmap bitmap, SKRectI region)
    {
        var count = 0;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 0)
                    count++;
            }
        }

        return count;
    }

    public static int CountInk(SKBitmap bitmap) => CountInk(bitmap, SKRectI.Create(0, 0, bitmap.Width, bitmap.Height));

    /// <summary>The smallest rectangle holding every inked pixel in the region, or empty when there's none.</summary>
    public static SKRectI InkBounds(SKBitmap bitmap, SKRectI region)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha == 0)
                    continue;

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
            }
        }

        return left == int.MaxValue ? SKRectI.Empty : new SKRectI(left, top, right, bottom);
    }

    /// <summary>The alpha values of a region, row by row.</summary>
    public static byte[] Alphas(SKBitmap bitmap, SKRectI region)
    {
        var alphas = new byte[region.Width * region.Height];
        for (var y = 0; y < region.Height; y++)
        {
            for (var x = 0; x < region.Width; x++)
                alphas[y * region.Width + x] = bitmap.GetPixel(region.Left + x, region.Top + y).Alpha;
        }

        return alphas;
    }
}
