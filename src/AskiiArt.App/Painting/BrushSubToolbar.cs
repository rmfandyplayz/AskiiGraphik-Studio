// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.Core.Tools.Brush;
using Avalonia.Controls;

namespace AskiiArt.App.Painting;

/// <summary>
/// Shows the Brush's sub-toolbar beside the Tools toolbar while the Brush is the active tool, and takes it away when
/// another tool is chosen (ADR 0043). One lives as long as its main window does.
/// </summary>
internal sealed class BrushSubToolbar : IDisposable
{
    private readonly CanvasViewModel _canvas;
    private readonly ShellDocking _docking;
    private readonly ToolbarBuilder _bars;
    private bool _isShowing;

    public BrushSubToolbar(CanvasViewModel canvas, ShellDocking docking, ActionRegistry actions)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(docking);
        ArgumentNullException.ThrowIfNull(actions);

        _canvas = canvas;
        _docking = docking;
        _bars = new ToolbarBuilder(actions);
        canvas.PropertyChanged += OnCanvasChanged;
        docking.SubToolbarChanged += OnSubToolbarChanged;
        Update();
    }

    public void Dispose()
    {
        _canvas.PropertyChanged -= OnCanvasChanged;
        _docking.SubToolbarChanged -= OnSubToolbarChanged;
    }

    // Replacing the document makes a new tool of the same kind, which leaves the sub-toolbar as it is.
    private void Update()
    {
        var isWanted = _canvas.ActiveTool.Descriptor.Id == BrushTool.Id;
        if (isWanted == _isShowing)
            return;

        if (isWanted)
            _docking.ShowSubToolbar(ShellDockingIds.ToolsToolbar, CreateView);
        else
            _docking.HideSubToolbar();

        _isShowing = isWanted && _docking.SubToolbarParent == ShellDockingIds.ToolsToolbar;
    }

    private Control CreateView() => _bars.Build(ToolbarLayout.CreateBrushSubToolbar());

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.ActiveTool))
            Update();
    }

    // Something else can take the sub-toolbar's place, such as the debug sample, or the window closing.
    private void OnSubToolbarChanged(object? sender, EventArgs e)
    {
        if (_docking.SubToolbarParent is null)
            _isShowing = false;
    }
}
