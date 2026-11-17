// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tests.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools;

/// <summary>What tools are made with in tests.</summary>
internal static class TestTools
{
    /// <summary>A font in which every glyph measures as no ink.</summary>
    public static IGlyphDensityProvider NoInk { get; } = new FakeGlyphDensities(new Dictionary<char, double>());

    /// <summary>
    /// A font in which each glyph of the Standard ramp is a step darker than the last, 'A' is about as dark as '*'
    /// (level 6), and 'W' is darker than '@'.
    /// </summary>
    public static IGlyphDensityProvider RampSteps { get; } = new FakeGlyphDensities(new Dictionary<char, double>
    {
        ['.'] = 0.1,
        [':'] = 0.2,
        ['-'] = 0.3,
        ['='] = 0.4,
        ['+'] = 0.5,
        ['*'] = 0.6,
        ['#'] = 0.7,
        ['%'] = 0.8,
        ['@'] = 0.9,
        ['A'] = 0.62,
        ['W'] = 0.95
    });

    /// <summary>
    /// A context with fresh colours and settings, in a font with square cells in which every glyph measures as no ink,
    /// unless told otherwise.
    /// </summary>
    public static ToolContext Context(EditingSession session, double cellWidthToHeight = 1,
        IGlyphDensityProvider? densities = null) =>
        new(session, new PaintColors(), new ToolSettings(), () => cellWidthToHeight, () => densities ?? NoInk);

    /// <summary>
    /// A tip that follows <paramref name="values"/>, in a font with square cells unless told otherwise.
    /// </summary>
    public static BrushTipSource Tip(ToolOptionValues values, double cellWidthToHeight = 1) =>
        new(values, () => cellWidthToHeight);

    /// <summary>Sets <paramref name="values"/> to a mathematical tip of <paramref name="size"/> and shape.</summary>
    public static void SetTip(ToolOptionValues values, int size, string shape = "square")
    {
        values.Set(BrushTipOptions.Size, size);
        values.Set(BrushTipOptions.Shape, shape);
        values.Set(BrushTipOptions.Proportions, "mathematical");
    }
}
