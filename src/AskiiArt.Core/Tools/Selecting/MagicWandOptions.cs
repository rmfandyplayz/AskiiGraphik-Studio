// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>What the Magic Wand compares cells by (ADR 0063).</summary>
public enum MagicWandMatch
{
    /// <summary>The same character. A space and an empty cell look the same, so they match.</summary>
    Glyph,

    /// <summary>How much ink a character puts on its cell, in the document's font, within the tolerance.</summary>
    Lightness,

    /// <summary>The text colour, within the tolerance.</summary>
    TextColor,

    /// <summary>The background colour, within the tolerance.</summary>
    BackgroundColor
}

/// <summary>The Magic Wand's options, read at the start of a click.</summary>
/// <param name="IsContiguous">True to select only matching cells joined to the clicked one along their sides.</param>
/// <param name="SamplesVisibleLayers">True to look at every visible layer together, false at the active layer.</param>
/// <param name="Match">What cells are compared by.</param>
/// <param name="Tolerance">How far a cell may differ and still match, from 0 (not at all) to 100 percent.</param>
public readonly record struct MagicWandSettings(
    bool IsContiguous,
    bool SamplesVisibleLayers,
    MagicWandMatch Match,
    int Tolerance);

/// <summary>
/// The Magic Wand's own options (ADR 0063), besides the mode every selection tool shares: whether it selects only the
/// matching cells joined to the one clicked or every matching cell, whether it looks at the active layer or at every
/// visible layer together, what it compares, and how far a cell may differ and still match.
/// </summary>
public static class MagicWandOptions
{
    private const string Contiguous = "contiguous";
    private const string Global = "global";
    private const string ActiveLayer = "active-layer";
    private const string VisibleLayers = "visible-layers";
    private const string Glyph = "glyph";
    private const string Lightness = "lightness";
    private const string TextColor = "text-color";
    private const string BackgroundColor = "background-color";

    /// <summary>Only the matching cells joined to the clicked one, or every matching cell.</summary>
    public static ChoiceToolOption Area { get; } = new("area", "ToolOptionWandArea",
        [
            new(Contiguous, "ToolOptionWandContiguous"),
            new(Global, "ToolOptionWandGlobal")
        ],
        Contiguous);

    /// <summary>The active layer, or every visible layer as the canvas shows them together.</summary>
    public static ChoiceToolOption Sample { get; } = new("sample", "ToolOptionWandSample",
        [
            new(ActiveLayer, "ToolOptionWandActiveLayer"),
            new(VisibleLayers, "ToolOptionWandVisibleLayers")
        ],
        ActiveLayer);

    /// <summary>What cells are compared by.</summary>
    public static ChoiceToolOption Match { get; } = new("match", "ToolOptionWandMatch",
        [
            new(Glyph, "ToolOptionWandMatchGlyph"),
            new(Lightness, "ToolOptionWandMatchLightness"),
            new(TextColor, "ToolOptionWandMatchTextColor"),
            new(BackgroundColor, "ToolOptionWandMatchBackgroundColor")
        ],
        Glyph);

    /// <summary>How far a cell may differ and still match, in percent. Same Glyph ignores it.</summary>
    public static NumberToolOption Tolerance { get; } = new("tolerance", "ToolOptionWandTolerance", 0, 100, 10)
    {
        Unit = ToolOptionUnit.Percent
    };

    /// <summary>The Magic Wand's options.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.select.wand", [Area, Sample, Match, Tolerance]);

    /// <summary>The settings <paramref name="values"/> hold.</summary>
    public static MagicWandSettings Read(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var match = values.Get(Match) switch
        {
            Lightness => MagicWandMatch.Lightness,
            TextColor => MagicWandMatch.TextColor,
            BackgroundColor => MagicWandMatch.BackgroundColor,
            _ => MagicWandMatch.Glyph
        };

        return new MagicWandSettings(values.Get(Area) != Global, values.Get(Sample) == VisibleLayers, match,
            values.Get(Tolerance));
    }
}
