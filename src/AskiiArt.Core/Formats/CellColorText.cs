// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Formats;

/// <summary>
/// A colour as files and settings write it (ADR 0037): an ANSI colour's name, such as "brightRed", or an RGB colour as
/// <c>#rrggbb</c>. An unset colour has no text.
/// </summary>
public static class CellColorText
{
    private const string RgbPrefix = "#";
    private const int RgbTextLength = 7;

    private static readonly string[] _ansiColorNames =
    [
        "black", "red", "green", "yellow", "blue", "magenta", "cyan", "white",
        "brightBlack", "brightRed", "brightGreen", "brightYellow", "brightBlue", "brightMagenta", "brightCyan",
        "brightWhite"
    ];

    /// <summary><paramref name="color"/> as text, or null when it's unset.</summary>
    public static string? Write(CellColor color)
    {
        if (color.TryGetAnsi(out var ansi))
            return _ansiColorNames[(int)ansi];

        if (color.TryGetRgb(out var rgb))
            return rgb.ToString();

        return null;
    }

    /// <summary>The colour <paramref name="text"/> names, or null when it isn't a colour.</summary>
    public static CellColor? Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == RgbTextLength && text.StartsWith(RgbPrefix, StringComparison.Ordinal)
            && TryParseHexByte(text.AsSpan(1, 2), out var red) && TryParseHexByte(text.AsSpan(3, 2), out var green)
            && TryParseHexByte(text.AsSpan(5, 2), out var blue))
        {
            return CellColor.FromRgb(new RgbColor(red, green, blue));
        }

        var ansiIndex = Array.IndexOf(_ansiColorNames, text);
        if (ansiIndex >= 0)
            return CellColor.FromAnsi((AnsiColor)ansiIndex);

        return null;
    }

    private static bool TryParseHexByte(ReadOnlySpan<char> digits, out byte value) =>
        byte.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
}
