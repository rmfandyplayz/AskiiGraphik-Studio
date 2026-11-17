// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Resources;
using AskiiArt.Core.Tools.Options;
using CommunityToolkit.Mvvm.ComponentModel;

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
    public static ToolOptionItem Create(ToolOption option, ToolOptionValues values) => option switch
    {
        NumberToolOption number => new NumberToolOptionItem(number, values),
        ChoiceToolOption choice => new ChoiceToolOptionItem(choice, values),
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

/// <summary>A choice, shown as a row of buttons of which one is down, or as a dropdown when there are many.</summary>
internal sealed class ChoiceToolOptionItem : ToolOptionItem
{
    // More choices than this make a dropdown. A placeholder rule (docs/TEMPORARY-UI.md).
    private const int MostChoicesAsButtons = 2;

    public ChoiceToolOptionItem(ChoiceToolOption option, ToolOptionValues values)
        : base(option)
    {
        Choices = [.. option.Choices.Select(choice => new ToolOptionChoiceItem(option, choice, values))];
    }

    public IReadOnlyList<ToolOptionChoiceItem> Choices { get; }

    /// <summary>Whether the toolbar shows the choices as a dropdown rather than as a row of buttons.</summary>
    public bool IsDropDown => Choices.Count > MostChoicesAsButtons;

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
        foreach (var choice in Choices)
            choice.Refresh();

        OnPropertyChanged(nameof(SelectedChoice));
    }
}

/// <summary>One of a choice option's buttons, or one of its dropdown's items.</summary>
internal sealed class ToolOptionChoiceItem(ChoiceToolOption option, ToolOptionChoice choice, ToolOptionValues values)
    : ObservableObject
{
    public string Name { get; } = UiText.Get(choice.NameKey);

    /// <summary>
    /// More about the choice, shown after its name, such as a ramp's characters. Null when there's none.
    /// </summary>
    public string? Detail => choice.Detail;

    /// <summary>Whether there's a <see cref="Detail"/>.</summary>
    public bool HasDetail => Detail is not null;

    /// <summary>Whether it can be chosen. One that can't yet is shown greyed out.</summary>
    public bool IsAvailable => choice.IsAvailable;

    /// <summary>
    /// Whether this is the choice. Setting it chooses it. A button can't be unchosen by itself, only by choosing
    /// another, so setting false just shows the truth again.
    /// </summary>
    public bool IsChosen
    {
        get => values.Get(option) == choice.Value;
        set
        {
            if (value)
                values.Set(option, choice.Value);
            else
                OnPropertyChanged();
        }
    }

    public void Refresh() => OnPropertyChanged(nameof(IsChosen));
}
