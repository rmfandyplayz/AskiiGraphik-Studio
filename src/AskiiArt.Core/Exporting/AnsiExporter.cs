// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Exporting;

/// <summary>
/// ANSI art for terminals (ARCHITECTURE §16, ADR 0044): the visible layers flattened as <see cref="TextExporter"/>
/// does, in UTF-8, with escape codes for each cell's colours. An ANSI document's colours get the 16-colour codes,
/// which each terminal shows in its own palette, and an RGB document's get 24-bit codes. Unset colours get none, so
/// they're the terminal's own text colour and background.
/// </summary>
/// <remarks>
/// A code is written only where a colour changes. Each row that set a colour ends by resetting it, so a background
/// doesn't run on to the terminal's edge and the next row starts from the terminal's own colours. A cell without a
/// glyph shows no text colour, so it leaves the text colour as it was.
/// </remarks>
public sealed class AnsiExporter : IExporter
{
    private const string ControlSequenceIntroducer = "\u001b[";
    private const char SelectGraphicRenditionEnd = 'm';
    private const string ResetColors = ControlSequenceIntroducer + "0m";
    private const char EmptyCellCharacter = ' ';

    // The codes for ANSI colours 0–7 and 8–15; a background's is 10 more than the text colour's.
    private const int FirstTextColorCode = 30;
    private const int FirstBrightTextColorCode = 90;
    private const int BackgroundCodeOffset = 10;
    private const int DefaultTextColorCode = 39;
    private const int DefaultBackgroundCode = 49;
    private const string RgbTextColorPrefix = "38;2;";
    private const string RgbBackgroundPrefix = "48;2;";

    /// <inheritdoc />
    public string Id => "askiiart.export.ansi";

    /// <inheritdoc />
    public string FileExtension => ".ans";

    /// <summary>
    /// UTF-8 without a byte-order mark, with <see cref="TextExporter.FileLineBreak"/> after every row, the last
    /// included.
    /// </summary>
    public byte[] Export(Document document) =>
        Encoding.UTF8.GetBytes(Write(document, TextExporter.FileLineBreak, lineBreakAfterLastRow: true));

    /// <summary>
    /// The rows joined by <paramref name="lineBreak"/>, with none after the last: the form for the clipboard, as
    /// <see cref="TextExporter.ToText"/> gives.
    /// </summary>
    public string ToText(Document document, string lineBreak) =>
        Write(document, lineBreak, lineBreakAfterLastRow: false);

    private string Write(Document document, string lineBreak, bool lineBreakAfterLastRow)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(lineBreak);

        var size = document.CanvasSize;
        var text = new StringBuilder((size.Width + lineBreak.Length) * size.Height);
        var row = new Cell[size.Width];
        Span<char> glyph = stackalloc char[2];
        for (var y = 0; y < size.Height; y++)
        {
            Compositor.CompositeRegion(document.ActiveFrame, new CellRect(0, y, size.Width, 1), row);

            // What the terminal is set to: each row starts from its own colours.
            var textColor = CellColor.Unset;
            var background = CellColor.Unset;
            foreach (var cell in row)
            {
                var wantedTextColor = cell.HasGlyph ? cell.Foreground : textColor;
                AppendColorChanges(text, textColor, wantedTextColor, background, cell.Background);
                textColor = wantedTextColor;
                background = cell.Background;

                if (cell.HasGlyph)
                    text.Append(glyph[..cell.Glyph.EncodeToUtf16(glyph)]);
                else
                    text.Append(EmptyCellCharacter);
            }

            if (!textColor.IsUnset || !background.IsUnset)
                text.Append(ResetColors);

            if (y < size.Height - 1 || lineBreakAfterLastRow)
                text.Append(lineBreak);
        }

        return text.ToString();
    }

    // One escape code for whatever changed, for example "ESC[31;44m" when both did.
    private static void AppendColorChanges(StringBuilder text, CellColor textColor, CellColor wantedTextColor,
        CellColor background, CellColor wantedBackground)
    {
        var textColorChanges = textColor != wantedTextColor;
        var backgroundChanges = background != wantedBackground;
        if (!textColorChanges && !backgroundChanges)
            return;

        text.Append(ControlSequenceIntroducer);
        if (textColorChanges)
            AppendColorCode(text, wantedTextColor, isBackground: false);

        if (textColorChanges && backgroundChanges)
            text.Append(';');

        if (backgroundChanges)
            AppendColorCode(text, wantedBackground, isBackground: true);

        text.Append(SelectGraphicRenditionEnd);
    }

    private static void AppendColorCode(StringBuilder text, CellColor color, bool isBackground)
    {
        if (color.TryGetAnsi(out var ansi))
        {
            var number = (int)ansi;
            var code = number < 8 ? FirstTextColorCode + number : FirstBrightTextColorCode + number - 8;
            if (isBackground)
                code += BackgroundCodeOffset;

            text.Append(CultureInfo.InvariantCulture, $"{code}");
        }
        else if (color.TryGetRgb(out var rgb))
        {
            text.Append(isBackground ? RgbBackgroundPrefix : RgbTextColorPrefix);
            text.Append(CultureInfo.InvariantCulture, $"{rgb.Red};{rgb.Green};{rgb.Blue}");
        }
        else
        {
            var code = isBackground ? DefaultBackgroundCode : DefaultTextColorCode;
            text.Append(CultureInfo.InvariantCulture, $"{code}");
        }
    }
}
