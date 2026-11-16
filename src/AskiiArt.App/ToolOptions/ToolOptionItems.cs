// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

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

/// <summary>A number, shown as a slider.</summary>
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

    public override void Refresh() => OnPropertyChanged(nameof(Value));
}

/// <summary>A choice, shown as a row of buttons of which one is down.</summary>
internal sealed class ChoiceToolOptionItem : ToolOptionItem
{
    public ChoiceToolOptionItem(ChoiceToolOption option, ToolOptionValues values)
        : base(option)
    {
        Choices = [.. option.Choices.Select(choice => new ToolOptionChoiceItem(option, choice, values))];
    }

    public IReadOnlyList<ToolOptionChoiceItem> Choices { get; }

    public override void Refresh()
    {
        foreach (var choice in Choices)
            choice.Refresh();
    }
}

/// <summary>One of a choice option's buttons.</summary>
internal sealed class ToolOptionChoiceItem(ChoiceToolOption option, ToolOptionChoice choice, ToolOptionValues values)
    : ObservableObject
{
    public string Name { get; } = UiText.Get(choice.NameKey);

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
