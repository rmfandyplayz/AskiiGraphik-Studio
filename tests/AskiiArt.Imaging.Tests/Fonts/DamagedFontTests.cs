// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Buffers.Binary;
using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Fonts;

/// <summary>
/// Font files the user loads can be damaged. Loading one must either give a font that's safe to
/// measure and draw with, or throw <see cref="FontLoadException"/>; nothing else.
/// </summary>
public sealed class DamagedFontTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AskiiArtTests", Guid.NewGuid().ToString("N"));
    private readonly byte[] _realFont = ReadBundledFont();

    public DamagedFontTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void LoadFromFile_FontClaimingAnEnormousCell_ThrowsFontLoadException()
    {
        // With 16 units per em instead of 1000, every measurement becomes about 60 times bigger.
        var damaged = FontFileEditor.SetUInt16(_realFont, "head", FontFileEditor.HeadUnitsPerEmOffset, 16);

        AssertSkiaAcceptsButTheAppRejects(damaged);
    }

    [Fact]
    public void LoadFromFile_FontClaimingNoHeight_ThrowsFontLoadException()
    {
        var damaged = _realFont;
        foreach (var offset in FontFileEditor.HheaVerticalMetricOffsets)
            damaged = FontFileEditor.SetUInt16(damaged, "hhea", offset, 0);

        foreach (var offset in FontFileEditor.Os2TypoMetricOffsets)
            damaged = FontFileEditor.SetUInt16(damaged, "OS/2", offset, 0);

        AssertSkiaAcceptsButTheAppRejects(damaged);
    }

    [Fact]
    public void LoadFromFile_TruncatedCopiesOfARealFont_LoadSafelyOrThrowFontLoadException()
    {
        int[] lengths = [0, 4, 12, 100, 1000, _realFont.Length / 4, _realFont.Length / 2, _realFont.Length - 1];

        foreach (var length in lengths)
            AssertLoadsSafelyOrIsRejected(_realFont[..length], $"truncated to {length} bytes");
    }

    [Fact]
    public void LoadFromFile_CorruptedCopiesOfARealFont_LoadSafelyOrThrowFontLoadException()
    {
        // A fixed seed, so a failure can be reproduced.
        var random = new Random(1601);
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var damaged = (byte[])_realFont.Clone();
            for (var change = 0; change < 20; change++)
                damaged[random.Next(damaged.Length)] = (byte)random.Next(256);

            AssertLoadsSafelyOrIsRejected(damaged, $"corrupted copy {attempt}");
        }
    }

    [Fact]
    public void FindProblem_BundledFonts_FindsNone()
    {
        foreach (var bundled in BundledFonts.All)
        {
            using var font = FontLoader.LoadBundled(bundled);

            Assert.Null(FontLoader.FindProblem(font));
        }
    }

    private void AssertSkiaAcceptsButTheAppRejects(byte[] fontData)
    {
        // Skia itself must accept the file, or this wouldn't be testing the app's own check.
        using (var data = SKData.CreateCopy(fontData))
        using (var font = new LoadedFont(SKTypeface.FromData(data)!, FontOrigin.File))
            Assert.NotNull(FontLoader.FindProblem(font));

        Assert.Throws<FontLoadException>(() => FontLoader.LoadFromFile(Write(fontData)));
    }

    private void AssertLoadsSafelyOrIsRejected(byte[] fontData, string description)
    {
        var path = Write(fontData);
        LoadedFont font;
        try
        {
            font = FontLoader.LoadFromFile(path);
        }
        catch (FontLoadException)
        {
            return;
        }

        using (font)
        {
            var record = Record.Exception(() => UseEverywhere(font));
            Assert.True(record is null, $"The font {description} loaded but then failed: {record}");
        }
    }

    // Everything the app does with a document's font.
    private static void UseEverywhere(LoadedFont font)
    {
        MonospaceCheck.Run(font);
        foreach (var pixelSize in new[] { CellMetrics.MinimumPixelSize, 16, 128 })
        {
            CellMetrics.Measure(font, pixelSize);
            using var atlas = new GlyphAtlas(font, pixelSize, GlyphRenderOptions.Default,
                FixedFallbackFontProvider.None);
            for (var character = ' '; character <= '~'; character++)
                atlas.GetOrAdd(new Rune(character));

            atlas.GetOrAdd(new Rune('█'));
            atlas.GetOrAdd(TestFonts.NoFontHasThis);
        }
    }

    private string Write(byte[] fontData)
    {
        var path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, fontData);
        return path;
    }

    private static byte[] ReadBundledFont()
    {
        using var stream = BundledFonts.Default.OpenFontStream();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}

