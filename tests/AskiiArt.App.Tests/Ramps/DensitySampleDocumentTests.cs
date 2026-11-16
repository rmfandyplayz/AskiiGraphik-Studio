// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Ramps;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.App.Tests.Ramps;

public sealed class DensitySampleDocumentTests : IDisposable
{
    private readonly LoadedFont _font = FontLoader.LoadBundled(BundledFonts.FiraCode);
    private readonly GlyphDensityMeter _meter;

    public DensitySampleDocumentTests()
    {
        _meter = new GlyphDensityMeter(_font, NoFallbackFonts.Instance);
    }

    public void Dispose()
    {
        _meter.Dispose();
        _font.Dispose();
    }

    [Fact]
    public void TheSample_IsAUnicodeDocumentInTheGivenFont()
    {
        var font = new FontReference("Fira Code");

        var document = DensitySampleDocument.Create("Layer", font, _meter);

        Assert.Equal(font, document.Font);
        Assert.Equal(CharacterSet.Unicode, document.CharacterSet);
    }

    [Fact]
    public void ThePrintableAscii_IsListedOnce_FromLightestToDarkest()
    {
        var lines = DensitySampleDocument.CreateLines(_meter);
        var listed = lines[lines.ToList().IndexOf(DensitySampleDocument.AsciiHeader) + 1].Trim();

        Assert.Equal(94, listed.Length);
        Assert.Equal(94, listed.Distinct().Count());
        var densities = listed.Select(character => _meter.GetDensity(new Rune(character))).ToList();
        Assert.Equal(densities.Order(), densities);
    }

    [Fact]
    public void EveryBuiltInRamp_IsPaintedFromNothingToItsDarkestGlyph()
    {
        var lines = DensitySampleDocument.CreateLines(_meter).ToList();
        var header = lines.FindIndex(line =>
            line.StartsWith(DensitySampleDocument.GradientsHeader, StringComparison.Ordinal));

        foreach (var (index, ramp) in BuiltInRamps.All.Index())
        {
            var painted = lines[header + 1 + index];
            Assert.StartsWith(UiText.Get(ramp.NameKey) + " ", painted.TrimStart(), StringComparison.Ordinal);
            var gradient = painted[(painted.IndexOf('|', StringComparison.Ordinal) + 1)..^1];

            Assert.Equal(DensitySampleDocument.GradientWidth, gradient.EnumerateRunes().Count());
            Assert.Equal(new Rune(' '), gradient.EnumerateRunes().First());
            Assert.Equal(ramp.Ramp.Glyphs[^1], gradient.EnumerateRunes().Last());
        }
    }

    private sealed class NoFallbackFonts : IFallbackFontProvider
    {
        public static NoFallbackFonts Instance { get; } = new();

        public LoadedFont? FindFontFor(Rune rune, LoadedFont primary) => null;
    }
}
