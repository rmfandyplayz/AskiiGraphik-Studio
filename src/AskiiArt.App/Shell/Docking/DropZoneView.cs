// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A tinted rectangle shown during a drag: a place to drop, or where the dragged item would land. It never
/// catches the pointer. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class DropZoneView : Border
{
    private IDisposable? _background;
    private bool _isActive;

    public DropZoneView()
    {
        IsHitTestVisible = false;
        FollowBrush();
    }

    /// <summary>Whether a drop would go here, which shows more strongly.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;

            _isActive = value;
            FollowBrush();
        }
    }

    private void FollowBrush()
    {
        _background?.Dispose();
        _background = Bind(BackgroundProperty,
            this.GetResourceObservable(_isActive ? "AppDropZoneActiveBrush" : "AppDropZoneBrush"));
    }
}
