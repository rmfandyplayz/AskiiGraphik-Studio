// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

/// <summary>Densities set by hand; a glyph it wasn't given measures 0.</summary>
internal sealed class FakeGlyphDensities(IReadOnlyDictionary<char, double> densities) : IGlyphDensityProvider
{
    public double GetDensity(Rune glyph) =>
        glyph.IsBmp && densities.TryGetValue((char)glyph.Value, out var density) ? density : 0;
}
