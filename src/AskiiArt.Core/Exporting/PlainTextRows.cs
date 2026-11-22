// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Exporting;

/// <summary>Writes rows of cells as plain text, the way every text the app gives out does (ADR 0022).</summary>
internal static class PlainTextRows
{
    /// <summary>What a cell without a glyph becomes, so the text keeps the art's shape.</summary>
    public const char EmptyCellCharacter = ' ';

    /// <summary>Appends each cell's glyph, or a space for a cell without one.</summary>
    public static void Append(StringBuilder text, ReadOnlySpan<Cell> row)
    {
        Span<char> glyph = stackalloc char[2];
        foreach (var cell in row)
        {
            if (cell.HasGlyph)
                text.Append(glyph[..cell.Glyph.EncodeToUtf16(glyph)]);
            else
                text.Append(EmptyCellCharacter);
        }
    }
}
