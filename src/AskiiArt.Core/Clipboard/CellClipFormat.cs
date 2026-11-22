// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Clipboard;

/// <summary>
/// The app's own clipboard format (ADR 0067), which Copy puts on the clipboard beside plain text, so a paste back into
/// the app keeps the cells' colours, the copied shape and where it came from. It's JSON: a version, the origin, the
/// shape as runs of cells, and the cells as a one-layer document file, compressed and in Base64, so they're written
/// and checked as files are.
/// </summary>
public static class CellClipFormat
{
    /// <summary>The name the format goes by on the clipboard.</summary>
    public const string Name = "AskiiArt.Cells";

    private const int Version = 1;
    private const string VersionProperty = "version";
    private const string OriginProperty = "origin";
    private const string ShapeProperty = "shape";
    private const string CellsProperty = "cells";

    // Base64's "+" and "/" stay as they are, rather than becoming escapes six characters long.
    private static readonly JsonWriterOptions _writerOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    // The clip's document never shows anywhere, so its layer's name doesn't matter.
    private const string LayerName = "Clip";

    /// <summary>
    /// Writes <paramref name="clip"/>, whose cells come from a document using <paramref name="characterSet"/> and
    /// <paramref name="colorMode"/>.
    /// </summary>
    public static string Write(CellClip clip, CharacterSet characterSet, ColorMode colorMode)
    {
        ArgumentNullException.ThrowIfNull(clip);

        var document = Document.Create(clip.Size, LayerName, characterSet, colorMode: colorMode);
        var layer = document.ActiveFrame.Layers[0];
        var cells = clip.Cells;
        for (var index = 0; index < cells.Length; index++)
            layer.SetCell(index, cells[index]);

        var file = DocumentFileFormat.Encode(document, DocumentFileVariant.Compressed);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, _writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteNumber(VersionProperty, Version);
            WriteOrigin(writer, clip.Origin);
            WriteShape(writer, clip.Shape);
            writer.WriteString(CellsProperty, Convert.ToBase64String(file));
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads what <see cref="Write"/> wrote.</summary>
    /// <returns>The clip, or null when the text isn't a clip this version of the app can read.</returns>
    public static CellClip? TryRead(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            if (JsonNode.Parse(text) is not JsonObject root || ReadInt(root[VersionProperty]) != Version)
                return null;

            if (root[CellsProperty]?.GetValue<string>() is not { } base64)
                return null;

            var document = DocumentFileFormat.Decode(Convert.FromBase64String(base64)).Document;
            var layer = document.ActiveFrame.Layers[0];
            var size = document.CanvasSize;
            var cells = new Cell[size.CellCount];
            for (var index = 0; index < cells.Length; index++)
                cells[index] = layer.GetCell(index);

            if (ReadShape(root[ShapeProperty], size) is not { } shape)
                return null;

            return new CellClip(size, cells, shape, ReadOrigin(root[OriginProperty]));
        }
        catch (Exception exception) when (exception is JsonException or FormatException or InvalidOperationException
                                              or ArgumentException or DocumentFileException)
        {
            return null;
        }
    }

    private static void WriteOrigin(Utf8JsonWriter writer, CellPoint? origin)
    {
        if (origin is not { } point)
        {
            writer.WriteNull(OriginProperty);
            return;
        }

        writer.WriteStartArray(OriginProperty);
        writer.WriteNumberValue(point.X);
        writer.WriteNumberValue(point.Y);
        writer.WriteEndArray();
    }

    // Each run is [row, column, length].
    private static void WriteShape(Utf8JsonWriter writer, SelectionMask shape)
    {
        writer.WriteStartArray(ShapeProperty);
        for (var row = shape.Bounds.Top; row < shape.Bounds.Bottom; row++)
        {
            foreach (var (column, length) in shape.RunsInRow(row))
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(row);
                writer.WriteNumberValue(column);
                writer.WriteNumberValue(length);
                writer.WriteEndArray();
            }
        }

        writer.WriteEndArray();
    }

    private static CellPoint? ReadOrigin(JsonNode? node) =>
        node is JsonArray { Count: 2 } origin ? new CellPoint(ReadInt(origin[0]), ReadInt(origin[1])) : null;

    // Runs outside the clip are left out, as the builder does with any run.
    private static SelectionMask? ReadShape(JsonNode? node, CanvasSize size)
    {
        if (node is not JsonArray runs)
            return null;

        var builder = new SelectionMaskBuilder(size);
        foreach (var run in runs)
        {
            if (run is not JsonArray { Count: 3 } parts)
                return null;

            builder.AddRun(ReadInt(parts[0]), ReadInt(parts[1]), ReadInt(parts[2]));
        }

        return builder.Build();
    }

    private static int ReadInt(JsonNode? node) =>
        node?.GetValue<int>() ?? throw new FormatException("A number is missing.");
}
