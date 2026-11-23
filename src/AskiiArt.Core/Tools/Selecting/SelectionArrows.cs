// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Moving;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The arrows in the selection tools (ADR 0073). With something selected and no gesture under way, they move the
/// selected cells just as the Move tool's arrows do, the cells floating and each run of presses an undo step, though
/// the tool stays as it is. With nothing selected they do nothing, and during a gesture they're swallowed, as a move
/// would fight it. Enter ends a move they started and deselects; a gesture starting, another tool, another layer or
/// any other change ends it where its cells are.
/// </summary>
internal sealed class SelectionArrows
{
    private readonly EditingSession _session;
    private readonly CellMover _mover;

    public SelectionArrows(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _mover = new CellMover(context.Session, context.CellWidthToHeight);
    }

    /// <summary>
    /// Handles the arrows and Enter, for a tool that has already handled its own keys.
    /// </summary>
    /// <returns>True when the key was used, or was swallowed during a gesture.</returns>
    public bool KeyPressed(ToolKey key, bool isGestureUnderWay)
    {
        if (!CellMover.IsArrow(key))
            return key == ToolKey.Enter && !isGestureUnderWay && _mover.Move is not null && _mover.Finish();

        if (isGestureUnderWay)
            return true;

        return !_session.Selection.IsEmpty && _mover.Nudge(key);
    }

    /// <summary>Notes when an arrow is let go, which decides whether the next press starts a new step.</summary>
    public void ArrowReleased(ToolKey arrow) => _mover.LetGo(arrow);

    /// <summary>Ends a move under way where its cells are, as a gesture starting does.</summary>
    public void EndMove() => _mover.End();

    /// <summary>Ends a move under way and stops following the session, for a tool being put away.</summary>
    public void Detach() => _mover.Detach();
}
