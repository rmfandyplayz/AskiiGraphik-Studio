// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.Globalization;
using AskiiArt.App.Resources;
using AskiiArt.Core.Tools.Options;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.ToolOptions;

/// <summary>One option on the Tool Options toolbar, which reads and changes its group's shared value.</summary>
internal abstract class ToolOptionItem : ObservableObject
{
    protected ToolOptionItem(ToolOption option)
    {
        Name = UiText.Get(option.NameKey);
    }

    /// <summary>The option's localized name.</summary>
    public string Name { get; }

    /// <summary>Makes an item for <paramref name="option"/>, which changes <paramref name="values"/>.</summary>
    /// <param name="option">The option.</param>
    /// <param name="values">Its group's values.</param>
    /// <param name="showGlyphPicker">Shows where a character option's character is chosen, or null if nowhere.</param>
    public static ToolOptionItem Create(ToolOption option, ToolOptionValues values, Action? showGlyphPicker) =>
        option switch
        {
            NumberToolOption number => new NumberToolOptionItem(number, values),
            ChoiceToolOption choice => new ChoiceToolOptionItem(choice, values),
            GlyphToolOption glyph => new GlyphToolOptionItem(glyph, values, showGlyphPicker),
            _ => throw new ArgumentException($"The toolbar can't show '{option.Id}', an unknown kind of option.",
                nameof(option))
        };

    /// <summary>Says that the value may have changed.</summary>
    public abstract void Refresh();
}

/// <summary>A number, shown as a slider with its value beside it.</summary>
internal sealed class NumberToolOptionItem(NumberToolOption option, ToolOptionValues values)
    : ToolOptionItem(option)
{
    public int Minimum => option.Minimum;

    public int Maximum => option.Maximum;

    /// <summary>The value. Setting one outside the range sets the nearest end instead.</summary>
    public int Value
    {
        get => values.Get(option);
        set => values.Set(option, value);
    }

    /// <summary>The value as the toolbar shows it, such as "50%" for a percentage.</summary>
    public string ValueText
    {
        get
        {
            if (option.Unit == ToolOptionUnit.Percent)
                return string.Format(CultureInfo.CurrentCulture, Strings.ToolOptionPercentFormat, Value);

            return Value.ToString(CultureInfo.CurrentCulture);
        }
    }

    public override void Refresh()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(ValueText));
    }
}

/// <summary>
/// A character, such as the Character Brush's (ADR 0053), shown in a box. Clicking the box shows where it's chosen: the
/// Characters window.
/// </summary>
internal sealed class GlyphToolOptionItem(GlyphToolOption option, ToolOptionValues values, Action? showPicker)
    : ToolOptionItem(option)
{
    /// <summary>The character.</summary>
    public string GlyphText => values.Get(option).ToString();

    /// <summary>The character's code point, such as "U+0041".</summary>
    public string Tooltip => string.Format(CultureInfo.CurrentCulture, Strings.CharacterTooltipFormat,
        values.Get(option).Value.ToString("X4", CultureInfo.InvariantCulture));

    /// <summary>Shows where the character is chosen. It can't run when there's nowhere.</summary>
    public IRelayCommand ShowPickerCommand { get; } =
        new RelayCommand(() => showPicker?.Invoke(), () => showPicker is not null);

    public override void Refresh()
    {
        OnPropertyChanged(nameof(GlyphText));
        OnPropertyChanged(nameof(Tooltip));
    }
}

/// <summary>
/// A choice, shown as a row of buttons of which one is down, or as a dropdown when there are many and the option
/// doesn't ask for buttons. Its choices can change while it's shown, as the user's ramps do (ADR 0052).
/// </summary>
internal sealed class ChoiceToolOptionItem : ToolOptionItem
{
    // More choices than this make a dropdown. A placeholder rule (docs/TEMPORARY-UI.md).
    private const int MostChoicesAsButtons = 2;

    private readonly ChoiceToolOption _option;
    private readonly ToolOptionValues _values;
    private readonly ObservableCollection<ToolOptionChoiceItem> _choices;

    public ChoiceToolOptionItem(ChoiceToolOption option, ToolOptionValues values)
        : base(option)
    {
        _option = option;
        _values = values;
        _choices = [.. values.ChoicesOf(option).Select(CreateChoiceItem)];
        Choices = new ReadOnlyObservableCollection<ToolOptionChoiceItem>(_choices);
    }

