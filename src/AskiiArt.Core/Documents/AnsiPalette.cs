// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The RGB values the 16 ANSI colours are shown with, and converted to and from (ADR 0036).
/// Every terminal has its own; <see cref="Vga"/> is the one classic ANSI art was drawn for.
/// </summary>
public sealed class AnsiPalette
{
    // Only the order of distances matters, so these are the redmean formula's weights without
    // the factors all three share: (512 + mean red) / 256 for red, 4 for green, and
    // (767 - mean red) / 256 for blue.
    private const int RedWeightBase = 512;
    private const int BlueWeightBase = 767;
    private const int GreenWeight = 4;
    private const int WeightShift = 8;

    private readonly RgbColor[] _colors;

    private AnsiPalette(RgbColor[] colors)
    {
        _colors = colors;
    }

    /// <summary>The IBM VGA colours.</summary>
    public static AnsiPalette Vga { get; } = new([
        new(0x00, 0x00, 0x00), new(0xAA, 0x00, 0x00), new(0x00, 0xAA, 0x00), new(0xAA, 0x55, 0x00),
        new(0x00, 0x00, 0xAA), new(0xAA, 0x00, 0xAA), new(0x00, 0xAA, 0xAA), new(0xAA, 0xAA, 0xAA),
        new(0x55, 0x55, 0x55), new(0xFF, 0x55, 0x55), new(0x55, 0xFF, 0x55), new(0xFF, 0xFF, 0x55),
        new(0x55, 0x55, 0xFF), new(0xFF, 0x55, 0xFF), new(0x55, 0xFF, 0xFF), new(0xFF, 0xFF, 0xFF)
    ]);

    /// <summary>The RGB value <paramref name="color"/> is shown with.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There's no such ANSI colour.</exception>
    public RgbColor this[AnsiColor color]
    {
        get
        {
            if (!Enum.IsDefined(color))
                throw new ArgumentOutOfRangeException(nameof(color), color, null);

            return _colors[(int)color];
        }
    }

    /// <summary>
    /// The ANSI colour that looks closest to <paramref name="color"/>. Closeness is the "redmean"
    /// distance, a cheap approximation of how different two colours look to people. A tie goes to
    /// the lower-numbered colour.
    /// </summary>
    public AnsiColor FindNearest(RgbColor color)
    {
        var nearest = 0;
        var nearestDistance = int.MaxValue;
        for (var index = 0; index < _colors.Length; index++)
        {
            var distance = Distance(color, _colors[index]);
            if (distance < nearestDistance)
            {
                nearest = index;
                nearestDistance = distance;
            }
        }

        return (AnsiColor)nearest;
    }

    private static int Distance(RgbColor first, RgbColor second)
    {
        var meanRed = (first.Red + second.Red) / 2;
        var red = first.Red - second.Red;
        var green = first.Green - second.Green;
        var blue = first.Blue - second.Blue;

        return (((RedWeightBase + meanRed) * red * red) >> WeightShift) + (GreenWeight * green * green)
               + (((BlueWeightBase - meanRed) * blue * blue) >> WeightShift);
    }
}
