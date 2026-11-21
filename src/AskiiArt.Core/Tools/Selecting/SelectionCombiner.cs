// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// Combines a selection tool's shape with the session's selection, in the mode its gesture started in (ADR 0060).
/// </summary>
internal sealed class SelectionCombiner(EditingSession session, ToolOptionValues options)
{
    /// <summary>The mode of the gesture in progress.</summary>
    public SelectionCombineMode Mode { get; private set; }

    /// <summary>Starts a gesture with <paramref name="modifiers"/> held, which may pick the mode.</summary>
    public void Start(ToolModifiers modifiers) =>
        Mode = SelectionToolOptions.ModeFor(modifiers, SelectionToolOptions.ReadMode(options));

    /// <summary>What <paramref name="shape"/> would do to the selection, for the canvas to show meanwhile.</summary>
    public SelectionPreview Preview(SelectionMask shape) => new(session.Selection.Mask, Mode, shape);

    /// <summary>
    /// What the shape <paramref name="makeShape"/> makes would do to the selection, working the shape out only if the
    /// canvas draws this preview. It makes null while there's no shape yet, as for a click.
    /// </summary>
    public SelectionPreview Preview(Func<SelectionMask?> makeShape) => new(session.Selection.Mask, Mode, makeShape);

    /// <summary>
    /// Ends the gesture with <paramref name="shape"/>, as one undo step named <paramref name="stepNameKey"/>, or named
    /// Deselect when nothing would be left selected. A shape combines even when it has no cells, as when it was drawn
    /// off the canvas. A null shape is a click's, which deselects when replacing and otherwise changes nothing.
    /// </summary>
    public void Finish(SelectionMask? shape, string stepNameKey)
    {
        var result = SelectionPreview.ResultOf(session.Selection.Mask, shape, Mode);
        if (result.IsEmpty)
            SelectionEdits.Deselect(session);
        else
            SelectionEdits.Select(session, result, stepNameKey);
    }
}
