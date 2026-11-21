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
    private readonly Func<SelectionMask> _makeShape;
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
    /// once, when the shape is first needed.
    /// </summary>
    public SelectionPreview(SelectionMask before, SelectionCombineMode mode, Func<SelectionMask> makeShape)
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

    /// <summary>The cells the shape drawn so far covers.</summary>
    public SelectionMask Shape => _shape ??= _makeShape();

    /// <summary>
    /// The selection the gesture would leave: the shape combined with <see cref="Before"/>, or, for a shape with no
    /// cells, as after a click, nothing when replacing and <see cref="Before"/> otherwise (ADR 0060).
    /// </summary>
    public SelectionMask Result => _result ??= ResultOf(Before, Shape, Mode);

    /// <summary>The cells selected before that the gesture would deselect.</summary>
    public SelectionMask Lost => _lost ??= Before.CombinedWith(Result, SelectionCombineMode.Subtract);

    // The one rule for what a gesture leaves, shared with SelectionCombiner.Finish so the preview always matches it.
    internal static SelectionMask ResultOf(SelectionMask before, SelectionMask shape, SelectionCombineMode mode)
    {
        if (!shape.IsEmpty)
            return before.CombinedWith(shape, mode);

        return mode == SelectionCombineMode.Replace ? SelectionMask.Empty(before.CanvasSize) : before;
    }
}
