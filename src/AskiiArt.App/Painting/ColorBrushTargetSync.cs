// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Canvas;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;

namespace AskiiArt.App.Painting;

/// <summary>
/// Keeps the FG Brush and the BG Brush in step with the chosen colour (ADR 0049). Choosing one of them chooses its
/// colour, and choosing the other colour, such as with the Colors toolbar's swatches, while one of them is active
/// switches to the other brush. With any other tool, choosing a colour only chooses it. One lives as long as its main
/// window does.
/// </summary>
internal sealed class ColorBrushTargetSync : IDisposable
{
    private readonly CanvasViewModel _canvas;

    public ColorBrushTargetSync(CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _canvas = canvas;
        canvas.PropertyChanged += OnCanvasChanged;
        canvas.Colors.TargetChanged += OnTargetChanged;
        ChooseTheBrushsColor();
    }

    public void Dispose()
    {
        _canvas.PropertyChanged -= OnCanvasChanged;
        _canvas.Colors.TargetChanged -= OnTargetChanged;
    }

    private static PaintColorTarget? TargetOf(string toolId) => toolId switch
    {
        ColorBrushTool.ForegroundId => PaintColorTarget.Foreground,
        ColorBrushTool.BackgroundId => PaintColorTarget.Background,
        _ => null
    };

    private static string BrushFor(PaintColorTarget target) => target == PaintColorTarget.Foreground
        ? ColorBrushTool.ForegroundId
        : ColorBrushTool.BackgroundId;

    private void ChooseTheBrushsColor()
    {
        if (TargetOf(_canvas.SelectedTool.Descriptor.Id) is { } target)
            _canvas.Colors.Target = target;
    }

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedTool))
            ChooseTheBrushsColor();
    }

    private void OnTargetChanged(object? sender, EventArgs e)
    {
        if (TargetOf(_canvas.SelectedTool.Descriptor.Id) is not null)
            _canvas.SelectTool(BrushFor(_canvas.Colors.Target));
    }
}
