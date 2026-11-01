// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Formats;

/// <summary>
/// The JSON both file variants store (ARCHITECTURE §13, ADR 0021). Each layer's glyphs are an
/// array of row strings, so the art is visible in a text editor, with empty cells written as
/// spaces. Which cells are occupied is stored separately as run lengths: row by row, alternately
/// empty and occupied, starting with empty. Reserved fields are written only when they're not
/// their defaults.
/// </summary>
internal static class DocumentJson
{
    private static readonly JsonWriterOptions _writerOptions = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",

        // Keeps accents, box drawing and other non-ASCII glyphs readable instead of \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonDocumentOptions _readerOptions = new() { MaxDepth = 16 };

    /// <summary>Writes <paramref name="document"/> as UTF-8 JSON, ending with a line break.</summary>
    public static byte[] Write(Document document)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, _writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteStartObject("canvas");
            writer.WriteNumber("width", document.CanvasSize.Width);
            writer.WriteNumber("height", document.CanvasSize.Height);
            writer.WriteEndObject();
            writer.WriteString("characterSet", NameOf(document.CharacterSet));
            writer.WriteString("font", document.Font.FamilyName);
            writer.WriteStartArray("frames");
            foreach (var frame in document.Frames)
            {
                writer.WriteStartObject();
                writer.WriteStartArray("layers");
                foreach (var layer in frame.Layers)
                    WriteLayer(writer, layer);

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        buffer.WriteByte((byte)'\n');
        return buffer.ToArray();
    }

    /// <summary>Parses UTF-8 JSON into the tree that migrations and <see cref="Read"/> work on.</summary>
    /// <exception cref="DocumentFileException">The JSON is broken or isn't an object.</exception>
    public static JsonObject Parse(ReadOnlySpan<byte> json)
    {
        // The parser checks UTF-8 only when a string is read, so damage would surface later as
        // an unrelated exception.
        if (!Utf8.IsValid(json))
            throw Damaged("The document isn't valid UTF-8.");

        try
        {
            CheckEscapedText(json);
            return JsonNode.Parse(json, documentOptions: _readerOptions) as JsonObject
                   ?? throw Damaged("The document isn't a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new DocumentFileException("The document's JSON is broken.", exception);
        }
    }

    // Escapes are also decoded only when a string or property name is read, so one that makes no
    // sense (such as half of a surrogate pair) would likewise surface later as an unrelated exception.
    private static void CheckEscapedText(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json, new JsonReaderOptions { MaxDepth = _readerOptions.MaxDepth });
        while (reader.Read())
        {
            if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName) || !reader.ValueIsEscaped)
                continue;

