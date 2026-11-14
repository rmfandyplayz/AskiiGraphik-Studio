// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// The colours the canvas draws a cell's colours in (ADR 0036): an ANSI colour as the VGA palette
/// shows it, an RGB colour as it is, and an unset one in the theme's ink, or as no background, so
/// the paper shows. Only the unset ones change with the theme.
/// </summary>
internal static class CellColorRendering
{
    // Indexed by the ANSI colour's number, which is its escape-code order.
    private static readonly SKColor[] _ansiColors =
        [.. Enum.GetValues<AnsiColor>().Order().Select(color => FromRgb(AnsiPalette.Vga[color]))];

    /// <summary>The colour to draw a glyph in.</summary>
    public static SKColor TextColor(CellColor color, SKColor ink) => Resolve(color, ink);

    /// <summary>The colour to fill a cell's background with: transparent when it has none.</summary>
    public static SKColor BackgroundColor(CellColor color) => Resolve(color, SKColors.Transparent);

    private static SKColor Resolve(CellColor color, SKColor unset)
    {
        if (color.TryGetAnsi(out var ansi))
            return _ansiColors[(int)ansi];

        if (color.TryGetRgb(out var rgb))
            return FromRgb(rgb);

        return unset;
    }

    private static SKColor FromRgb(RgbColor color) => new(color.Red, color.Green, color.Blue);
}
