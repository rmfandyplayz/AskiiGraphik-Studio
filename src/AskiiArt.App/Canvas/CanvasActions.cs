// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's view actions: Zoom In, Zoom Out, Actual Size and Show Grid. Zooming with the
/// mouse wheel is handled by the canvas control itself, because it zooms around the pointer.
/// </summary>
internal sealed class CanvasActions(CanvasViewModel canvas) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var zoomIn = new RelayCommand(canvas.ZoomIn, () => canvas.CanZoomIn);
        var zoomOut = new RelayCommand(canvas.ZoomOut, () => canvas.CanZoomOut);
        var gridState = new ActionCheckedState(() => canvas.IsGridVisible);

        canvas.PropertyChanged += (_, e) => OnCanvasPropertyChanged(e, zoomIn, zoomOut, gridState);

        // The plus key is "=" without Shift on many keyboards, so both work, as does the keypad.
        yield return new AppAction(CanvasActionIds.ZoomIn, nameof(Strings.ActionZoomIn), zoomIn,
        [
            new ActionShortcut(Key.OemPlus, ShortcutModifiers.Command),
            new ActionShortcut(Key.OemPlus, ShortcutModifiers.Command | ShortcutModifiers.Shift),
            new ActionShortcut(Key.Add, ShortcutModifiers.Command)
        ]);

        yield return new AppAction(CanvasActionIds.ZoomOut, nameof(Strings.ActionZoomOut), zoomOut,
        [
            new ActionShortcut(Key.OemMinus, ShortcutModifiers.Command),
            new ActionShortcut(Key.Subtract, ShortcutModifiers.Command)
        ]);

        yield return new AppAction(CanvasActionIds.ActualSize, nameof(Strings.ActionActualSize),
            new RelayCommand(canvas.ZoomToActualSize),
        [
            new ActionShortcut(Key.D0, ShortcutModifiers.Command),
            new ActionShortcut(Key.NumPad0, ShortcutModifiers.Command)
        ]);

        yield return new AppAction(CanvasActionIds.ShowGrid, nameof(Strings.ActionShowGrid),
            new RelayCommand(canvas.ToggleGrid),
            [new ActionShortcut(Key.OemQuotes, ShortcutModifiers.Command)],
            checkedState: gridState);
    }

    private static void OnCanvasPropertyChanged(PropertyChangedEventArgs e, RelayCommand zoomIn, RelayCommand zoomOut,
        ActionCheckedState gridState)
    {
        switch (e.PropertyName)
        {
            case nameof(CanvasViewModel.ZoomFactor):
                zoomIn.NotifyCanExecuteChanged();
                zoomOut.NotifyCanExecuteChanged();
                break;
            case nameof(CanvasViewModel.IsGridVisible):
                gridState.NotifyChanged();
                break;
        }
    }
}
