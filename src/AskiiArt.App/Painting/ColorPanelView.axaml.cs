// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

namespace AskiiArt.App.Painting;

/// <summary>
/// The Color window. The picker's colour is set from code rather than bound both ways, so that showing
/// the current colour can never count as the user picking it: with nothing chosen, the picker still has to
/// show some colour, and a two-way binding would write that back as a choice.
/// </summary>
internal sealed partial class ColorPanelView : UserControl
{
    private PaintColorsViewModel? _colors;
    private bool _isShowingCurrentColor;

    public ColorPanelView()
    {
        InitializeComponent();
        Picker.ColorChanged += OnPickerColorChanged;
    }

    // Only while it's showing, so a closed window doesn't stay alive through the view model's event.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _colors = DataContext as PaintColorsViewModel;
        if (_colors is null)
            return;

        _colors.PropertyChanged += OnColorsPropertyChanged;
        ShowCurrentColor();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_colors is not null)
            _colors.PropertyChanged -= OnColorsPropertyChanged;

        _colors = null;
    }

    private void OnColorsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PaintColorsViewModel.PickerColor))
            ShowCurrentColor();
    }

    private void ShowCurrentColor()
    {
        if (_colors is null)
            return;

        _isShowingCurrentColor = true;
        try
        {
            Picker.Color = _colors.PickerColor;
        }
        finally
        {
            _isShowingCurrentColor = false;
        }
    }

    private void OnPickerColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (!_isShowingCurrentColor)
            _colors?.PickColor(e.NewColor);
    }
}
