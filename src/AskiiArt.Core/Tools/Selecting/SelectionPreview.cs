// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// What a selection tool's gesture would do if it ended now, for the canvas to show while it lasts (ADR 0061): the
/// selection before it, the combine mode, the shape drawn so far and the selection it would leave. The shape is worked
/// out the first time it's asked for, and only then, so a lasso's path can grow at every pointer move while the canvas
/// fills it at most once a frame. Read it on the UI thread.
/// </summary>
public sealed class SelectionPreview
{
    private readonly Func<SelectionMask?> _makeShape;
    private bool _isShapeMade;
    private SelectionMask? _shape;
    private SelectionMask? _result;
    private SelectionMask? _lost;

    /// <summary>A preview of <paramref name="shape"/> combined with <paramref name="before"/>.</summary>
    public SelectionPreview(SelectionMask before, SelectionCombineMode mode, SelectionMask shape)
        : this(before, mode, () => shape)
    {
        ArgumentNullException.ThrowIfNull(shape);
    }

    /// <summary>
    /// A preview of the shape <paramref name="makeShape"/> makes, combined with <paramref name="before"/>. It's called
    /// once, when the shape is first needed, and returns null while the gesture has no shape yet and would end as a
    /// click does, such as a lasso path with too few points to go round anything.
    /// </summary>
    public SelectionPreview(SelectionMask before, SelectionCombineMode mode, Func<SelectionMask?> makeShape)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(makeShape);
        Before = before;
        Mode = mode;
        _makeShape = makeShape;
    }

    /// <summary>The selection when the gesture started.</summary>
    public SelectionMask Before { get; }

    /// <summary>How the shape combines with the selection.</summary>
    public SelectionCombineMode Mode { get; }

    /// <summary>The cells the shape drawn so far covers: none while there's no shape yet.</summary>
    public SelectionMask Shape => MadeShape() ?? SelectionMask.Empty(Before.CanvasSize);

    /// <summary>
    /// The selection the gesture would leave: the shape combined with <see cref="Before"/>, even when the shape has no
    /// cells, as when it lies off the canvas. While there's no shape yet, it's what a click leaves: nothing when
    /// replacing, and <see cref="Before"/> otherwise (ADR 0060, ADR 0061).
    /// </summary>
    public SelectionMask Result => _result ??= ResultOf(Before, MadeShape(), Mode);

    /// <summary>The cells selected before that the gesture would deselect, past the canvas's edges too.</summary>
    public SelectionMask Lost => _lost ??= Before.CombinedWith(Result, SelectionCombineMode.Subtract);

    // The one rule for what a gesture leaves, shared with SelectionCombiner.Finish so the preview always matches it.
    // A null shape is a click's. Selection tools work on the canvas only, so in every mode the part of the selection
    // past its edges is dropped (ADR 0071).
    internal static SelectionMask ResultOf(SelectionMask before, SelectionMask? shape, SelectionCombineMode mode)
    {
        var onCanvas = before.CutToCanvas();
        if (shape is not null)
            return onCanvas.CombinedWith(shape.CutToCanvas(), mode);

        return mode == SelectionCombineMode.Replace ? SelectionMask.Empty(before.CanvasSize) : onCanvas;
    }

    private SelectionMask? MadeShape()
    {
        if (!_isShapeMade)
        {
            _shape = _makeShape();
            _isShapeMade = true;
        }

        return _shape;
    }
}