            try
            {
                reader.GetString();
            }
            catch (InvalidOperationException exception)
            {
                throw new DocumentFileException("The document has an escaped character that isn't valid.",
                    exception);
            }
        }
    }

    /// <summary>Builds the document that <paramref name="root"/> describes, checking every value.</summary>
    /// <exception cref="DocumentFileException">Something is missing, of the wrong kind, or out of range.</exception>
    public static Document Read(JsonObject root)
    {
        var canvas = RequireObject(root, "canvas");
        var width = RequireInt(canvas, "width");
        var height = RequireInt(canvas, "height");
        if (!CanvasSize.IsValid(width, height))
            throw Damaged($"The canvas size {width} × {height} is outside the allowed sizes.");

        var size = new CanvasSize(width, height);
        var characterSet = ReadCharacterSet(RequireString(root, "characterSet"));
        var fontName = RequireString(root, "font");
        if (string.IsNullOrWhiteSpace(fontName))
            throw Damaged("The font name is empty.");

        var frames = RequireArray(root, "frames");

        // Version 1 has exactly one frame; more are reserved for animation (ARCHITECTURE §18).
        if (frames.Count != 1)
            throw Damaged($"The document has {frames.Count} frames instead of one.");

        var layerArray = RequireArray(RequireObject(frames[0], "frames[0]"), "layers");
        if (layerArray.Count == 0)
            throw Damaged("The frame has no layers.");

        var layers = new List<Layer>(layerArray.Count);
        var ids = new HashSet<Guid>();
        for (var index = 0; index < layerArray.Count; index++)
        {
            var layer = ReadLayer(RequireObject(layerArray[index], $"layers[{index}]"), size, characterSet);
            if (!ids.Add(layer.Id))
                throw Damaged($"Two layers have the id {layer.Id}.");

            layers.Add(layer);
        }

        return Document.CreateWithLayers(size, characterSet, new FontReference(fontName), layers);
    }

    private static void WriteLayer(Utf8JsonWriter writer, Layer layer)
    {
        var properties = layer.Properties;
        writer.WriteStartObject();
        writer.WriteString("id", layer.Id.ToString("D"));
        writer.WriteString("name", properties.Name);
        writer.WriteBoolean("visible", properties.IsVisible);
        writer.WriteBoolean("locked", properties.IsLocked);

        // Reserved (ARCHITECTURE §18): written only once they hold something.
        if (properties.Opacity != 1.0)
            writer.WriteNumber("opacity", properties.Opacity);

        if (properties.BlendMode != LayerBlendMode.Normal)
            writer.WriteString("blendMode", NameOf(properties.BlendMode));

        var occupancy = new OccupancyRunsWriter();
        var row = new StringBuilder();
        Span<char> glyph = stackalloc char[2];
        writer.WriteStartArray("rows");
        for (var y = 0; y < layer.Size.Height; y++)
        {
            row.Clear();
            foreach (var cell in layer.GetRow(y))
            {
                occupancy.Add(cell.IsOccupied);
                if (cell.IsEmpty)
                {
                    row.Append(' ');
                    continue;
                }

                var length = cell.Glyph.EncodeToUtf16(glyph);
                row.Append(glyph[..length]);
            }

            writer.WriteStringValue(row.ToString());
        }

        writer.WriteEndArray();

        // On one line: the runs are for the app, and one per line would bury the art.
        writer.WritePropertyName("occupancy");
        writer.WriteRawValue(occupancy.ToJson(), skipInputValidation: true);
        writer.WriteEndObject();
    }

    private static Layer ReadLayer(JsonObject layerObject, CanvasSize size, CharacterSet characterSet)
    {
        if (!Guid.TryParseExact(RequireString(layerObject, "id"), "D", out var id))
            throw Damaged("A layer's id isn't a GUID.");

        var properties = new LayerProperties(RequireString(layerObject, "name"))
        {
            IsVisible = RequireBool(layerObject, "visible"),
            IsLocked = RequireBool(layerObject, "locked"),
            Opacity = ReadOpacity(layerObject),
            BlendMode = ReadBlendMode(layerObject)
        };

        var layer = new Layer(id, properties, size);
        var rows = RequireArray(layerObject, "rows");
        if (rows.Count != size.Height)
            throw Damaged($"Layer {id} has {rows.Count} rows instead of {size.Height}.");

        var occupancy = new OccupancyRunsReader(ReadRuns(layerObject, size.Bounds.Area));
        var index = 0;
        for (var y = 0; y < size.Height; y++)
        {
            var text = RequireStringValue(rows[y], $"row {y} of layer {id}");
            var x = 0;
            foreach (var rune in text.EnumerateRunes())
            {
                if (x == size.Width)
                    throw Damaged($"Row {y} of layer {id} is longer than the canvas.");

                if (occupancy.NextIsOccupied())
                    layer.SetCell(index, ToCell(rune, characterSet));

                x++;
                index++;
            }

            if (x != size.Width)
                throw Damaged($"Row {y} of layer {id} is shorter than the canvas.");
        }

        return layer;
    }

    private static Cell ToCell(Rune rune, CharacterSet characterSet)
    {
        // An ASCII only document holding other characters is damaged (ADR 0016).
        if (Rune.IsControl(rune) || (characterSet == CharacterSet.Ascii && !characterSet.Allows(rune)))
            throw Damaged($"The document holds U+{rune.Value:X4}, which it can't.");

        return Cell.FromGlyph(rune);
    }

    private static int[] ReadRuns(JsonObject layerObject, int area)
    {
        var array = RequireArray(layerObject, "occupancy");
        var runs = new int[array.Count];
        long total = 0;
        for (var index = 0; index < runs.Length; index++)
        {
            var run = RequireIntValue(array[index], "an occupancy run");
            if (run < 0)
                throw Damaged("An occupancy run is negative.");

            runs[index] = run;
            total += run;
        }

        if (total != area)
            throw Damaged($"The occupancy runs cover {total} cells instead of {area}.");

        return runs;
    }

    private static double ReadOpacity(JsonObject layerObject)
    {
        if (layerObject["opacity"] is not { } node)
            return 1.0;

        if (node is not JsonValue value || !value.TryGetValue(out double opacity) || opacity is not (>= 0.0 and <= 1.0))
            throw Damaged("A layer's opacity isn't a number from 0 to 1.");

        return opacity;
    }

    private static LayerBlendMode ReadBlendMode(JsonObject layerObject)
    {
        if (layerObject["blendMode"] is null)
            return LayerBlendMode.Normal;

        return RequireString(layerObject, "blendMode") switch
        {
            "normal" => LayerBlendMode.Normal,
            var other => throw Damaged($"'{other}' isn't a blend mode.")
        };
    }

    private static CharacterSet ReadCharacterSet(string name) => name switch
    {
        "unicode" => CharacterSet.Unicode,
        "ascii" => CharacterSet.Ascii,
        _ => throw Damaged($"'{name}' isn't a character set.")
    };

    private static string NameOf(CharacterSet characterSet) => characterSet switch
    {
        CharacterSet.Unicode => "unicode",
        CharacterSet.Ascii => "ascii",
        _ => throw new ArgumentOutOfRangeException(nameof(characterSet), characterSet, null)
    };

    private static string NameOf(LayerBlendMode blendMode) => blendMode switch
    {
        LayerBlendMode.Normal => "normal",
        _ => throw new ArgumentOutOfRangeException(nameof(blendMode), blendMode, null)
    };

    private static JsonObject RequireObject(JsonObject parent, string name) =>
        RequireObject(parent[name], name);

    private static JsonObject RequireObject(JsonNode? node, string description) =>
        node as JsonObject ?? throw Damaged($"'{description}' is missing or isn't an object.");

    private static JsonArray RequireArray(JsonObject parent, string name) =>
        parent[name] as JsonArray ?? throw Damaged($"'{name}' is missing or isn't a list.");

    private static string RequireString(JsonObject parent, string name) => RequireStringValue(parent[name], name);

    private static string RequireStringValue(JsonNode? node, string description)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.String)
            return value.GetValue<string>();

        throw Damaged($"'{description}' is missing or isn't text.");
    }

    private static int RequireInt(JsonObject parent, string name) => RequireIntValue(parent[name], name);

    private static int RequireIntValue(JsonNode? node, string description)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.Number
            && value.TryGetValue(out int number))
        {
            return number;
        }

        throw Damaged($"'{description}' is missing or isn't a whole number.");
    }

    private static bool RequireBool(JsonObject parent, string name)
    {
        if (parent[name] is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
            return value.GetValue<bool>();

        throw Damaged($"'{name}' is missing or isn't true or false.");
    }

    private static DocumentFileException Damaged(string message) => new(DocumentFileProblem.Damaged, message);

    /// <summary>Collects occupancy run lengths cell by cell.</summary>
    private sealed class OccupancyRunsWriter
    {
        private readonly List<int> _runs = [];
        private bool _isCountingOccupied;
        private int _currentRun;

        public void Add(bool isOccupied)
        {
            if (isOccupied != _isCountingOccupied)
            {
                _runs.Add(_currentRun);
                _currentRun = 0;
                _isCountingOccupied = isOccupied;
            }

            _currentRun++;
        }

        public string ToJson()
        {
            var json = new StringBuilder("[");
            foreach (var run in _runs)
                json.Append(run.ToString(CultureInfo.InvariantCulture)).Append(',');

            return json.Append(_currentRun.ToString(CultureInfo.InvariantCulture)).Append(']').ToString();
        }
    }

    /// <summary>Walks occupancy run lengths cell by cell. The runs must cover every cell.</summary>
    private sealed class OccupancyRunsReader(int[] runs)
    {
        private int _runIndex = -1;
        private int _remainingInRun;
        private bool _isOccupied = true;

        public bool NextIsOccupied()
        {
            while (_remainingInRun == 0)
            {
                _runIndex++;
                _remainingInRun = runs[_runIndex];
                _isOccupied = !_isOccupied;
            }

            _remainingInRun--;
            return _isOccupied;
        }
    }
}
