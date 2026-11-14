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
/// The tab strip a dock window has in place of its title, even with one tab (docs/DOCKING.md §3.3). Every window in a
/// group has one, with the same tabs, and only the one showing is visible. Too many tabs scroll, with fades like a
/// toolbar's, and the tab showing is scrolled clear of them. Placeholder: the owner designs the real one.
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
    private IncomingTabView? _incoming;
    private double? _revealFrom;
    private bool _isRevealPending;
    private bool _revealsSelected;

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

    /// <summary>
    /// Shows these tabs, with <paramref name="selected"/> showing. Only what changed is redone. When the tabs or the
    /// one showing change, the strip scrolls as little as it can to show that tab clear of the fades, starting from
    /// <paramref name="scrolledTo"/> if it's given, which is where the strip showing before was, so the tabs don't
    /// jump.
    /// </summary>
    public void SetTabs(IReadOnlyList<DockTab> tabs, string selected, double? scrolledTo = null)
    {
        if (!tabs.SequenceEqual(_views.Select(view => view.Tab)))
            Rebuild(tabs);

        if (_selected == selected)
            return;

        _selected = selected;
        UpdateSelection();
        _revealsSelected = true;
        RevealAfterLayout(scrolledTo);
    }

    /// <summary>The tab marking where a dragged window would go, while there is one.</summary>
    public Control? IncomingTab => _incoming?.View;

    /// <summary>
    /// Shows where a dragged window would go as a tab: <paramref name="marker"/>, as long as a tab with
    /// <paramref name="title"/> would be, before the tab now at <paramref name="index"/>. The strip scrolls to show it, and
    /// scrolls back when it goes.
    /// </summary>
    public void ShowIncomingTab(int index, string title, Control marker)
    {
        if (_incoming is { } shown && shown.Index == index && shown.Title == title && shown.Marker == marker)
            return;

        var scrolledFrom = _incoming?.ScrolledFrom ?? _scroller.Offset;
        HideIncomingTab();

        var sizer = new Border
        {
            Padding = TabPadding,
            Child = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center, Opacity = 0 },
        };
        var view = new Panel { Children = { sizer, marker } };
        _incoming = new IncomingTabView(index, title, marker, view, scrolledFrom);
        _tabs.Children.Insert(Math.Clamp(index, 0, _views.Count), view);

        RevealAfterLayout(scrolledTo: null);
    }

    /// <summary>Takes away the incoming tab, if there is one, and scrolls back to where the strip was before it.</summary>
    public void HideIncomingTab()
    {
        if (_incoming is not { } incoming)
            return;

        // Unless the tab showing changed meanwhile, the strip goes back to where it was, and stays there.
        if (_isRevealPending && !_revealsSelected)
            CancelReveal();

        _tabs.Children.Remove(incoming.View);
        incoming.View.Children.Remove(incoming.Marker);
        _scroller.Offset = incoming.ScrolledFrom;
        _incoming = null;
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

        if (_incoming is { } incoming)
            _tabs.Children.Insert(Math.Clamp(incoming.Index, 0, _views.Count), incoming.View);
    }

    // Once the tabs have places, since a tab that hasn't been laid out can't be scrolled to. A strip that's hidden is
    // left alone: it's given its place again when it next shows.
    private void RevealAfterLayout(double? scrolledTo)
    {
        _revealFrom = scrolledTo;
        if (_isRevealPending)
            return;

        _isRevealPending = true;
        _tabs.LayoutUpdated += Reveal;
        _tabs.InvalidateMeasure();
    }

    // The incoming tab while there is one, and otherwise the tab showing.
    private void Reveal(object? sender, EventArgs e)
    {
        var from = _revealFrom ?? _scroller.Offset.X;
        var revealsSelected = _revealsSelected;
        CancelReveal();

        Control? tab = _incoming?.View;
        if (tab is null && revealsSelected)
            tab = _views.FirstOrDefault(view => view.Tab.WindowId == _selected).View;

        if (tab is null || !IsEffectivelyVisible)
            return;

        var offset = ScrolledToShow(tab.Bounds.X, tab.Bounds.Right, from);
        _scroller.Offset = _scroller.Offset.WithX(offset);
    }

    private void CancelReveal()
    {
        _tabs.LayoutUpdated -= Reveal;
        _isRevealPending = false;
        _revealsSelected = false;
        _revealFrom = null;
    }

    // As near "from" as it can be with the tab clear of the fades. At either end of the strip there's no fade to clear.
    private double ScrolledToShow(double tabStart, double tabEnd, double from)
    {
        var viewport = _scroller.Viewport.Width;
        var clearance = Math.Min(FadeLength, viewport / 2);
        var offset = from;

        if (tabEnd + clearance > offset + viewport)
            offset = tabEnd + clearance - viewport;

        // Last, so a tab too long to clear both fades keeps its start in view.
        if (tabStart - clearance < offset)
            offset = tabStart - clearance;

        return Math.Clamp(offset, 0, Math.Max(0, _scroller.Extent.Width - viewport));
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

    private sealed record IncomingTabView(int Index, string Title, Control Marker, Panel View, Vector ScrolledFrom);
}
