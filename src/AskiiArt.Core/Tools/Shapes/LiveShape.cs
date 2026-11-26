// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools.Shapes;

/// <summary>
/// The shape a shape tool's drag has drawn so far (ADR 0079). The shape is real cells in the document while it's
/// dragged, and each redraw takes back the last one's undo step first, so the drag ends as one step.
/// </summary>
internal sealed class LiveShape(EditingSession session, string stepNameKey)
{
    // The step that draws the shape now, or null while none does.
    private IDocumentCommand? _drawn;

    /// <summary>
    /// Whether something else has changed the history since the shape was drawn, such as an undo during the drag. The
    /// drag then ends where it is, rather than take back a step that isn't its own.
    /// </summary>
    public bool IsInterrupted => _drawn is not null && !ReferenceEquals(session.History.NewestStep, _drawn);

    /// <summary>Takes the shape back, as if it had never been drawn.</summary>
    public void TakeBack()
    {
        if (_drawn is not null && ReferenceEquals(session.History.NewestStep, _drawn))
            session.History.CancelNewestStep();

        _drawn = null;
    }

    /// <summary>
    /// Starts collecting the shape's cells on the active layer, kept to the canvas and the selection, in
    /// <paramref name="foreground"/> and <paramref name="background"/>.
    /// </summary>
    public ShapeCells Collect(CellColor foreground, CellColor background) => new(session, foreground, background);

    /// <summary>Draws <paramref name="cells"/> as one step, unless no cell would change.</summary>
    public void Show(ShapeCells cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Build(stepNameKey) is not { } command)
            return;

        session.History.Execute(command);
        _drawn = command;
    }
}
