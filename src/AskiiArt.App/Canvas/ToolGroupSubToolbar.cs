// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.Core.Tools;
using Avalonia.Controls;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Shows the chosen tool's group as a sub-toolbar beside the Tools toolbar, while that group has more than one tool,
/// and takes it away when a tool of another group is chosen (ADR 0049). One lives as long as its main window does.
/// </summary>
internal sealed class ToolGroupSubToolbar : IDisposable
{
    private readonly CanvasViewModel _canvas;
    private readonly ShellDocking _docking;
    private readonly ToolbarBuilder _bars;
    private string? _shownGroupId;

    public ToolGroupSubToolbar(CanvasViewModel canvas, ShellDocking docking, ActionRegistry actions)
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

    // Choosing another tool of the same group, or replacing the document, leaves the sub-toolbar as it is.
    private void Update()
    {
        var group = _canvas.SelectedGroup;
        var wantedId = group.ToolIds.Count > 1 ? group.Id : null;
        if (wantedId == _shownGroupId)
            return;

        if (wantedId is null)
            _docking.HideSubToolbar();
        else
            _docking.ShowSubToolbar(ShellDockingIds.ToolsToolbar, () => CreateView(group));

        _shownGroupId = _docking.SubToolbarParent == ShellDockingIds.ToolsToolbar ? wantedId : null;
    }

    private Control CreateView(ToolGroup group) => _bars.Build(ToolbarLayout.CreateToolGroupSubToolbar(group));

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedGroup))
            Update();
    }

    // The window closing takes the sub-toolbar away.
    private void OnSubToolbarChanged(object? sender, EventArgs e)
    {
        if (_docking.SubToolbarParent is null)
            _shownGroupId = null;
    }
}
