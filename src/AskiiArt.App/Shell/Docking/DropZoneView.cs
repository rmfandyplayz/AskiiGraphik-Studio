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
    private readonly double _dimmedOpacity;
    private bool _isOutlined;
    private bool _isDimmed;

    /// <param name="fadeDuration">How long it takes to fade away, or come back.</param>
    /// <param name="dimmedOpacity">How much of it still shows while it's dimmed.</param>
    public DropZoneView(TimeSpan fadeDuration, double dimmedOpacity)
    {
        _dimmedOpacity = dimmedOpacity;
        IsHitTestVisible = false;
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = fadeDuration }];
        Bind(BackgroundProperty, this.GetResourceObservable("AppDropZoneBrush"));
    }

    /// <summary>Whether the pointer is over it. Its tint fades away, leaving just the dashed outline around it.</summary>
    public bool IsOutlined
    {
        get => _isOutlined;
        set
        {
            _isOutlined = value;
            UpdateOpacity();
        }
    }

    /// <summary>Whether the copy of a toolbar being pushed back overlaps it, which dims it without hiding it.</summary>
    public bool IsDimmed
    {
        get => _isDimmed;
        set
        {
            _isDimmed = value;
            UpdateOpacity();
        }
    }

    private void UpdateOpacity()
    {
        if (_isOutlined)
            Opacity = 0;
        else
            Opacity = _isDimmed ? _dimmedOpacity : 1;
    }
}
