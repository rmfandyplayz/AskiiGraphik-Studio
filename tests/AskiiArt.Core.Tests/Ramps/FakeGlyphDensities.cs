// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

/// <summary>
/// Densities set by hand; a glyph it wasn't given measures 0. A glyph's ink is spread evenly over its cell, unless
/// <paramref name="shapes"/> gives it a shape, whose ink then also sets its density.
/// </summary>
internal sealed class FakeGlyphDensities(
    IReadOnlyDictionary<char, double> densities,
    IReadOnlyDictionary<char, GlyphShape>? shapes = null) : IGlyphDensityProvider
{
    public double GetDensity(Rune glyph)
    {
        if (ShapeOf(glyph) is { } shape)
            return shape.Ink;

        return glyph.IsBmp && densities.TryGetValue((char)glyph.Value, out var density) ? density : 0;
    }

    public GlyphShape GetShape(Rune glyph) => ShapeOf(glyph) ?? GlyphShape.Even(GetDensity(glyph));

    private GlyphShape? ShapeOf(Rune glyph) =>
        glyph.IsBmp && shapes is not null && shapes.TryGetValue((char)glyph.Value, out var shape) ? shape : null;
}
