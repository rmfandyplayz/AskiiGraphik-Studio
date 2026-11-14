// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Which colours a document may hold, chosen per document like its character set (ARCHITECTURE
/// §5.11, ADR 0036). A document never holds a colour its mode doesn't allow.
/// </summary>
public enum ColorMode
{
    /// <summary>No colours: every cell is drawn in the canvas's own ink and paper.</summary>
    Monochrome = 0,

    /// <summary>The 16 standard ANSI colours.</summary>
    Ansi = 1,

    /// <summary>Any 24-bit RGB colour.</summary>
    Rgb = 2
}

/// <summary>The rules behind each <see cref="ColorMode"/>.</summary>
public static class ColorModeRules
{
    /// <summary>Whether a document in <paramref name="colorMode"/> may hold <paramref name="color"/>.</summary>
    public static bool Allows(this ColorMode colorMode, CellColor color) => colorMode switch
    {
        ColorMode.Monochrome => color.IsUnset,
        ColorMode.Ansi => color.Kind is CellColorKind.Unset or CellColorKind.Ansi,
        ColorMode.Rgb => color.Kind is CellColorKind.Unset or CellColorKind.Rgb,
        _ => throw new ArgumentOutOfRangeException(nameof(colorMode), colorMode, null)
    };

    /// <summary>
    /// Whether a document in <paramref name="colorMode"/> may hold both of <paramref name="cell"/>'s colours.
    /// </summary>
    public static bool Allows(this ColorMode colorMode, Cell cell) =>
        colorMode.Allows(cell.Foreground) && colorMode.Allows(cell.Background);

    /// <summary>
    /// The colour <paramref name="colorMode"/> allows that's closest to <paramref name="color"/>:
    /// unset for monochrome, the nearest of the 16 for ANSI, and the colour the ANSI one is shown
    /// with for RGB. Both use the <see cref="AnsiPalette.Vga"/> palette.
    /// </summary>
    public static CellColor Convert(this ColorMode colorMode, CellColor color)
    {
        if (colorMode.Allows(color))
            return color;

        if (colorMode == ColorMode.Ansi && color.TryGetRgb(out var rgb))
            return CellColor.FromAnsi(AnsiPalette.Vga.FindNearest(rgb));

        if (colorMode == ColorMode.Rgb && color.TryGetAnsi(out var ansi))
            return CellColor.FromRgb(AnsiPalette.Vga[ansi]);

        return CellColor.Unset;
    }

    /// <summary>
    /// <paramref name="cell"/> with both colours converted (see <see cref="Convert(ColorMode, CellColor)"/>).
    /// A cell holding only a background that's removed becomes empty.
    /// </summary>
    public static Cell Convert(this ColorMode colorMode, Cell cell) =>
        cell.WithForeground(colorMode.Convert(cell.Foreground)).WithBackground(colorMode.Convert(cell.Background));

    /// <summary>
    /// Whether converting <paramref name="color"/> to <paramref name="colorMode"/> changes how it
    /// looks: true when the colour is removed, or when an RGB colour isn't exactly one of the 16.
    /// Turning an ANSI colour into RGB never does.
    /// </summary>
    public static bool ConversionChangesTheLookOf(this ColorMode colorMode, CellColor color)
    {
        if (colorMode.Allows(color) || colorMode == ColorMode.Rgb)
            return false;

        if (colorMode == ColorMode.Ansi && color.TryGetRgb(out var rgb))
            return AnsiPalette.Vga[AnsiPalette.Vga.FindNearest(rgb)] != rgb;

        return true;
    }
}
