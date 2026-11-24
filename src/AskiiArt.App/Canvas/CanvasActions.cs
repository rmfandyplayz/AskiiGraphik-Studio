// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas.FrameRate;
using AskiiArt.App.Resources;
using AskiiArt.Core.Tools.RampStrokes;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's view actions: Zoom In, Zoom Out, Actual Size and Show Grid, and in Debug builds Show Frame Rate,
/// Mouse Acts as a Pen and Brush Edges. Zooming with the mouse wheel is handled by the canvas
/// control itself, because it zooms around the pointer.
/// </summary>
internal sealed class CanvasActions(CanvasViewModel canvas, bool includeDebugActions) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var zoomIn = new RelayCommand(canvas.ZoomIn, () => canvas.CanZoomIn);
        var zoomOut = new RelayCommand(canvas.ZoomOut, () => canvas.CanZoomOut);
        var gridState = new ActionCheckedState(() => canvas.IsGridVisible);
        var penState = new ActionCheckedState(() => canvas.IsMouseActingAsPen);

        canvas.PropertyChanged += (_, e) => OnCanvasPropertyChanged(e, zoomIn, zoomOut, gridState, penState);

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

        if (includeDebugActions)
        {
            var frameRateState = new ActionCheckedState(() => canvas.FrameRate.IsVisible);
            canvas.FrameRate.PropertyChanged += (_, e) => OnFrameRatePropertyChanged(e, frameRateState);
            yield return new AppAction(CanvasActionIds.ShowFrameRate, nameof(Strings.ActionShowFrameRate),
                new RelayCommand(canvas.FrameRate.Toggle), checkedState: frameRateState);
            yield return new AppAction(CanvasActionIds.MouseActsAsPen, nameof(Strings.ActionMouseActsAsPen),
                new RelayCommand(canvas.ToggleMouseActingAsPen), checkedState: penState);
            foreach (var action in BrushEdgesActions())
                yield return action;
        }
    }

    // How strictly the Brush and the Soft Eraser keep edge glyphs' ink on the painted side (ADR 0075): one item for
    // each, ticked while it's the one in use.
    private IEnumerable<AppAction> BrushEdgesActions()
    {
        var choices = new (string Id, string LabelKey, EdgeStrictness Strictness)[]
        {
            (CanvasActionIds.BrushEdgesStandard, nameof(Strings.ActionBrushEdgesStandard), EdgeStrictness.Standard),
            (CanvasActionIds.BrushEdgesStrict, nameof(Strings.ActionBrushEdgesStrict), EdgeStrictness.Strict)
        };
        var states = choices
            .Select(choice => new ActionCheckedState(() => canvas.EdgeStrictness == choice.Strictness))
            .ToArray();
        canvas.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(CanvasViewModel.EdgeStrictness))
                return;

            foreach (var state in states)
                state.NotifyChanged();
        };

        for (var index = 0; index < choices.Length; index++)
        {
            var (id, labelKey, strictness) = choices[index];
            var state = states[index];
            yield return new AppAction(id, labelKey, new RelayCommand(() => Choose(strictness, state)),
                checkedState: state);
        }
    }

    private void Choose(EdgeStrictness strictness, ActionCheckedState state)
    {
        // A menu may have unticked the item it was already ticked for; tick it again.
        if (canvas.EdgeStrictness == strictness)
            state.NotifyChanged();
        else
            canvas.EdgeStrictness = strictness;
    }

    private static void OnFrameRatePropertyChanged(PropertyChangedEventArgs e, ActionCheckedState frameRateState)
    {
        if (e.PropertyName == nameof(FrameRateReadout.IsVisible))
            frameRateState.NotifyChanged();
    }

    private static void OnCanvasPropertyChanged(PropertyChangedEventArgs e, RelayCommand zoomIn, RelayCommand zoomOut,
        ActionCheckedState gridState, ActionCheckedState penState)
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
            case nameof(CanvasViewModel.IsMouseActingAsPen):
                penState.NotifyChanged();
                break;
        }
    }
}
