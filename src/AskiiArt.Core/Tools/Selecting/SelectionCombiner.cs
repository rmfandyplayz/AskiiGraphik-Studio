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

    /// <summary>
    /// The selection <paramref name="shape"/> would make, for the canvas to outline during the gesture.
    /// </summary>
    public SelectionMask Preview(SelectionMask shape) => session.Selection.Mask.CombinedWith(shape, Mode);

    /// <summary>
    /// Ends the gesture with <paramref name="shape"/>, as one undo step named <paramref name="stepNameKey"/>. A shape
    /// with no cells, as after a click, deselects when replacing, and otherwise changes nothing.
    /// </summary>
    public void Finish(SelectionMask? shape, string stepNameKey)
    {
        if (shape is null || shape.IsEmpty)
        {
            if (Mode == SelectionCombineMode.Replace)
                SelectionEdits.Deselect(session);

            return;
        }

        SelectionEdits.Select(session, session.Selection.Mask.CombinedWith(shape, Mode), stepNameKey);
    }
}
