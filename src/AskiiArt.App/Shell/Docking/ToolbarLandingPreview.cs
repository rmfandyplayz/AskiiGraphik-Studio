// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;
using Avalonia.Animation;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A dashed outline showing where a dragged toolbar would go. Over a drop zone that's all it is, around the zone. For a
/// toolbar pushed back inside the canvas region, a real copy of the toolbar shows in it, a little see-through. It never
/// catches the pointer. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class ToolbarLandingPreview : Panel
{
    /// <param name="toolbar">The copy of the toolbar, which this owns.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public ToolbarLandingPreview(ToolbarFrame toolbar, ShellLayoutMetrics metrics)
    {
        Toolbar = toolbar;
        Outline = new LandingOutline(metrics);
        IsHitTestVisible = false;
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = metrics.MotionDuration }];

        toolbar.Bind(OpacityProperty, toolbar.GetResourceObservable("AppLandingToolbarOpacity"));
        Children.Add(toolbar);
        Children.Add(Outline);
    }

    /// <summary>The copy of the toolbar.</summary>
    public ToolbarFrame Toolbar { get; }

    /// <summary>The outline around it.</summary>
    public LandingOutline Outline { get; }

    /// <summary>Whether the copy shows in the outline, or the outline is all there is.</summary>
    public bool ShowsCopy
    {
        get => Toolbar.IsVisible;
        set => Toolbar.IsVisible = value;
    }

    /// <summary>The size the toolbar wants each way round.</summary>
    public ToolbarShapes MeasureBothWays() => Toolbar.MeasureBothWays();
}