/// <summary>
/// Changes values in an OpenType font file and fixes its checksums, so font engines that check
/// them see a well-formed file with odd values.
/// </summary>
internal static class FontFileEditor
{
    public const int HeadUnitsPerEmOffset = 18;

    // The ascender, descender and line gap, from the OpenType specification.
    public static IReadOnlyList<int> HheaVerticalMetricOffsets { get; } = [4, 6, 8];

    public static IReadOnlyList<int> Os2TypoMetricOffsets { get; } = [68, 70, 72];

    private const int TableCountOffset = 4;
    private const int TableRecordsOffset = 12;
    private const int TableRecordSize = 16;
    private const int HeadChecksumAdjustmentOffset = 8;
    private const uint ChecksumMagic = 0xB1B0AFBA;

    public static byte[] SetUInt16(byte[] font, string tableTag, int offsetInTable, ushort value)
    {
        var edited = (byte[])font.Clone();
        var (recordOffset, tableOffset, tableLength) = FindTable(edited, tableTag);
        BinaryPrimitives.WriteUInt16BigEndian(edited.AsSpan(tableOffset + offsetInTable), value);

        var (_, headOffset, headLength) = FindTable(edited, "head");
        var adjustment = edited.AsSpan(headOffset + HeadChecksumAdjustmentOffset, sizeof(uint));
        adjustment.Clear();

        BinaryPrimitives.WriteUInt32BigEndian(edited.AsSpan(recordOffset + 4),
            Checksum(edited, tableOffset, tableLength));
        if (tableTag != "head")
        {
            var (headRecordOffset, _, _) = FindTable(edited, "head");
            BinaryPrimitives.WriteUInt32BigEndian(edited.AsSpan(headRecordOffset + 4),
                Checksum(edited, headOffset, headLength));
        }

        BinaryPrimitives.WriteUInt32BigEndian(adjustment, ChecksumMagic - Checksum(edited, 0, edited.Length));
        return edited;
    }

    private static (int RecordOffset, int TableOffset, int TableLength) FindTable(byte[] font, string tag)
    {
        var tagValue = BinaryPrimitives.ReadUInt32BigEndian(Encoding.ASCII.GetBytes(tag));
        var tableCount = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(TableCountOffset));
        for (var index = 0; index < tableCount; index++)
        {
            var record = TableRecordsOffset + index * TableRecordSize;
            if (BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(record)) != tagValue)
                continue;

            var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(record + 8));
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(record + 12));
            return (record, offset, length);
        }

        throw new InvalidOperationException($"The font has no '{tag}' table.");
    }

    // The sum of the data as big-endian 32-bit words, padded with zeros to a whole word.
    private static uint Checksum(byte[] font, int offset, int length)
    {
        uint sum = 0;
        Span<byte> word = stackalloc byte[sizeof(uint)];
        for (var position = offset; position < offset + length; position += sizeof(uint))
        {
            word.Clear();
            var available = Math.Min(sizeof(uint), font.Length - position);
            font.AsSpan(position, available).CopyTo(word);
            sum += BinaryPrimitives.ReadUInt32BigEndian(word);
        }

        return sum;
    }
}
