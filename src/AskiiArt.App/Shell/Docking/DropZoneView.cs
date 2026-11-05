// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Animation;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A tinted rectangle shown during a drag: a place to drop. It never catches the pointer. Placeholder: the owner
/// designs the real one.
/// </summary>
internal sealed class DropZoneView : Border
{
    private bool _isCovered;

    /// <param name="fadeDuration">How long it takes to fade away, or come back.</param>
    public DropZoneView(TimeSpan fadeDuration)
    {
        IsHitTestVisible = false;
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = fadeDuration }];
        Bind(BackgroundProperty, this.GetResourceObservable("AppDropZoneBrush"));
    }

    /// <summary>Whether the preview of where a drop would land overlaps it, which makes it fade away.</summary>
    public bool IsCovered
    {
        get => _isCovered;
        set
        {
            if (_isCovered == value)
                return;

            _isCovered = value;
            Opacity = value ? 0 : 1;
        }
    }
}
