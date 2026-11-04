// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The small 6-dot handle at each end of a toolbar. It catches the pointer over its whole area, even while its
/// dots are invisible, and only draws them as faint as the theme says.
/// </summary>
internal sealed class ToolbarGrip : Control
{
    private const int DotsAcross = 3;
    private const int DotsAlong = 2;

    public static readonly StyledProperty<IBrush?> DotBrushProperty =
        AvaloniaProperty.Register<ToolbarGrip, IBrush?>(nameof(DotBrush));

    public static readonly StyledProperty<double> DotOpacityProperty =
        AvaloniaProperty.Register<ToolbarGrip, double>(nameof(DotOpacity));

    public static readonly StyledProperty<double> DotSizeProperty =
        AvaloniaProperty.Register<ToolbarGrip, double>(nameof(DotSize));

    public static readonly StyledProperty<double> DotGapProperty =
        AvaloniaProperty.Register<ToolbarGrip, double>(nameof(DotGap));

    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<ToolbarGrip, double>(nameof(Length));

    public static readonly StyledProperty<ToolbarOrientation> ToolbarDirectionProperty =
        AvaloniaProperty.Register<ToolbarGrip, ToolbarOrientation>(nameof(ToolbarDirection));

    static ToolbarGrip()
    {
        AffectsRender<ToolbarGrip>(DotBrushProperty, DotOpacityProperty, DotSizeProperty, DotGapProperty, ToolbarDirectionProperty);
        AffectsMeasure<ToolbarGrip>(DotSizeProperty, DotGapProperty, LengthProperty, ToolbarDirectionProperty);
    }

    public IBrush? DotBrush
    {
        get => GetValue(DotBrushProperty);
        set => SetValue(DotBrushProperty, value);
    }

    /// <summary>How visible the dots are: invisible until the pointer is over the toolbar.</summary>
    public double DotOpacity
    {
        get => GetValue(DotOpacityProperty);
        set => SetValue(DotOpacityProperty, value);
    }

    public double DotSize
    {
        get => GetValue(DotSizeProperty);
        set => SetValue(DotSizeProperty, value);
    }

    public double DotGap
    {
        get => GetValue(DotGapProperty);
        set => SetValue(DotGapProperty, value);
    }

    /// <summary>How much of the toolbar's length it takes.</summary>
    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>Which way its toolbar runs. The dots lie across it.</summary>
    public ToolbarOrientation ToolbarDirection
    {
        get => GetValue(ToolbarDirectionProperty);
        set => SetValue(ToolbarDirectionProperty, value);
    }

    private bool IsOnHorizontalToolbar => ToolbarDirection == ToolbarOrientation.Horizontal;

    protected override Size MeasureOverride(Size availableSize)
    {
        var across = ExtentOf(DotsAcross);
        return IsOnHorizontalToolbar ? new Size(Length, across) : new Size(across, Length);
    }

    public override void Render(DrawingContext context)
    {
        // Filled rather than left empty, because an empty area doesn't catch the pointer.
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        if (DotBrush is not { } brush || DotOpacity <= 0)
            return;

        var columns = IsOnHorizontalToolbar ? DotsAlong : DotsAcross;
        var rows = IsOnHorizontalToolbar ? DotsAcross : DotsAlong;
        var left = (Bounds.Width - ExtentOf(columns)) / 2;
        var top = (Bounds.Height - ExtentOf(rows)) / 2;
        var step = DotSize + DotGap;
        var radius = DotSize / 2;

        using (context.PushOpacity(DotOpacity))
        {
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var center = new Point(left + column * step + radius, top + row * step + radius);
                    context.DrawEllipse(brush, null, center, radius, radius);
                }
            }
        }
    }

    private double ExtentOf(int dots) => dots * DotSize + (dots - 1) * DotGap;
}