    /// <summary>
    /// The choices, in order. When they change, the items are updated, added and removed in place rather than
    /// replaced, since a dropdown whose items are all replaced loses its selection.
    /// </summary>
    public ReadOnlyObservableCollection<ToolOptionChoiceItem> Choices { get; }

    /// <summary>Whether the toolbar shows the choices as a dropdown rather than as a row of buttons.</summary>
    public bool IsDropDown => !_option.IsShownAsButtons && Choices.Count > MostChoicesAsButtons;

    /// <summary>
    /// The chosen choice, for the dropdown. Setting it chooses it, unless it can't be chosen yet, which just shows the
    /// chosen one again.
    /// </summary>
    public ToolOptionChoiceItem? SelectedChoice
    {
        get => Choices.FirstOrDefault(choice => choice.IsChosen);
        set
        {
            if (value is { IsAvailable: true })
                value.IsChosen = true;
            else
                OnPropertyChanged();
        }
    }

    public override void Refresh()
    {
        var choices = _values.ChoicesOf(_option);
        var wasDropDown = IsDropDown;
        UpdateAndAddChoices(choices);

        foreach (var choice in _choices)
            choice.Refresh();

        // The dropdown moves to the chosen item before a gone one is removed, so it never has nothing to show.
        OnPropertyChanged(nameof(SelectedChoice));
        RemoveGoneChoices(choices);

        if (IsDropDown != wasDropDown)
            OnPropertyChanged(nameof(IsDropDown));
    }

    private ToolOptionChoiceItem CreateChoiceItem(ToolOptionChoice choice) => new(_option, choice, _values);

    // Each choice that's still there keeps its item, updated, and a new one gets an item after the choice before it.
    private void UpdateAndAddChoices(IReadOnlyList<ToolOptionChoice> choices)
    {
        var index = 0;
        foreach (var choice in choices)
        {
            var existing = IndexOfChoice(choice.Value);
            if (existing < 0)
            {
                _choices.Insert(index, CreateChoiceItem(choice));
                index++;
                continue;
            }

            _choices[existing].Update(choice);
            index = Math.Max(index, existing + 1);
        }
    }

    private void RemoveGoneChoices(IReadOnlyList<ToolOptionChoice> choices)
    {
        for (var index = _choices.Count - 1; index >= 0; index--)
        {
            if (!choices.Any(choice => choice.Value == _choices[index].Value))
                _choices.RemoveAt(index);
        }

        // Sources keep their order, so this only moves anything if one reorders its choices.
        for (var index = 0; index < choices.Count; index++)
        {
            var current = IndexOfChoice(choices[index].Value);
            if (current != index)
                _choices.Move(current, index);
        }
    }

    private int IndexOfChoice(string value)
    {
        for (var index = 0; index < _choices.Count; index++)
        {
            if (_choices[index].Value == value)
                return index;
        }

        return -1;
    }
}

/// <summary>
/// One of a choice option's buttons, or one of its dropdown's items. Its name and detail can change while it's shown,
/// as a user ramp's do.
/// </summary>
internal sealed class ToolOptionChoiceItem(ChoiceToolOption option, ToolOptionChoice choice, ToolOptionValues values)
    : ObservableObject
{
    private ToolOptionChoice _choice = choice;

    /// <summary>The value this item chooses, which never changes.</summary>
    public string Value => _choice.Value;

    public string Name => _choice.Name ?? UiText.Get(_choice.NameKey);

    /// <summary>
    /// More about the choice, shown after its name, such as a ramp's characters. Null when there's none.
    /// </summary>
    public string? Detail => _choice.Detail;

    /// <summary>Whether there's a <see cref="Detail"/>.</summary>
    public bool HasDetail => Detail is not null;

    /// <summary>Whether it can be chosen. One that can't yet is shown greyed out.</summary>
    public bool IsAvailable => _choice.IsAvailable;

    /// <summary>
    /// Whether this is the choice. Setting it chooses it. A button can't be unchosen by itself, only by choosing
    /// another, so setting false just shows the truth again.
    /// </summary>
    public bool IsChosen
    {
        get => values.Get(option) == Value;
        set
        {
            if (value)
                values.Set(option, Value);
            else
                OnPropertyChanged();
        }
    }

    public void Refresh() => OnPropertyChanged(nameof(IsChosen));

    /// <summary>Shows <paramref name="updated"/>, the same choice with a new name, detail or availability.</summary>
    public void Update(ToolOptionChoice updated)
    {
        if (updated == _choice)
            return;

        _choice = updated;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(IsAvailable));
    }
}
