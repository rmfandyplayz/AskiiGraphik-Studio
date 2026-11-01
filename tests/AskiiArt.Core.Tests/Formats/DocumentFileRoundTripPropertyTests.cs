// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Tests.Commands;
using CsCheck;

namespace AskiiArt.Core.Tests.Formats;

/// <summary>
/// The file format's key invariant (ARCHITECTURE §19): saving and opening any document gives it
/// back unchanged, in both variants. CsCheck prints a seed when this fails, so it can be replayed.
/// </summary>
public sealed class DocumentFileRoundTripPropertyTests
{
    private static readonly Rune[] _asciiGlyphs = [.. " aZ09\\\"/#{}~".EnumerateRunes()];

    private static readonly Rune[] _unicodeGlyphs =
        [.. _asciiGlyphs, .. "é│█░ñ漢".EnumerateRunes(), new Rune(0x1F600)];

    private static readonly string[] _names = ["Layer 1", "", "名前", "quote \" and back\\slash", " spaced "];

    private static readonly string[] _fonts = ["JetBrains Mono", "Fira Code", "A Font Nobody Has"];

    [Fact]
    public void AnyDocument_SurvivesSavingAndOpening()
    {
        Gen.Select(Gen.Int[1, 9], Gen.Int[1, 6], Gen.Int[1, 4], Gen.Bool, Gen.Int).Sample(
            (width, height, layerCount, isAscii, seed) =>
            {
                var document = CreateDocument(width, height, layerCount, isAscii, new Random(seed));
                var expected = TestCommands.Snapshot(document);

                foreach (var variant in new[] { DocumentFileVariant.Plain, DocumentFileVariant.Compressed })
                {
                    var reopened = DocumentFileFormat.Decode(DocumentFileFormat.Encode(document, variant)).Document;
                    Assert.Equal(expected, TestCommands.Snapshot(reopened));
                }
            });
    }

    private static Document CreateDocument(int width, int height, int layerCount, bool isAscii, Random random)
    {
        var characterSet = isAscii ? CharacterSet.Ascii : CharacterSet.Unicode;
        var glyphs = isAscii ? _asciiGlyphs : _unicodeGlyphs;
        var document = Document.Create(new CanvasSize(width, height), Pick(random, _names), characterSet,
            new FontReference(Pick(random, _fonts)));
        for (var index = 1; index < layerCount; index++)
            TestCells.AddLayer(document, Pick(random, _names));

        foreach (var layer in document.ActiveFrame.Layers)
        {
            layer.SetProperties(new LayerProperties(layer.Properties.Name)
            {
                IsVisible = random.Next(2) == 0,
                IsLocked = random.Next(2) == 0,
                Opacity = random.Next(3) == 0 ? random.NextDouble() : 1.0
            });

            for (var index = 0; index < width * height; index++)
            {
                // About a third of the cells stay empty.
                if (random.Next(3) != 0)
                    layer.SetCell(index, Cell.FromGlyph(Pick(random, glyphs)));
            }
        }

        return document;
    }

    private static T Pick<T>(Random random, T[] choices) => choices[random.Next(choices.Length)];
}
