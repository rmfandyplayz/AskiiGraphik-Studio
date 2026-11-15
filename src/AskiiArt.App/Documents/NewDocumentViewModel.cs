// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Documents;

/// <summary>
/// The New dialog: the canvas size, which characters the document may hold (ADR 0016) and which colours
/// (ADR 0036).
/// </summary>
internal sealed class NewDocumentViewModel : ObservableObject
{
    // 80 × 25 and ASCII only, chosen with the owner in step 1.6: the app is for ASCII art.
    public CanvasSizeFields Size { get; } = new(NewDocumentSettings.Default.Size);

    public bool IsAsciiOnly
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsUnicode));
        }
    } = NewDocumentSettings.Default.CharacterSet == CharacterSet.Ascii;

    /// <summary>The other choice of the pair; the two radio buttons bind to one each.</summary>
    public bool IsUnicode
    {
        get => !IsAsciiOnly;
        set => IsAsciiOnly = !value;
    }

    // Monochrome by default, chosen with the owner in M2b planning: colour is a choice, not the norm.
    public ColorMode ColorMode
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsMonochrome));
            OnPropertyChanged(nameof(IsAnsi));
            OnPropertyChanged(nameof(IsRgb));
        }
    } = NewDocumentSettings.Default.ColorMode;

    // One radio button binds to each. A button being unticked is another being ticked, so false is ignored.
    public bool IsMonochrome
    {
        get => ColorMode == ColorMode.Monochrome;
        set => ChooseColorModeIf(value, ColorMode.Monochrome);
    }

    public bool IsAnsi
    {
        get => ColorMode == ColorMode.Ansi;
        set => ChooseColorModeIf(value, ColorMode.Ansi);
    }

    public bool IsRgb
    {
        get => ColorMode == ColorMode.Rgb;
        set => ChooseColorModeIf(value, ColorMode.Rgb);
    }

    /// <summary>The settings to create the document with, or null while the size isn't valid.</summary>
    public NewDocumentSettings? Result
    {
        get
        {
            if (Size.Size is not { } size)
                return null;

            return new NewDocumentSettings(size, IsAsciiOnly ? CharacterSet.Ascii : CharacterSet.Unicode, ColorMode);
        }
    }

    private void ChooseColorModeIf(bool isChosen, ColorMode colorMode)
    {
        if (isChosen)
            ColorMode = colorMode;
    }
}
