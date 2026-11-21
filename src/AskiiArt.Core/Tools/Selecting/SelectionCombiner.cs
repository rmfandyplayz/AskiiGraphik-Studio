// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// Combines a selection tool's shape with the session's selection, in the mode its gesture started in (ADR 0060).
/// While the gesture shows a preview, an undo or redo that changes the selection hands the tool a new one, so the
/// canvas shows what letting go would now leave without waiting for the pointer to move (ADR 0061).
/// </summary>
/// <param name="session">The session whose selection the shape combines with.</param>
/// <param name="options">The selection tools' options, which hold the mode the Mode buttons chose.</param>
/// <param name="previewChanged">Takes the new preview when the selection changes during a gesture.</param>
internal sealed class SelectionCombiner(EditingSession session, ToolOptionValues options,
    Action<SelectionPreview> previewChanged)
{
    // The shape the gesture's last preview showed, or null while it shows none.
    private Func<SelectionMask?>? _makeShape;
    private bool _isWatching;

    /// <summary>The mode of the gesture in progress.</summary>
    public SelectionCombineMode Mode { get; private set; }

    /// <summary>Starts a gesture with <paramref name="modifiers"/> held, which may pick the mode.</summary>
    public void Start(ToolModifiers modifiers)
    {
        Mode = SelectionToolOptions.ModeFor(modifiers, SelectionToolOptions.ReadMode(options));
        _makeShape = null;
        if (_isWatching)
            return;

        session.Selection.Changed += OnSelectionChanged;
        _isWatching = true;
    }

    /// <summary>
    /// What the shape <paramref name="makeShape"/> makes would do to the selection, working the shape out only if the
    /// canvas draws this preview. It makes null while there's no shape yet, as for a click. It may be called again
    /// for the same preview on a changed selection, so it reads the canvas's size when it's called.
    /// </summary>
    public SelectionPreview Preview(Func<SelectionMask?> makeShape)
    {
        ArgumentNullException.ThrowIfNull(makeShape);
        _makeShape = makeShape;
        return new SelectionPreview(session.Selection.Mask, Mode, makeShape);
    }

    /// <summary>Ends the gesture without changing the selection, as when it's cancelled.</summary>
    public void Stop()
    {
        _makeShape = null;
        if (!_isWatching)
            return;

        session.Selection.Changed -= OnSelectionChanged;
        _isWatching = false;
    }

    /// <summary>
    /// Ends the gesture with <paramref name="shape"/>, as one undo step named <paramref name="stepNameKey"/>, or named
    /// Deselect when nothing would be left selected. A shape combines even when it has no cells, as when it was drawn
    /// off the canvas. A null shape is a click's, which deselects when replacing and otherwise changes nothing.
    /// </summary>
    public void Finish(SelectionMask? shape, string stepNameKey)
    {
        Stop();
        var result = SelectionPreview.ResultOf(session.Selection.Mask, shape, Mode);
        if (result.IsEmpty)
            SelectionEdits.Deselect(session);
        else
            SelectionEdits.Select(session, result, stepNameKey);
    }

    private void OnSelectionChanged(object? sender, SessionSelectionChangedEventArgs e)
    {
        if (_makeShape is { } makeShape)
            previewChanged(new SelectionPreview(e.NewMask, Mode, makeShape));
    }
}
