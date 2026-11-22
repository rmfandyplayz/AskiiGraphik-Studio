// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// How dark each glyph looks in one font, and where its ink lies, as the canvas draws it (ARCHITECTURE §7, ADRs 0045
/// and 0068). Imaging measures both by drawing each glyph; Core tests use a fake.
/// </summary>
public interface IGlyphDensityProvider
{
    /// <summary>How much of its cell <paramref name="glyph"/> inks, from 0 to 1.</summary>
    double GetDensity(Rune glyph);

    /// <summary>Where in its cell <paramref name="glyph"/>'s ink lies.</summary>
    GlyphShape GetShape(Rune glyph);
}
