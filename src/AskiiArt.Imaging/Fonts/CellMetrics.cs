// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// The size of one grid cell, in device pixels, for a font at a given size (ARCHITECTURE §11,
/// ADR 0013). The width is the advance of <see cref="ReferenceCharacter"/> and the height is the
/// font's line height (ascent + descent + line gap), each rounded to whole pixels. Both come
/// from the font file without hinting, so they're the same on every operating system.
/// </summary>
public readonly record struct CellMetrics
{
    /// <summary>The glyph whose advance sets the cell width; CSS's <c>ch</c> unit uses the same one.</summary>
    public const char ReferenceCharacter = '0';

    /// <summary>The smallest font size, in device pixels.</summary>
    public const float MinimumPixelSize = 1;

    /// <summary>The largest font size, in device pixels.</summary>
    public const float MaximumPixelSize = 1024;

    /// <summary>
    /// The widest or tallest a usable cell can be, in ems. Real fonts stay well under 3; a
    /// damaged file can claim thousands.
    /// </summary>
    internal const float MaximumSideInEms = 8;

    // Used when a font has no reference glyph and no average width: a typical monospace ratio.
    private const float FallbackAdvancePerEm = 0.6f;

    // Per-em measurements don't depend on the size, so any size works for checking them.
    private const float CheckingPixelSize = 100;

    private CellMetrics(float pixelSize, int width, int height, int baseline)
    {
        PixelSize = pixelSize;
        Width = width;
        Height = height;
        Baseline = baseline;
    }

    /// <summary>The font size (the em size) in device pixels.</summary>
    public float PixelSize { get; }

    /// <summary>The cell width in device pixels.</summary>
    public int Width { get; }

    /// <summary>The cell height in device pixels.</summary>
    public int Height { get; }

    /// <summary>The distance from the cell's top edge down to the text baseline, in device pixels.</summary>
    public int Baseline { get; }

    /// <summary>Measures the cell size of <paramref name="font"/> at <paramref name="pixelSize"/>.</summary>
    public static CellMetrics Measure(LoadedFont font, float pixelSize)
    {
        ArgumentNullException.ThrowIfNull(font);
        ValidatePixelSize(pixelSize);

        using var skFont = CreateMeasuringFont(font, pixelSize);
        var vertical = ReadVerticalMetrics(font, skFont);
        var ascent = vertical.AscentPerEm * pixelSize;
        var descent = vertical.DescentPerEm * pixelSize;
        var lineGap = vertical.LineGapPerEm * pixelSize;

        // Half the line gap goes above the text and half below, so the text sits in the middle
        // of its line.
        var height = RoundToWholePixels(ascent + descent + lineGap);
        var baseline = Math.Clamp(RoundToInt(lineGap / 2 + ascent), 0, height);
        var width = RoundToWholePixels(MeasureReferenceAdvance(font, skFont));

        return new CellMetrics(pixelSize, width, height, baseline);
    }

    /// <summary>
    /// Whether <paramref name="font"/>'s cell is more than zero and at most
    /// <see cref="MaximumSideInEms"/> wide and tall.
    /// </summary>
    internal static bool HasUsableSize(LoadedFont font)
    {
        using var skFont = CreateMeasuringFont(font, CheckingPixelSize);
        var vertical = ReadVerticalMetrics(font, skFont);
        var heightInEms = vertical.AscentPerEm + vertical.DescentPerEm + vertical.LineGapPerEm;
        var widthInEms = MeasureReferenceAdvance(font, skFont) / CheckingPixelSize;

        return IsUsableSide(heightInEms) && IsUsableSide(widthInEms);
    }

    /// <summary>
    /// Creates an SKFont for measuring: unhinted, with linear metrics, so measurements are the
    /// same on every operating system.
    /// </summary>
    internal static SKFont CreateMeasuringFont(LoadedFont font, float pixelSize) =>
        new(font.Typeface, pixelSize)
        {
            Hinting = SKFontHinting.None,
            LinearMetrics = true,
            Subpixel = true
        };

    internal static void ValidatePixelSize(float pixelSize)
    {
        if (!float.IsFinite(pixelSize) || pixelSize < MinimumPixelSize || pixelSize > MaximumPixelSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize), pixelSize,
                $"The pixel size must be between {MinimumPixelSize} and {MaximumPixelSize}.");
        }
    }

    /// <summary>The unrounded cell width: the advance of <see cref="ReferenceCharacter"/> in <paramref name="skFont"/>.</summary>
    internal static float MeasureReferenceAdvance(LoadedFont font, SKFont skFont)
    {
        if (font.Typeface.ContainsGlyph(ReferenceCharacter))
            return skFont.MeasureText(ReferenceCharacter.ToString());

        var averageWidth = skFont.Metrics.AverageCharacterWidth;
        if (averageWidth > 0)
            return averageWidth;

        return skFont.Size * FallbackAdvancePerEm;
    }

    private static VerticalMetrics ReadVerticalMetrics(LoadedFont font, SKFont skFont) =>
        VerticalMetrics.Read(font.Typeface) ?? VerticalMetrics.FromSkia(skFont.Metrics, skFont.Size);

    private static bool IsUsableSide(float sizeInEms) =>
        float.IsFinite(sizeInEms) && sizeInEms > 0 && sizeInEms <= MaximumSideInEms;

    private static int RoundToWholePixels(float value) => Math.Max(1, RoundToInt(value));

    private static int RoundToInt(float value) => (int)MathF.Round(value, MidpointRounding.AwayFromZero);
}
