// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// The current values of one <see cref="ToolOptionGroup"/>, which start at the options' defaults. Shared by every
/// document and every tool made for the group, so a tool reads them when it needs them rather than copying them.
/// </summary>
public sealed class ToolOptionValues
{
    private readonly Dictionary<string, int> _numbers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _choices = new(StringComparer.Ordinal);

    /// <summary>Creates values for <paramref name="group"/>, each at its default.</summary>
    /// <exception cref="ArgumentException">Two of the group's options share an id.</exception>
    public ToolOptionValues(ToolOptionGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in group.Options)
        {
            if (!ids.Add(option.Id))
                throw new ArgumentException($"Two options have the id '{option.Id}'.", nameof(group));

            switch (option)
            {
                case NumberToolOption number:
                    _numbers.Add(number.Id, number.DefaultValue);
                    break;
                case ChoiceToolOption choice:
                    _choices.Add(choice.Id, choice.DefaultValue);
                    break;
                default:
                    throw new ArgumentException($"'{option.Id}' is an unknown kind of option.", nameof(group));
            }
        }

        Group = group;
    }

    /// <summary>Raised after any value changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The options these are the values of.</summary>
    public ToolOptionGroup Group { get; }

    /// <summary>The value of <paramref name="option"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="option"/> isn't one of the group's.</exception>
    public int Get(NumberToolOption option)
    {
        ThrowIfNotInGroup(option);
        return _numbers[option.Id];
    }

    /// <summary>The value of <paramref name="option"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="option"/> isn't one of the group's.</exception>
    public string Get(ChoiceToolOption option)
    {
        ThrowIfNotInGroup(option);
        return _choices[option.Id];
    }

    /// <summary>
    /// Sets <paramref name="option"/> to <paramref name="value"/>, moved into its range if it's outside.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="option"/> isn't one of the group's.</exception>
    public void Set(NumberToolOption option, int value)
    {
        ThrowIfNotInGroup(option);

        var clamped = option.Clamp(value);
        if (_numbers[option.Id] == clamped)
            return;

        _numbers[option.Id] = clamped;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Chooses <paramref name="value"/> for <paramref name="option"/>.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="option"/> isn't one of the group's, or <paramref name="value"/> isn't one of its choices that
    /// can be chosen.
    /// </exception>
    public void Set(ChoiceToolOption option, string value)
    {
        ThrowIfNotInGroup(option);
        if (!option.Allows(value))
            throw new ArgumentException($"'{value}' isn't a choice of '{option.Id}'.", nameof(value));

        if (_choices[option.Id] == value)
            return;

        _choices[option.Id] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Compared by reference: an option with the same id from another group is a different option.
    private void ThrowIfNotInGroup(ToolOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (!Group.Options.Any(known => ReferenceEquals(known, option)))
            throw new ArgumentException($"'{option.Id}' isn't an option of '{Group.Id}'.", nameof(option));
    }
}
