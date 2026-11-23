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
/// Mouse Acts as a Pen and step 4.10a's Brush Edges choice. Zooming with the mouse wheel is handled by the canvas
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
            foreach (var action in PatsActions())
                yield return action;
        }
    }

    // Step 4.10a's temporary choice of how the Brush and the Soft Eraser pick edge glyphs (ADR 0074): one item per
    // variant, ticked while it's the one in use.
    private IEnumerable<AppAction> PatsActions()
    {
        var choices = new (string Id, string LabelKey, PatsVariant Variant)[]
        {
            (CanvasActionIds.PatsUnshaped, nameof(Strings.ActionPatsUnshaped), PatsVariant.All[0]),
            (CanvasActionIds.PatsShipped, nameof(Strings.ActionPatsShipped), PatsVariant.All[1]),
            (CanvasActionIds.PatsIdea1, nameof(Strings.ActionPatsIdea1), PatsVariant.All[2]),
            (CanvasActionIds.PatsIdea2, nameof(Strings.ActionPatsIdea2), PatsVariant.All[3]),
            (CanvasActionIds.PatsIdea3, nameof(Strings.ActionPatsIdea3), PatsVariant.All[4]),
            (CanvasActionIds.PatsIdeas1And3, nameof(Strings.ActionPatsIdeas1And3), PatsVariant.All[5]),
            (CanvasActionIds.PatsIdeas1And2And3, nameof(Strings.ActionPatsIdeas1And2And3), PatsVariant.All[6]),
            (CanvasActionIds.PatsIdea3Fixed, nameof(Strings.ActionPatsIdea3Fixed), PatsVariant.All[7]),
            (CanvasActionIds.PatsIdeas1And2And3Fixed, nameof(Strings.ActionPatsIdeas1And2And3Fixed),
                PatsVariant.All[8])
        };
        var states = choices.Select(choice => new ActionCheckedState(() => canvas.Pats == choice.Variant)).ToArray();
        canvas.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(CanvasViewModel.Pats))
                return;

            foreach (var state in states)
                state.NotifyChanged();
        };

        for (var index = 0; index < choices.Length; index++)
        {
            var (id, labelKey, variant) = choices[index];
            var state = states[index];
            yield return new AppAction(id, labelKey, new RelayCommand(() => Choose(variant, state)),
                checkedState: state);
        }
    }

    private void Choose(PatsVariant variant, ActionCheckedState state)
    {
        // A menu may have unticked the item it was already ticked for; tick it again.
        if (canvas.Pats == variant)
            state.NotifyChanged();
        else
            canvas.Pats = variant;
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
