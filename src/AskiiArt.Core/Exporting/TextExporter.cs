// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Exporting;

/// <summary>
/// Plain text (ARCHITECTURE §16): the visible layers flattened by the <see cref="Compositor"/>, one
/// line per row of the canvas. Every line is the canvas's full width, with empty cells as spaces, so
/// the art keeps its shape wherever it's pasted (ADR 0022).
/// </summary>
public sealed class TextExporter : IExporter
{
    /// <summary>The line break in exported files, as for every file the app writes.</summary>
    public const string FileLineBreak = "\n";

    private const char EmptyCellCharacter = ' ';

    private readonly CompositingPolicy _policy;

    /// <summary>Creates an exporter that flattens with <paramref name="policy"/>, as the canvas does.</summary>
    public TextExporter(CompositingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;
    }

    /// <inheritdoc />
    public string Id => "askiiart.export.text";

    /// <inheritdoc />
    public string FileExtension => ".txt";

    /// <summary>
    /// UTF-8 without a byte-order mark, with <see cref="FileLineBreak"/> after every row, the last included.
    /// </summary>
    public byte[] Export(Document document) =>
        Encoding.UTF8.GetBytes(Flatten(document, FileLineBreak, lineBreakAfterLastRow: true));

    /// <summary>
    /// The rows joined by <paramref name="lineBreak"/>, with none after the last: the form for the
    /// clipboard, where a final line break would add an empty line wherever it's pasted.
    /// </summary>
    public string ToText(Document document, string lineBreak) =>
        Flatten(document, lineBreak, lineBreakAfterLastRow: false);

    private string Flatten(Document document, string lineBreak, bool lineBreakAfterLastRow)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(lineBreak);

        var size = document.CanvasSize;
        var text = new StringBuilder((size.Width + lineBreak.Length) * size.Height);
        var row = new Cell[size.Width];
        Span<char> glyph = stackalloc char[2];
        for (var y = 0; y < size.Height; y++)
        {
            Compositor.CompositeRegion(document.ActiveFrame, new CellRect(0, y, size.Width, 1), _policy, row);
            foreach (var cell in row)
            {
                if (cell.IsEmpty)
                {
                    text.Append(EmptyCellCharacter);
                    continue;
                }

                text.Append(glyph[..cell.Glyph.EncodeToUtf16(glyph)]);
            }

            if (y < size.Height - 1 || lineBreakAfterLastRow)
                text.Append(lineBreak);
        }

        return text.ToString();
    }
}
