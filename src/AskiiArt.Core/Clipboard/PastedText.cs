// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Clipboard;

/// <summary>
/// Text pasted from other apps (ADR 0067): its lines as the characters a paste writes, and those lines as cells.
/// </summary>
public static class PastedText
{
    /// <summary>A tab runs to the next multiple of this many columns, as in terminals.</summary>
    public const int TabWidth = 8;

    private const int Tab = 0x09;
    private const int LineFeed = 0x0A;
    private const int CarriageReturn = 0x0D;
    private const int Escape = 0x1B;
    private const int Bell = 0x07;
    private const int LineSeparator = 0x2028;
    private const int ParagraphSeparator = 0x2029;

    /// <summary>
    /// The lines of <paramref name="text"/>, split at any kind of line break, without ANSI escape codes or other
    /// control characters, with each tab as spaces to the next multiple of <see cref="TabWidth"/>, and without empty
    /// lines at the end. Colours in escape codes are left out: pasting ANSI art comes with ANSI import (M7).
    /// </summary>
    public static IReadOnlyList<Rune[]> Lines(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var runes = text.EnumerateRunes().ToArray();
        List<Rune[]> lines = [];
        List<Rune> line = [];
        for (var index = 0; index < runes.Length; index++)
        {
            var rune = runes[index];
            switch (rune.Value)
            {
                case CarriageReturn or LineFeed or LineSeparator or ParagraphSeparator:
                    if (rune.Value == CarriageReturn && index + 1 < runes.Length && runes[index + 1].Value == LineFeed)
                        index++;

                    lines.Add([.. line]);
                    line.Clear();
                    break;
                case Tab:
                    do
                    {
                        line.Add(new Rune(' '));
                    }
                    while (line.Count % TabWidth != 0);

                    break;
                case Escape:
                    index = EndOfEscapeCode(runes, index);
                    break;
                default:
                    if (!Rune.IsControl(rune))
                        line.Add(rune);

                    break;
            }
        }

        lines.Add([.. line]);
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        return lines;
    }

    /// <summary>
    /// <paramref name="text"/> as cells: a row per line, as wide as the longest, each character a glyph without
    /// colours. Spaces and the places shorter lines don't reach are empty, so a paste covers nothing round the text's
    /// shape. The whole rectangle is the shape, and it has no origin. Lines and rows past the largest canvas are left
    /// out.
    /// </summary>
    /// <returns>The cells, or null when the text has no character but spaces.</returns>
    public static CellClip? ToClip(string text)
    {
        var lines = Lines(text);
        var height = Math.Min(lines.Count, CanvasSize.MaximumSide);
        var width = 0;
        for (var y = 0; y < height; y++)
            width = Math.Max(width, Math.Min(lines[y].Length, CanvasSize.MaximumSide));

        if (width == 0)
            return null;

        var size = new CanvasSize(width, height);
        var cells = new Cell[size.CellCount];
        for (var y = 0; y < height; y++)
        {
            var line = lines[y];
            for (var x = 0; x < Math.Min(line.Length, width); x++)
            {
                if (line[x].Value != ' ')
                    cells[(y * width) + x] = Cell.FromGlyph(line[x]);
            }
        }

        var clip = new CellClip(size, cells, SelectionMask.All(size), origin: null);
        return clip.HasContent ? clip : null;
    }

    // An escape code is ESC [ ... ending in a letter or another final character (colours, cursor moves), ESC ] ...
    // ending in BEL or ESC \ (titles, links), or ESC and one more character. Returns the index of its last character.
    private static int EndOfEscapeCode(Rune[] runes, int escape)
    {
        var next = escape + 1;
        if (next >= runes.Length)
            return escape;

        if (runes[next].Value == '[')
        {
            var end = next + 1;
            while (end < runes.Length && runes[end].Value is < 0x40 or > 0x7E)
                end++;

            return Math.Min(end, runes.Length - 1);
        }

        if (runes[next].Value == ']')
        {
            for (var end = next + 1; end < runes.Length; end++)
            {
                if (runes[end].Value == Bell)
                    return end;

                if (runes[end].Value == Escape && end + 1 < runes.Length && runes[end + 1].Value == '\\')
                    return end + 1;
            }

            return runes.Length - 1;
        }

        return next;
    }
}
