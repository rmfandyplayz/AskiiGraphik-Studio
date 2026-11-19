// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Characters;

/// <summary>One character in the Characters window's grid, which chooses it for the Character Brush.</summary>
internal sealed class GlyphItem : ObservableObject
{
    /// <summary>Creates the item for <paramref name="glyph"/>.</summary>
    /// <param name="glyph">The character.</param>
    /// <param name="choose">Chooses it, when it's clicked.</param>
    public GlyphItem(Rune glyph, Action<GlyphItem> choose)
    {
        Glyph = glyph;
        ChooseCommand = new RelayCommand(() => choose(this));
    }

    public Rune Glyph { get; }

    public string Text => Glyph.ToString();

    /// <summary>The character's code point, such as "U+0041".</summary>
    public string Tooltip => string.Format(CultureInfo.CurrentCulture, Strings.CharacterTooltipFormat,
        Glyph.Value.ToString("X4", CultureInfo.InvariantCulture));

    /// <summary>Whether the document can hold it. One that can't is greyed out.</summary>
    public bool IsAvailable
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Whether it's the Character Brush's character.</summary>
    public bool IsChosen
    {
        get;
        set => SetProperty(ref field, value);
    }

    public IRelayCommand ChooseCommand { get; }
}
