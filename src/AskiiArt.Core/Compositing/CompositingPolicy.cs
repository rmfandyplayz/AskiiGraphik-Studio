// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Compositing;

/// <summary>
/// Decides which cells hide the layers below them during compositing (ARCHITECTURE §5.4, §5.6).
/// This is the one place for that rule: nothing else checks for spaces.
/// </summary>
/// <remarks>
/// Each layer chooses its own <see cref="SpaceHandling"/> (ADR 0033). A policy is read for every cell
/// of every layer, so there's one shared instance per choice.
/// </remarks>
public sealed class CompositingPolicy
{
    private const int SpaceCodePoint = ' ';

    private static readonly CompositingPolicy _spacesHide = new(SpaceHandling.HidesLayersBelow);
    private static readonly CompositingPolicy _spacesAreEmpty = new(SpaceHandling.TreatedAsEmpty);

    private CompositingPolicy(SpaceHandling spaceHandling)
    {
        SpaceHandling = spaceHandling;
    }

    /// <summary>How spaces are treated. Only U+0020 counts as a space.</summary>
    public SpaceHandling SpaceHandling { get; }

    /// <summary>The policy for a layer that treats spaces this way.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There's no such way of treating spaces.</exception>
    public static CompositingPolicy For(SpaceHandling spaceHandling) => spaceHandling switch
    {
        SpaceHandling.HidesLayersBelow => _spacesHide,
        SpaceHandling.TreatedAsEmpty => _spacesAreEmpty,
        _ => throw new ArgumentOutOfRangeException(nameof(spaceHandling), spaceHandling, null)
    };

    /// <summary>True when <paramref name="cell"/>, on a visible layer, hides whatever is below it.</summary>
    public bool HidesLayersBelow(Cell cell)
    {
        if (cell.IsEmpty)
            return false;

        return SpaceHandling == SpaceHandling.HidesLayersBelow || cell.Glyph.Value != SpaceCodePoint;
    }
}
