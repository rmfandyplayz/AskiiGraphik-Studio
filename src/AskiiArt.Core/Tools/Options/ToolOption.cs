// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

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
/// <param name="NameKey">The App's resource key for the choice's localized name.</param>
public sealed record ToolOptionChoice(string Value, string NameKey)
{
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

    /// <summary>The choices, in the order they're shown.</summary>
    public IReadOnlyList<ToolOptionChoice> Choices { get; }

    /// <summary>The chosen value until the user changes it.</summary>
    public string DefaultValue { get; }

    /// <summary>Whether <paramref name="value"/> is one of the choices, and one that can be chosen.</summary>
    public bool Allows(string value) => Choices.Any(choice => choice.Value == value && choice.IsAvailable);
}

/// <summary>
/// Options that tools share one set of values for. The tools of a tool group share some, so switching between them
/// keeps those settings (ADR 0047), and a tool can have options of its own besides (ADR 0049).
/// </summary>
/// <param name="Id">A stable id, for example "askiiart.brush". Never shown to the user.</param>
/// <param name="Options">The options, in the order the Tool Options toolbar shows them.</param>
public sealed record ToolOptionGroup(string Id, IReadOnlyList<ToolOption> Options);
