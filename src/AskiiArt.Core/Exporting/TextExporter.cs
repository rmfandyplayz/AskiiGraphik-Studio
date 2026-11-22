// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Exporting;

/// <summary>
/// Plain text (ARCHITECTURE §16): the visible layers flattened by the <see cref="Compositor"/>, one
/// line per row of the canvas. Every line is the canvas's full width, with empty cells as spaces, so
/// the art keeps its shape wherever it's pasted (ADR 0022).
/// </summary>
public sealed class TextExporter : IExporter
{
    // Exporters are injected as instances (ADR 0022), so ToText stays one like Export, though it needs no state.
    private const string InjectedJustification = "Exporters are injected instances, like IExporter.Export.";

    /// <summary>The line break in exported files, as for every file the app writes.</summary>
    public const string FileLineBreak = "\n";

    /// <inheritdoc />
    public string Id => "askiiart.export.text";

    /// <inheritdoc />
    public string FileExtension => ".txt";

    /// <summary>
    /// UTF-8 without a byte-order mark, with <see cref="FileLineBreak"/> after every row, the last included.
    /// </summary>
    public byte[] Export(Document document) =>
        Encoding.UTF8.GetBytes(Flatten(document, selection: null, FileLineBreak, lineBreakAfterLastRow: true));

    /// <summary>
    /// The rows joined by <paramref name="lineBreak"/>, with none after the last: the form for the
    /// clipboard, where a final line break would add an empty line wherever it's pasted. With a
    /// <paramref name="selection"/>, only the rows and columns of its bounds, and the cells in them
    /// that aren't selected as spaces (ADR 0067).
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = InjectedJustification)]
    public string ToText(Document document, string lineBreak, SelectionMask? selection = null) =>
        Flatten(document, selection, lineBreak, lineBreakAfterLastRow: false);

    private static string Flatten(Document document, SelectionMask? selection, string lineBreak,
        bool lineBreakAfterLastRow)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(lineBreak);

        var region = FlattenedRows.RegionOf(document, selection);
        var text = new StringBuilder((region.Width + lineBreak.Length) * region.Height);
        var row = new Cell[region.Width];
        for (var y = region.Top; y < region.Bottom; y++)
        {
            FlattenedRows.Read(document, selection, region, y, row);
            PlainTextRows.Append(text, row);
            if (y < region.Bottom - 1 || lineBreakAfterLastRow)
                text.Append(lineBreak);
        }

        return text.ToString();
    }
}
