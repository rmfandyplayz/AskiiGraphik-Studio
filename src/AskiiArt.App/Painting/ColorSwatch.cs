// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Path = Avalonia.Controls.Shapes.Path;

namespace AskiiArt.App.Painting;

/// <summary>
/// A small square showing a colour, or a diagonal slash when there's no colour (None). Placeholder look.
/// </summary>
internal sealed class ColorSwatch : Border
{
    /// <summary>The colour to show, or null for None.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ColorSwatch, IBrush?>(nameof(Fill));

    private readonly Path _noneSlash = new() { Stretch = Stretch.Fill, IsHitTestVisible = false };

    public ColorSwatch()
    {
        this.Bind(WidthProperty, this.GetResourceObservable("AppColorSwatchSize"));
        this.Bind(HeightProperty, this.GetResourceObservable("AppColorSwatchSize"));
        this.Bind(BorderThicknessProperty, this.GetResourceObservable("AppColorSwatchBorderThickness"));
        this.Bind(BorderBrushProperty, this.GetResourceObservable("AppColorSwatchBorderBrush"));
        _noneSlash.Bind(Path.DataProperty, _noneSlash.GetResourceObservable("AppColorNoneSlashGeometry"));
        _noneSlash.Bind(Shape.StrokeProperty, _noneSlash.GetResourceObservable("AppColorNoneSlashBrush"));
        _noneSlash.Bind(Shape.StrokeThicknessProperty, _noneSlash.GetResourceObservable("AppColorNoneSlashThickness"));
        Child = _noneSlash;
        ShowFill();
    }

    /// <inheritdoc cref="FillProperty"/>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>True while the slash for None shows.</summary>
    public bool IsShowingNone => _noneSlash.IsVisible;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FillProperty)
            ShowFill();
    }

    private void ShowFill()
    {
        Background = Fill;
        _noneSlash.IsVisible = Fill is null;
    }
}
