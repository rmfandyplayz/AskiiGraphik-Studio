// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Layers;

internal sealed partial class LayersPanelView : UserControl
{
    private LayersPanelViewModel? _viewModel;

    public LayersPanelView()
    {
        InitializeComponent();

        Rows.AddHandler(DoubleTappedEvent, OnRowDoubleTapped);
        Rows.AddHandler(KeyDownEvent, OnKeyDownWhileRenaming, RoutingStrategies.Tunnel);
        Rows.AddHandler(LostFocusEvent, OnNameBoxLostFocus);
    }

    /// <summary>The buttons below the list.</summary>
    public Control? Buttons
    {
        get => ButtonsHost.Content as Control;
        set => ButtonsHost.Content = value;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _viewModel = DataContext as LayersPanelViewModel;
        if (_viewModel is not null)
            _viewModel.RenameRequested += OnRenameRequested;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_viewModel is not null)
            _viewModel.RenameRequested -= OnRenameRequested;

        _viewModel = null;
    }

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Visual { DataContext: LayerRow row } source)
            return;

        // Quick clicks on a checkbox are just clicks, and a name box handles its own.
        if (source.FindAncestorOfType<CheckBox>(includeSelf: true) is null &&
            source.FindAncestorOfType<TextBox>(includeSelf: true) is null)
        {
            OnRenameRequested(this, row);
        }
    }

    private void OnRenameRequested(object? sender, LayerRow row)
    {
        row.BeginRename();

        // The name box can only take focus once the layout has shown it.
        Dispatcher.UIThread.Post(() =>
        {
            if (Rows.ContainerFromItem(row)?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() is { } box)
            {
                box.Focus();
                box.SelectAll();
            }
        }, DispatcherPriority.Loaded);
    }

    private void OnKeyDownWhileRenaming(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Escape) || e.Source is not TextBox { DataContext: LayerRow row })
            return;

        _viewModel?.FinishRename(row, keepNewName: e.Key == Key.Enter);
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(PanelEvents.DoneTypingEvent));
    }

    // Clicking away keeps the new name, as most apps do.
    private void OnNameBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox { DataContext: LayerRow row })
            _viewModel?.FinishRename(row, keepNewName: true);
    }
}
