// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Documents;

/// <summary>One of the nine anchor buttons, laid out three by three in the dialog.</summary>
/// <param name="Anchor">The edge, corner or centre that stays put.</param>
/// <param name="Symbol">The arrow drawn on the button, pointing at that spot.</param>
/// <param name="Name">The anchor's name, for the tooltip and screen readers.</param>
internal sealed record ResizeAnchorOption(ResizeAnchor Anchor, string Symbol, string Name);

/// <summary>
/// The Resize Canvas dialog: the new size and the anchor that stays put (ARCHITECTURE §5.7).
/// </summary>
internal sealed class ResizeCanvasViewModel : ObservableObject
{
    private readonly CanvasSize _currentSize;

    public ResizeCanvasViewModel(CanvasSize currentSize)
    {
        _currentSize = currentSize;
        Size = new CanvasSizeFields(currentSize);
        Size.PropertyChanged += OnSizePropertyChanged;
        SelectedAnchor = Anchors.Single(option => option.Anchor == ResizeAnchor.Center);
    }

    public CanvasSizeFields Size { get; }

    /// <summary>The nine anchors, in reading order: top left first, bottom right last.</summary>
    public IReadOnlyList<ResizeAnchorOption> Anchors { get; } =
    [
        new(ResizeAnchor.TopLeft, Strings.ResizeAnchorTopLeftSymbol, Strings.ResizeAnchorTopLeft),
        new(ResizeAnchor.Top, Strings.ResizeAnchorTopSymbol, Strings.ResizeAnchorTop),
        new(ResizeAnchor.TopRight, Strings.ResizeAnchorTopRightSymbol, Strings.ResizeAnchorTopRight),
        new(ResizeAnchor.Left, Strings.ResizeAnchorLeftSymbol, Strings.ResizeAnchorLeft),
        new(ResizeAnchor.Center, Strings.ResizeAnchorCenterSymbol, Strings.ResizeAnchorCenter),
        new(ResizeAnchor.Right, Strings.ResizeAnchorRightSymbol, Strings.ResizeAnchorRight),
        new(ResizeAnchor.BottomLeft, Strings.ResizeAnchorBottomLeftSymbol, Strings.ResizeAnchorBottomLeft),
        new(ResizeAnchor.Bottom, Strings.ResizeAnchorBottomSymbol, Strings.ResizeAnchorBottom),
        new(ResizeAnchor.BottomRight, Strings.ResizeAnchorBottomRightSymbol, Strings.ResizeAnchorBottomRight)
    ];

    /// <summary>The anchor that stays put; the centre unless the user picks another.</summary>
    public ResizeAnchorOption? SelectedAnchor
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(Result));
        }
    }

    /// <summary>True when the new size is smaller on either side, so some cells are cut off.</summary>
    public bool CropsCanvas
    {
        get
        {
            if (Size.Size is not { } size)
                return false;

            return size.Width < _currentSize.Width || size.Height < _currentSize.Height;
        }
    }

    /// <summary>The resize to make, or null while the size isn't valid or no anchor is picked.</summary>
    public CanvasResizeRequest? Result
    {
        get
        {
            if (Size.Size is not { } size || SelectedAnchor is null)
                return null;

            return new CanvasResizeRequest(size, SelectedAnchor.Anchor);
        }
    }

    public bool CanAccept => Result is not null;

    private void OnSizePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CanvasSizeFields.Size))
            return;

        OnPropertyChanged(nameof(CropsCanvas));
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(CanAccept));
    }
}
