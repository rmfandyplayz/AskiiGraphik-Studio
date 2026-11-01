// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Documents;

/// <summary>The New dialog: the canvas size and which characters the document may hold (ADR 0016).</summary>
internal sealed class NewDocumentViewModel : ObservableObject
{
    /// <summary>The size a new document starts at, chosen with the owner in step 1.6.</summary>
    public static CanvasSize DefaultSize { get; } = new(80, 25);

    public CanvasSizeFields Size { get; } = new(DefaultSize);

    /// <summary>ASCII only is the default: the app is for ASCII art (the owner's choice in step 1.6).</summary>
    public bool IsAsciiOnly
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsUnicode));
        }
    } = true;

    /// <summary>The other choice of the pair; the two radio buttons bind to one each.</summary>
    public bool IsUnicode
    {
        get => !IsAsciiOnly;
        set => IsAsciiOnly = !value;
    }

    /// <summary>The settings to create the document with, or null while the size isn't valid.</summary>
    public NewDocumentSettings? Result
    {
        get
        {
            if (Size.Size is not { } size)
                return null;

            return new NewDocumentSettings(size, IsAsciiOnly ? CharacterSet.Ascii : CharacterSet.Unicode);
        }
    }
}
