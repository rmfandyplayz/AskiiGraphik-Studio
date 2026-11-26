// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// One setting a tool offers, described as data (ADR 0047), so the Tool Options toolbar can be built from it and the
/// values can be saved and exported without knowing the tool.
/// </summary>
/// <param name="Id">A stable id within its group, for example "size". Never shown to the user.</param>
/// <param name="NameKey">The App's resource key for the option's localized name.</param>
public abstract record ToolOption(string Id, string NameKey);

/// <summary>What a <see cref="NumberToolOption"/> counts, which says how its value is shown.</summary>
public enum ToolOptionUnit
{
    /// <summary>A plain number, such as a size in cells.</summary>
    None,

    /// <summary>A percentage, from 0 to 100.</summary>
    Percent
}

/// <summary>A whole number between <see cref="Minimum"/> and <see cref="Maximum"/>, inclusive.</summary>
public sealed record NumberToolOption : ToolOption
{
    /// <summary>Creates a number option.</summary>
    /// <exception cref="ArgumentException">The range is empty, or the default lies outside it.</exception>
    public NumberToolOption(string id, string nameKey, int minimum, int maximum, int defaultValue)
        : base(id, nameKey)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimum, maximum);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultValue, minimum);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultValue, maximum);

        Minimum = minimum;
        Maximum = maximum;
        DefaultValue = defaultValue;
    }

    /// <summary>The smallest value.</summary>
    public int Minimum { get; }

    /// <summary>The largest value.</summary>
    public int Maximum { get; }

    /// <summary>The value until the user changes it.</summary>
    public int DefaultValue { get; }

    /// <summary>What the value counts, which says how it's shown.</summary>
    public ToolOptionUnit Unit { get; init; }

    /// <summary><paramref name="value"/>, moved into the range if it's outside.</summary>
    public int Clamp(int value) => Math.Clamp(value, Minimum, Maximum);
}

/// <summary>One of the choices of a <see cref="ChoiceToolOption"/>.</summary>
/// <param name="Value">A stable value, for example "circle". Never shown to the user.</param>
/// <param name="NameKey">
/// The App's resource key for the choice's localized name. Empty for a choice with a <see cref="Name"/>.
/// </param>
public sealed record ToolOptionChoice(string Value, string NameKey)
{
    /// <summary>
    /// A name shown exactly as it is, never translated, such as the name the user gave a ramp. When it's set, it's
    /// shown instead of <see cref="NameKey"/>'s.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>Text shown after the name exactly as it is, never translated, such as a ramp's characters.</summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Whether it can be chosen. One that can't is shown, but greyed out, as a choice that's coming (ADR 0049).
    /// </summary>
    public bool IsAvailable { get; init; } = true;
}

/// <summary>A choice of one of <see cref="Choices"/>.</summary>
public sealed record ChoiceToolOption : ToolOption
{
    /// <summary>Creates a choice option.</summary>
    /// <exception cref="ArgumentException">
    /// There are no choices, two share a value, or the default isn't one of them that can be chosen.
    /// </exception>
    public ChoiceToolOption(string id, string nameKey, IReadOnlyList<ToolOptionChoice> choices, string defaultValue)
        : base(id, nameKey)
    {
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count == 0)
            throw new ArgumentException("A choice option needs at least one choice.", nameof(choices));

        if (choices.Select(choice => choice.Value).Distinct(StringComparer.Ordinal).Count() != choices.Count)
            throw new ArgumentException("Two choices share a value.", nameof(choices));

        Choices = choices;
        if (!Allows(defaultValue))
            throw new ArgumentException($"'{defaultValue}' isn't a choice that can be chosen.", nameof(defaultValue));

        DefaultValue = defaultValue;
    }

    /// <summary>
    /// The choices that are always there, in the order they're shown. <see cref="MoreChoicesFrom"/> can add more.
    /// </summary>
    public IReadOnlyList<ToolOptionChoice> Choices { get; }

    /// <summary>
    /// The id of an <see cref="IToolOptionChoiceSource"/> whose choices follow <see cref="Choices"/>, such as the
    /// user's ramps, or null when there's none. <see cref="ToolSettings"/> knows the sources; values made without
    /// them offer only <see cref="Choices"/>.
    /// </summary>
    public string? MoreChoicesFrom { get; init; }

    /// <summary>The chosen value until the user changes it.</summary>
    public string DefaultValue { get; }

    /// <summary>
    /// Whether the Tool Options toolbar shows every choice as a button, however many there are, for choices that are
    /// switched often. Otherwise it decides by how many there are.
    /// </summary>
    public bool IsShownAsButtons { get; init; }

    /// <summary>
    /// Whether <paramref name="value"/> is one of <see cref="Choices"/>, and one that can be chosen. It doesn't look at
    /// <see cref="MoreChoicesFrom"/>'s: <see cref="ToolOptionValues.Allows"/> does.
    /// </summary>
    public bool Allows(string value) => Choices.Any(choice => choice.Value == value && choice.IsAvailable);
}

/// <summary>An option that's on or off, such as a rectangle's rounded corners (ADR 0080).</summary>
public sealed record ToggleToolOption(string Id, string NameKey, bool DefaultValue) : ToolOption(Id, NameKey);

/// <summary>
/// A character, such as the one the Character Brush paints (ADR 0053): one that fills a cell and shows ink
/// (<see cref="GlyphSet.CanHold"/>). The Tool Options toolbar shows it in a box that opens the Characters window.
/// </summary>
public sealed record GlyphToolOption : ToolOption
{
    /// <summary>Creates a character option.</summary>
    /// <exception cref="ArgumentException">The default can't be chosen.</exception>
    public GlyphToolOption(string id, string nameKey, Rune defaultValue)
        : base(id, nameKey)
    {
        if (!Allows(defaultValue))
            throw new ArgumentException($"U+{defaultValue.Value:X4} can't be chosen.", nameof(defaultValue));

        DefaultValue = defaultValue;
    }

    /// <summary>The character until the user changes it.</summary>
    public Rune DefaultValue { get; }

    /// <summary>Whether <paramref name="value"/> can be chosen.</summary>
    public static bool Allows(Rune value) => GlyphSet.CanHold(value);
}

/// <summary>
/// Options that tools share one set of values for. The tools of a tool group share some, so switching between them
/// keeps those settings (ADR 0047), and a tool can have options of its own besides (ADR 0049).
/// </summary>
/// <param name="Id">A stable id, for example "askiiart.brush". Never shown to the user.</param>
/// <param name="Options">The options, in the order the Tool Options toolbar shows them.</param>
public sealed record ToolOptionGroup(string Id, IReadOnlyList<ToolOption> Options);
