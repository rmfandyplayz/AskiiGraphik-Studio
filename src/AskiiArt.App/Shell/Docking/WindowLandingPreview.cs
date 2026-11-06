// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Animation;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Where a dragged window would float when that isn't where it's held, because floating windows stay inside: a very
/// light hatch inside a dashed outline, with no "+" (the owner's choice). It never catches the pointer. Placeholder: the
/// owner designs the real one.
/// </summary>
internal sealed class WindowLandingPreview : Panel
{
    /// <param name="metrics">The theme's sizes.</param>
    public WindowLandingPreview(ShellLayoutMetrics metrics)
    {
        Outline = new LandingOutline(metrics);
        IsHitTestVisible = false;
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = metrics.MotionDuration }];

        Children.Add(new WindowPlaceholderView(metrics, isLight: true));
        Children.Add(Outline);
    }

    /// <summary>The outline around it.</summary>
    public LandingOutline Outline { get; }
}
