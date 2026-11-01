// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Tests.Commands;

namespace AskiiArt.Core.Tests.Formats;

public sealed class DocumentFileFormatTests
{
    private const string PlainHeader = "ASKIIART 1\n";

    [Theory]
    [InlineData(DocumentFileVariant.Plain)]
    [InlineData(DocumentFileVariant.Compressed)]
    public void Decode_OfEncode_ReturnsTheSameDocumentAndVariant(DocumentFileVariant variant)
    {
        var document = CreateSampleDocument();

        var contents = DocumentFileFormat.Decode(DocumentFileFormat.Encode(document, variant));

        Assert.Equal(TestCommands.Snapshot(document), TestCommands.Snapshot(contents.Document));
        Assert.Equal(variant, contents.Variant);
    }

    [Fact]
    public void PlainFile_ShowsTheArtAsRows_WithUnixLineEndings()
    {
        var text = Encoding.UTF8.GetString(DocumentFileFormat.Encode(CreateSampleDocument(),
            DocumentFileVariant.Plain));

        Assert.StartsWith(PlainHeader, text, StringComparison.Ordinal);
        Assert.Contains("\"│ é █\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainFile_LeavesOutReservedFields_WhileTheyHoldTheirDefaults()
    {
        var document = TestCells.CreateDocument(3, 2);

        var text = Encoding.UTF8.GetString(DocumentFileFormat.Encode(document, DocumentFileVariant.Plain));

        Assert.DoesNotContain("opacity", text, StringComparison.Ordinal);
        Assert.DoesNotContain("blendMode", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CompressedFile_HoldsTheSameJsonAsThePlainOne()
    {
        var document = CreateSampleDocument();
        var plain = DocumentFileFormat.Encode(document, DocumentFileVariant.Plain);
        var compressed = DocumentFileFormat.Encode(document, DocumentFileVariant.Compressed);

        // The compressed header is 8 magic bytes and a 4-byte version.
        using var brotli = new BrotliStream(new MemoryStream(compressed[12..]), CompressionMode.Decompress);
        using var json = new MemoryStream();
        brotli.CopyTo(json);

        Assert.Equal(0x89, compressed[0]);
        Assert.Equal(plain[PlainHeader.Length..], json.ToArray());
        Assert.True(compressed.Length < plain.Length);
    }

    [Fact]
    public void Decode_PlainFileEditedInATextEditor_ToleratesAByteOrderMarkAndWindowsLineEndings()
    {
        var document = CreateSampleDocument();
        var plain = Encoding.UTF8.GetString(DocumentFileFormat.Encode(document, DocumentFileVariant.Plain));
        var windowsLineEndings = plain.Replace("\n", "\r\n", StringComparison.Ordinal);
        byte[] edited = [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(windowsLineEndings)];

        var contents = DocumentFileFormat.Decode(edited);

        Assert.Equal(TestCommands.Snapshot(document), TestCommands.Snapshot(contents.Document));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("{\"canvas\": {}}")]
    [InlineData("ASKIIART")]
    public void Decode_SomethingElse_IsNotADocument(string text)
    {
        var exception =
            Assert.Throws<DocumentFileException>(() => DocumentFileFormat.Decode(Encoding.UTF8.GetBytes(text)));

        Assert.Equal(DocumentFileProblem.NotADocument, exception.Problem);
    }

    [Fact]
    public void Decode_NewerFormatVersion_SaysWhichVersion()
    {
        var exception = Assert.Throws<DocumentFileException>(() =>
            DocumentFileFormat.Decode(Encoding.UTF8.GetBytes("ASKIIART 2\n{}")));

        Assert.Equal(DocumentFileProblem.NewerVersion, exception.Problem);
        Assert.Equal(2, exception.FileVersion);
    }

    [Theory]
    [InlineData("ASKIIART \n{}")]
    [InlineData("ASKIIART 0\n{}")]
    [InlineData("ASKIIART 1{}")]
    [InlineData("ASKIIART 1\n{")]
    [InlineData("ASKIIART 1\n[]")]
    public void Decode_BrokenHeaderOrJson_IsDamaged(string text)
    {
        AssertDamaged(Encoding.UTF8.GetBytes(text));
    }

    public static TheoryData<string> Damage =>
    [
        "canvas.width=0",
        "canvas.width=5000",
        "canvas.height=\"3\"",
        "characterSet=\"klingon\"",
        "font=\" \"",
        "frames=[]",
        "frames.twoFrames",
        "layers=[]",
        "layer.id=\"not a guid\"",
        "layer.visible=1",
        "layer.opacity=2",
        "layer.blendMode=\"screen\"",
        "layer.rows.missingOne",
        "layer.rows.firstTooShort",
        "layer.rows.firstTooLong",
        "layer.rows.firstHasABell",
        "layer.occupancy=[1]",
        "layer.occupancy=[-1,13]",
        "layers.sameIdTwice",
        "asciiDocument.withAccent"
    ];

    [Theory]
    [MemberData(nameof(Damage))]
    public void Decode_ValuesThatDontFitTogether_AreDamaged(string damage)
    {
        var root = DocumentJson.Parse(DocumentJson.Write(TestCells.CreateDocument(3, 4)));
        var frame = root["frames"]![0]!.AsObject();
        var layer = frame["layers"]![0]!.AsObject();
        var rows = layer["rows"]!.AsArray();
        switch (damage)
        {
            case "frames.twoFrames":
                root["frames"]!.AsArray().Add(frame.DeepClone());
                break;
            case "layer.rows.missingOne":
                rows.RemoveAt(0);
                break;
            case "layer.rows.firstTooShort":
                rows[0] = "ab";
                break;
            case "layer.rows.firstTooLong":
                rows[0] = "abcd";
                break;
            case "layer.rows.firstHasABell":
                rows[0] = "a\u0007c";
                layer["occupancy"] = new JsonArray(0, 12);
                break;
            case "layers.sameIdTwice":
                frame["layers"]!.AsArray().Add(layer.DeepClone());
                break;
            case "asciiDocument.withAccent":
                root["characterSet"] = "ascii";
                rows[0] = "aéc";
                layer["occupancy"] = new JsonArray(0, 12);
                break;
            default:
                SetValue(root, frame, layer, damage);
                break;
        }

        AssertDamaged(Encoding.UTF8.GetBytes(PlainHeader + root.ToJsonString()));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(11)]
    [InlineData(40)]
    public void Decode_CompressedFileCutShort_IsDamaged(int length)
    {
        var compressed = DocumentFileFormat.Encode(CreateSampleDocument(), DocumentFileVariant.Compressed);

        AssertDamagedOrNotADocument(compressed[..length]);
    }

    [Fact]
    public void Decode_CorruptedFiles_OpenOrFailWithADocumentFileException()
    {
        // A fixed seed, so a failure can be reproduced.
        var random = new Random(1701);
        foreach (var variant in new[] { DocumentFileVariant.Plain, DocumentFileVariant.Compressed })
        {
            var file = DocumentFileFormat.Encode(CreateSampleDocument(), variant);
            for (var attempt = 0; attempt < 200; attempt++)
            {
                var damaged = (byte[])file.Clone();
                for (var change = 0; change < 1 + attempt % 4; change++)
                    damaged[random.Next(damaged.Length)] = (byte)random.Next(256);

                var exception = Record.Exception(() => DocumentFileFormat.Decode(damaged));
                Assert.True(exception is null or DocumentFileException,
                    $"{variant} attempt {attempt} threw {exception}");
            }
        }
    }

    [Theory]
    [InlineData("art.asciiart", DocumentFileVariant.Plain)]
    [InlineData("art.ASCIIARTZ", DocumentFileVariant.Compressed)]
    [InlineData("art.txt", DocumentFileVariant.Plain)]
    [InlineData("art", DocumentFileVariant.Plain)]
    public void VariantForPath_FollowsTheExtension(string path, DocumentFileVariant expected)
    {
        Assert.Equal(expected, DocumentFileFormat.VariantForPath(path));
    }

    /// <summary>
    /// A document with everything the format stores: two layers (one hidden, locked and
    /// half-transparent), deliberate spaces, empty cells, characters JSON escapes, accents, box
    /// drawing and a character outside the Basic Multilingual Plane.
    /// </summary>
    internal static Document CreateSampleDocument()
    {
        var document = Document.Create(new CanvasSize(5, 3), "Bottom \"layer\"", CharacterSet.Unicode,
            new FontReference("Fira Code"));
        TestCells.Paint(document.ActiveFrame.Layers[0], "│ é █", "·\\\"·/", "·····");
        var top = TestCells.AddLayer(document, "Top");
        top.SetProperties(new LayerProperties("Top") { IsVisible = false, IsLocked = true, Opacity = 0.5 });
        TestCells.Paint(top, "·····", "·····", "ab·· ");
        top.SetCell(new CellPoint(2, 2), Cell.FromGlyph(new Rune(0x1F600)));
        return document;
    }

    private static void SetValue(JsonObject root, JsonObject frame, JsonObject layer, string damage)
    {
        var (path, json) = (damage[..damage.IndexOf('=', StringComparison.Ordinal)],
            damage[(damage.IndexOf('=', StringComparison.Ordinal) + 1)..]);
        var value = JsonNode.Parse(json);
        switch (path.Split('.'))
        {
            case ["canvas", var name]:
                root["canvas"]![name] = value;
                break;
            case ["layer", var name]:
                layer[name] = value;
                break;
            case ["layers"]:
                frame["layers"] = value;
                break;
            case [var name]:
                root[name] = value;
                break;
            default:
                throw new ArgumentException($"Unknown damage '{damage}'.", nameof(damage));
        }
    }

    private static void AssertDamaged(byte[] file)
    {
        var exception = Assert.Throws<DocumentFileException>(() => DocumentFileFormat.Decode(file));

        Assert.Equal(DocumentFileProblem.Damaged, exception.Problem);
    }

    private static void AssertDamagedOrNotADocument(byte[] file)
    {
        var exception = Assert.Throws<DocumentFileException>(() => DocumentFileFormat.Decode(file));

        Assert.NotEqual(DocumentFileProblem.NewerVersion, exception.Problem);
    }
}
