// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Selection;

/// <summary>
/// Walks the runs of selected cells in one row of a <see cref="SelectionMask"/>
/// (<see cref="SelectionMask.RunsInRow"/>), left to right, with <c>foreach</c> and without allocating, so the canvas
/// can go through a big selection every frame.
/// </summary>
public ref struct SelectionRowRuns
{
    private readonly int _left;
    private readonly bool _hasBits;
    private BitRow.RunEnumerator _bitRuns;

    // A row of a rectangle is a single run, given once.
    private int _wholeRunLength;

    internal SelectionRowRuns(int left, int wholeRunLength)
    {
        _left = left;
        _wholeRunLength = wholeRunLength;
    }

    internal SelectionRowRuns(int left, BitRow.RunEnumerator bitRuns)
    {
        _left = left;
        _hasBits = true;
        _bitRuns = bitRuns;
    }

    /// <summary>The current run: the column of its first cell, and how many cells it has.</summary>
    public (int Column, int Length) Current { get; private set; }

    /// <summary>Lets <c>foreach</c> walk the runs.</summary>
    public readonly SelectionRowRuns GetEnumerator() => this;

    /// <summary>Moves to the next run, or returns false when there are no more.</summary>
    public bool MoveNext()
    {
        if (_hasBits)
        {
            if (!_bitRuns.MoveNext())
                return false;

            var (start, length) = _bitRuns.Current;
            Current = (_left + start, length);
            return true;
        }

        if (_wholeRunLength <= 0)
            return false;

        Current = (_left, _wholeRunLength);
        _wholeRunLength = 0;
        return true;
    }
}
