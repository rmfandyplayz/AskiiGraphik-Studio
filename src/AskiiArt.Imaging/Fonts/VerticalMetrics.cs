// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Buffers.Binary;
using SkiaSharp;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// A font's ascent, descent and line gap as fractions of the em size, read straight from its
/// OpenType tables. Each operating system's font engine picks different tables (Windows prefers
/// the "win" values, macOS and Linux the "hhea" ones), so reading them ourselves keeps the cell
/// height identical everywhere (ADR 0013).
/// </summary>
internal readonly record struct VerticalMetrics(float AscentPerEm, float DescentPerEm, float LineGapPerEm)
{
    private const uint HheaTableTag = 0x68686561; // "hhea"
    private const uint Os2TableTag = 0x4F532F32; // "OS/2"

    // Offsets into the tables, from the OpenType specification.
    private const int HheaAscenderOffset = 4;
    private const int HheaDescenderOffset = 6;
    private const int HheaLineGapOffset = 8;
    private const int Os2SelectionFlagsOffset = 62;
    private const int Os2TypoAscenderOffset = 68;
    private const int Os2TypoDescenderOffset = 70;
    private const int Os2TypoLineGapOffset = 72;

    // When a font sets this OS/2 flag, its designer asks for the "typo" values to be used.
    private const ushort UseTypoMetricsFlag = 1 << 7;

    /// <summary>
    /// Follows the same rule as FreeType and browsers: the OS/2 "typo" values when the font asks
    /// for them, otherwise the "hhea" values. Returns null when the font has neither table.
    /// </summary>
    public static VerticalMetrics? Read(SKTypeface typeface)
    {
        float unitsPerEm = typeface.UnitsPerEm;
        if (unitsPerEm <= 0)
            return null;

        if (typeface.TryGetTableData(Os2TableTag, out var os2)
            && os2.Length >= Os2TypoLineGapOffset + sizeof(short)
            && (ReadUInt16(os2, Os2SelectionFlagsOffset) & UseTypoMetricsFlag) != 0)
        {
            return FromDesignUnits(ReadInt16(os2, Os2TypoAscenderOffset), ReadInt16(os2, Os2TypoDescenderOffset),
                ReadInt16(os2, Os2TypoLineGapOffset), unitsPerEm);
        }

        if (typeface.TryGetTableData(HheaTableTag, out var hhea) && hhea.Length >= HheaLineGapOffset + sizeof(short))
        {
            return FromDesignUnits(ReadInt16(hhea, HheaAscenderOffset), ReadInt16(hhea, HheaDescenderOffset),
                ReadInt16(hhea, HheaLineGapOffset), unitsPerEm);
        }

        return null;
    }

    /// <summary>The engine's own values, for the rare font without OpenType tables.</summary>
    public static VerticalMetrics FromSkia(SKFontMetrics metrics, float pixelSize) =>
        new(-metrics.Ascent / pixelSize, metrics.Descent / pixelSize, Math.Max(0, metrics.Leading) / pixelSize);

    // Descenders are stored as negative numbers (below the baseline).
    private static VerticalMetrics FromDesignUnits(short ascender, short descender, short lineGap, float unitsPerEm) =>
        new(ascender / unitsPerEm, -descender / unitsPerEm, Math.Max((short)0, lineGap) / unitsPerEm);

    private static short ReadInt16(byte[] table, int offset) => BinaryPrimitives.ReadInt16BigEndian(table.AsSpan(offset));

    private static ushort ReadUInt16(byte[] table, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(offset));
}
