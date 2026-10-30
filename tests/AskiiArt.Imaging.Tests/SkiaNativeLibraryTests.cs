// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.Imaging.Tests;

// SkiaSharp is a wrapper around a native library that ships separately for each OS. This catches
// a missing native library (for example on Linux CI) before any real Imaging code depends on it.
public sealed class SkiaNativeLibraryTests
{
    [Fact]
    public void Skia_CanDrawToABitmapOnThisOperatingSystem()
    {
        using var bitmap = new SKBitmap(2, 2);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Black);

        Assert.Equal(SKColors.Black, bitmap.GetPixel(1, 1));
    }
}
