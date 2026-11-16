// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Canvas;
using AskiiArt.Core.Tools.Options;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.ToolOptions;

/// <summary>
/// What the Tool Options toolbar shows: the chosen tool's options, built from its option group as data (ADR 0047).
/// Changing one changes the value that every tool of the group shares. One lives as long as the app does.
/// </summary>
internal sealed class ToolOptionsViewModel : ObservableObject, IDisposable
{
    private readonly CanvasViewModel _canvas;
    private ToolOptionValues? _values;

    public ToolOptionsViewModel(CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _canvas = canvas;
        canvas.PropertyChanged += OnCanvasChanged;
        Update();
    }

    /// <summary>The chosen tool's options, in the order its group lists them. Empty when it has none.</summary>
    public IReadOnlyList<ToolOptionItem> Items { get; private set; } = [];

    /// <summary>Whether the chosen tool has any options.</summary>
    public bool HasOptions => Items.Count > 0;

    public void Dispose()
    {
        _canvas.PropertyChanged -= OnCanvasChanged;
        Follow(null);
    }

    // Choosing another tool of the same group keeps the items, and so whatever the toolbar is in the middle of.
    private void Update()
    {
        var group = _canvas.SelectedTool.Options;
        var values = group is null ? null : _canvas.ToolSettings.For(group);
        if (values == _values)
            return;

        Follow(values);
        Items = values is null ? [] : [.. values.Group.Options.Select(option => ToolOptionItem.Create(option, values))];
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(HasOptions));
    }

    private void Follow(ToolOptionValues? values)
    {
        if (_values is not null)
            _values.Changed -= OnValuesChanged;

        _values = values;
        if (_values is not null)
            _values.Changed += OnValuesChanged;
    }

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedTool))
            Update();
    }

    // A value can change from elsewhere too, such as the [ and ] keys on the canvas.
    private void OnValuesChanged(object? sender, EventArgs e)
    {
        foreach (var item in Items)
            item.Refresh();
    }
}
