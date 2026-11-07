// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A tab in a <see cref="DockTabStrip"/>.</summary>
/// <param name="WindowId">The window it shows.</param>
/// <param name="Title">The window's name.</param>
internal readonly record struct DockTab(string WindowId, string Title);

/// <summary>A tab was right-clicked, for its window's menu.</summary>
internal sealed class DockTabMenuRequestedEventArgs(string windowId, Control tab) : EventArgs
{
    public string WindowId { get; } = windowId;

    /// <summary>The tab, which the menu opens at.</summary>
    public Control Tab { get; } = tab;
}

/// <summary>
/// The tab strip a dock window has in place of its title, even with one tab (docs/DOCKING.md §3.3). Every window in a group
/// has one, with the same tabs, and only the one showing is visible. Too many tabs scroll, with fades like a toolbar's.
/// Placeholder: the owner designs the real one.
/// </summary>
internal sealed class DockTabStrip : UserControl
{
    public static readonly StyledProperty<IBrush?> SelectedTabBrushProperty =
        AvaloniaProperty.Register<DockTabStrip, IBrush?>(nameof(SelectedTabBrush));

    public static readonly StyledProperty<Thickness> TabPaddingProperty =
        AvaloniaProperty.Register<DockTabStrip, Thickness>(nameof(TabPadding));

    private readonly StackPanel _tabs = new() { Orientation = Orientation.Horizontal };
    private readonly ScrollViewer _scroller;
    private readonly List<(DockTab Tab, Border View)> _views = [];
    private ToolbarFadeAmounts _fades;
    private string? _selected;

    public DockTabStrip()
    {
        _scroller = new ScrollViewer
        {
            Content = _tabs,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Content = _scroller;

        Bind(SelectedTabBrushProperty, this.GetResourceObservable("AppDockSelectedTabBrush"));
        Bind(TabPaddingProperty, this.GetResourceObservable("AppDockTabPadding"));

        _scroller.PointerWheelChanged += OnWheel;
        _scroller.ScrollChanged += (_, _) => UpdateFades();
        _scroller.SizeChanged += (_, _) => UpdateFades();

        // Tunnelling, so the tab is known before the title row takes the press for dragging.
        AddHandler(PointerPressedEvent, (_, e) => PressedWindowId = TabAt(e.GetPosition(this))?.Tab.WindowId,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(ContextRequestedEvent, OnContextRequested);
    }

    /// <summary>Raised when a tab is right-clicked. Right-clicking the empty part of the strip opens the frame's own menu.</summary>
    public event EventHandler<DockTabMenuRequestedEventArgs>? TabMenuRequested;

    /// <summary>The background of the tab that's showing.</summary>
    public IBrush? SelectedTabBrush
    {
        get => GetValue(SelectedTabBrushProperty);
        set => SetValue(SelectedTabBrushProperty, value);
    }

    /// <summary>The space around each tab's title.</summary>
    public Thickness TabPadding
    {
        get => GetValue(TabPaddingProperty);
        set => SetValue(TabPaddingProperty, value);
    }

    /// <summary>How far one wheel notch scrolls.</summary>
    public double WheelStep { get; set; }

    /// <summary>How long the fade is on a side with more to scroll to.</summary>
    public double FadeLength { get; set; }

    /// <summary>The tab pressed last, or null if the press was on the strip's empty part.</summary>
    public string? PressedWindowId { get; private set; }

    /// <summary>How far the tabs are scrolled.</summary>
    public double ScrollOffset => _scroller.Offset.X;

    /// <summary>The tabs' windows, in order.</summary>
    public IReadOnlyList<string> WindowIds => [.. _views.Select(view => view.Tab.WindowId)];

    /// <summary>How long each tab is, by window id.</summary>
    public IReadOnlyDictionary<string, double> TabLengths =>
        _views.ToDictionary(view => view.Tab.WindowId, view => view.View.Bounds.Width, StringComparer.Ordinal);

    /// <summary>Shows these tabs, with <paramref name="selected"/> showing. Only what changed is redone.</summary>
    public void SetTabs(IReadOnlyList<DockTab> tabs, string selected)
    {
        if (!tabs.SequenceEqual(_views.Select(view => view.Tab)))
            Rebuild(tabs);

        if (_selected == selected)
            return;

        _selected = selected;
        UpdateSelection();
        if (_views.FirstOrDefault(view => view.Tab.WindowId == selected).View is { } shown)
            shown.BringIntoView();
    }

    /// <summary>The tab showing a window, if there is one.</summary>
    public Control? TabFor(string windowId) => _views.FirstOrDefault(view => view.Tab.WindowId == windowId).View;

    /// <summary>The tab at a point in the strip's own coordinates, if there is one.</summary>
    public (DockTab Tab, Border View)? TabAt(Point point)
    {
        foreach (var view in _views)
        {
            if (this.TranslatePoint(point, view.View) is { } inside && new Rect(view.View.Bounds.Size).Contains(inside))
                return view;
        }

        return null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedTabBrushProperty)
            UpdateSelection();
        else if (change.Property == TabPaddingProperty)
            _views.ForEach(view => view.View.Padding = TabPadding);
    }

    private void Rebuild(IReadOnlyList<DockTab> tabs)
    {
        _views.Clear();
        _tabs.Children.Clear();

        foreach (var tab in tabs)
        {
            var view = new Border
            {
                Padding = TabPadding,
                Child = new TextBlock { Text = tab.Title, VerticalAlignment = VerticalAlignment.Center },
            };

            _views.Add((tab, view));
            _tabs.Children.Add(view);
        }

        _selected = null;
    }

    private void UpdateSelection()
    {
        foreach (var (tab, view) in _views)
            view.Background = tab.WindowId == _selected ? SelectedTabBrush : null;
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (!e.TryGetPosition(this, out var point) || TabAt(point) is not { } tab)
            return;

        TabMenuRequested?.Invoke(this, new DockTabMenuRequestedEventArgs(tab.Tab.WindowId, tab.View));
        e.Handled = true;
    }

    private void UpdateFades()
    {
        var fades = ToolbarFades.For(_scroller.Offset.X, _scroller.Extent.Width, _scroller.Viewport.Width, FadeLength);
        if (fades == _fades)
            return;

        _fades = fades;
        _scroller.OpacityMask = ToolbarFades.ToMask(fades, isHorizontal: true);
    }

    // A mouse's wheel only turns one way, so it has to scroll sideways here. The scroller handles a sideways wheel or
    // trackpad itself, so this only sees the rest.
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        var offset = _scroller.Offset;
        _scroller.Offset = offset.WithX(offset.X - e.Delta.Y * WheelStep);
        e.Handled = true;
    }
}
