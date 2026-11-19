// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json.Nodes;

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// The current values of one <see cref="ToolOptionGroup"/>, which start at the options' defaults. Shared by every
/// document and every tool made for the group, so a tool reads them when it needs them rather than copying them.
/// </summary>
public sealed class ToolOptionValues
{
    private readonly Dictionary<string, int> _numbers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _choices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IToolOptionChoiceSource> _moreChoices = new(StringComparer.Ordinal);

    /// <summary>Creates values for <paramref name="group"/>, each at its default, with only the options' own choices.
    /// </summary>
    /// <exception cref="ArgumentException">Two of the group's options share an id.</exception>
    public ToolOptionValues(ToolOptionGroup group)
        : this(group, new Dictionary<string, IToolOptionChoiceSource>())
    {
    }

    /// <summary>
    /// Creates values for <paramref name="group"/>, each at its default. A choice option that names one of
    /// <paramref name="sources"/> also offers its choices, and goes back to its default when its choice goes. These
    /// values follow the sources from then on.
    /// </summary>
    /// <param name="group">The options.</param>
    /// <param name="sources">The sources of more choices, by id.</param>
    /// <exception cref="ArgumentException">Two of the group's options share an id.</exception>
    public ToolOptionValues(ToolOptionGroup group, IReadOnlyDictionary<string, IToolOptionChoiceSource> sources)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(sources);

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
                    if (choice.MoreChoicesFrom is { } sourceId && sources.TryGetValue(sourceId, out var source))
                        _moreChoices.Add(choice.Id, source);

                    break;
                default:
                    throw new ArgumentException($"'{option.Id}' is an unknown kind of option.", nameof(group));
            }
        }

        Group = group;
        foreach (var source in _moreChoices.Values.Distinct())
            source.Changed += OnSourceChanged;
    }

    /// <summary>Raised after any value changes.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Raised after a source's choices change (<see cref="ChoiceToolOption.MoreChoicesFrom"/>). When that takes away
    /// a chosen choice, the option goes back to its default first, and <see cref="Changed"/> is raised too.
    /// </summary>
    public event EventHandler? ChoicesChanged;

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

    /// <summary>
    /// The choices <paramref name="option"/> offers: its own, then its source's, in the order they're shown.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="option"/> isn't one of the group's.</exception>
    public IReadOnlyList<ToolOptionChoice> ChoicesOf(ChoiceToolOption option)
    {
        ThrowIfNotInGroup(option);
        if (!_moreChoices.TryGetValue(option.Id, out var source))
            return option.Choices;

        return [.. option.Choices, .. source.Choices];
    }

    /// <summary>
    /// Whether <paramref name="value"/> is one of the choices <paramref name="option"/> offers, and one that can be
    /// chosen.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="option"/> isn't one of the group's.</exception>
    public bool Allows(ChoiceToolOption option, string value)
    {
        ThrowIfNotInGroup(option);
        if (option.Allows(value))
            return true;

        return _moreChoices.TryGetValue(option.Id, out var source)
            && source.Choices.Any(choice => choice.Value == value && choice.IsAvailable);
    }

    /// <summary>Chooses <paramref name="value"/> for <paramref name="option"/>.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="option"/> isn't one of the group's, or <paramref name="value"/> isn't one of its choices that
    /// can be chosen.
    /// </exception>
    public void Set(ChoiceToolOption option, string value)
    {
        if (!Allows(option, value))
            throw new ArgumentException($"'{value}' isn't a choice of '{option.Id}'.", nameof(value));

        if (_choices[option.Id] == value)
            return;

        _choices[option.Id] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The values as JSON, an object with a property for each option: a number for a number, and a string for a
    /// choice. <see cref="ToolSettings"/> saves these.
    /// </summary>
    internal JsonObject ToJson()
    {
        var json = new JsonObject();
        foreach (var option in Group.Options)
        {
            json[option.Id] = option switch
            {
                NumberToolOption number => JsonValue.Create(_numbers[number.Id]),
                ChoiceToolOption choice => JsonValue.Create(_choices[choice.Id]),
                _ => null
            };
        }

        return json;
    }

    /// <summary>
    /// Sets the values that <paramref name="json"/>, made by <see cref="ToJson"/>, holds. A value of the wrong kind,
    /// or a choice that isn't there any more, is ignored, a number outside its range is moved into it, and an option
    /// it doesn't mention keeps its value.
    /// </summary>
    internal void Restore(JsonObject json)
    {
        foreach (var option in Group.Options)
        {
            if (json[option.Id] is not JsonValue saved)
                continue;

            switch (option)
            {
                case NumberToolOption number when saved.TryGetValue(out int count):
                    Set(number, count);
                    break;
                case ChoiceToolOption choice when saved.TryGetValue(out string? value) && Allows(choice, value):
                    Set(choice, value);
                    break;
            }
        }
    }

    // A choice that's gone is no use to anyone, so its option goes back to its default.
    private void OnSourceChanged(object? sender, EventArgs e)
    {
        var anyGone = false;
        foreach (var option in Group.Options.OfType<ChoiceToolOption>())
        {
            if (!_moreChoices.ContainsKey(option.Id) || Allows(option, _choices[option.Id]))
                continue;

            _choices[option.Id] = option.DefaultValue;
            anyGone = true;
        }

        if (anyGone)
            Changed?.Invoke(this, EventArgs.Empty);

        ChoicesChanged?.Invoke(this, EventArgs.Empty);
    }

    // Compared by reference: an option with the same id from another group is a different option.
    private void ThrowIfNotInGroup(ToolOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (!Group.Options.Any(known => ReferenceEquals(known, option)))
            throw new ArgumentException($"'{option.Id}' isn't an option of '{Group.Id}'.", nameof(option));
    }
}
