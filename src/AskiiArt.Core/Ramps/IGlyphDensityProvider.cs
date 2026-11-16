// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// How dark each glyph looks in one font: the share of its cell the glyph inks, from 0 (none) to 1 (all of it), as
/// the canvas draws it (ARCHITECTURE §7, ADR 0045). Imaging measures it by drawing each glyph; Core tests use a fake.
/// </summary>
public interface IGlyphDensityProvider
{
    /// <summary>How much of its cell <paramref name="glyph"/> inks, from 0 to 1.</summary>
    double GetDensity(Rune glyph);
}
