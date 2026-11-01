// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Documents;

/// <summary>
/// The width and height boxes of the New and Resize Canvas dialogs. Sizes above the soft limit
/// get a warning but are allowed; sizes outside the hard limits aren't (ARCHITECTURE §5.7).
/// </summary>
/// <remarks>The values are decimals because that's what Avalonia's number box binds to.</remarks>
internal sealed class CanvasSizeFields : ObservableObject
{
    public CanvasSizeFields(CanvasSize initialSize)
    {
        Width = initialSize.Width;
        Height = initialSize.Height;
    }

    public static decimal MinimumSide => CanvasSize.MinimumSide;

    public static decimal MaximumSide => CanvasSize.MaximumSide;

    /// <summary>The width in cells, or null while the box is empty.</summary>
    public decimal? Width
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnSizeChanged();
        }
    }

    /// <summary>The height in cells, or null while the box is empty.</summary>
    public decimal? Height
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnSizeChanged();
        }
    }

    /// <summary>The size typed in, or null when it isn't a valid canvas size.</summary>
    public CanvasSize? Size
    {
        get
        {
            if (ToSide(Width) is not { } width || ToSide(Height) is not { } height || !CanvasSize.IsValid(width, height))
                return null;

            return new CanvasSize(width, height);
        }
    }

    public bool IsValid => Size is not null;

    public bool ExceedsRecommendedSize => Size is { ExceedsRecommendedSize: true };

    public static string RecommendedSizeWarning => string.Format(CultureInfo.CurrentCulture,
        Strings.CanvasSizeWarningFormat, CanvasSize.RecommendedMaximumSide);

    private static int? ToSide(decimal? value)
    {
        if (value is not { } side || side != decimal.Truncate(side) || side > int.MaxValue || side < int.MinValue)
            return null;

        return (int)side;
    }

    private void OnSizeChanged()
    {
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(ExceedsRecommendedSize));
    }
}
