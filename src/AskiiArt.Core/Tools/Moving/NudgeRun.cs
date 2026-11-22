// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// Which of the Move tool's arrow presses make one step together (ADR 0070). Presses in quick succession, or while an
/// arrow is held down, carry a run on, also when they turn, so pressing up and right in turn to go diagonally is one
/// step. A turn after three or more presses one way starts a new run, as does a press 0.4 s or more after the last
/// arrow was let go. Two presses one way count as a slip, not a change of mind.
/// </summary>
internal sealed class NudgeRun(TimeProvider clock)
{
    // Measured from when the last arrow was let go, so a key held down, which the keyboard repeats only after a delay,
    // stays one run.
    private static readonly TimeSpan _pause = TimeSpan.FromSeconds(0.4);

    // How many presses one way make a turn start a new run.
    private const int PressesBeforeATurnCounts = 3;

    private readonly HashSet<ToolKey> _heldArrows = [];
    private DateTimeOffset _lastLetGo = DateTimeOffset.MinValue;
    private bool _isUnderWay;
    private CellPoint _direction;
    private int _pressesThatWay;

    /// <summary>
    /// Notes a press of <paramref name="arrow"/>, which goes <paramref name="direction"/>, and says whether it carries
    /// on the run under way rather than starting a new one.
    /// </summary>
    public bool Continues(ToolKey arrow, CellPoint direction)
    {
        var isSoon = _heldArrows.Count > 0 || clock.GetUtcNow() - _lastLetGo < _pause;
        var turns = direction != _direction;
        var continues = _isUnderWay && isSoon && !(turns && _pressesThatWay >= PressesBeforeATurnCounts);

        _heldArrows.Add(arrow);
        _pressesThatWay = continues && !turns ? _pressesThatWay + 1 : 1;
        _direction = direction;
        _isUnderWay = true;
        return continues;
    }

    /// <summary>Notes that <paramref name="arrow"/> was let go.</summary>
    public void LetGo(ToolKey arrow)
    {
        if (_heldArrows.Remove(arrow))
            _lastLetGo = clock.GetUtcNow();
    }

    /// <summary>Ends the run under way, as a drag or anything else that ends the step does.</summary>
    public void Stop() => _isUnderWay = false;
}
