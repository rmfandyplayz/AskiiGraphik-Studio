// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Compositing;

/// <summary>
/// Decides which cells hide the layers below them during compositing (ARCHITECTURE §5.4, §5.6).
/// This is the one place for that rule: nothing else checks for spaces.
/// </summary>
/// <remarks>
/// The space option will be set per layer (ARCHITECTURE §23, step 2.4). Until then callers pass
/// <see cref="Default"/>.
/// </remarks>
public sealed class CompositingPolicy
{
    private const int SpaceCodePoint = ' ';

    /// <summary>Creates a policy.</summary>
    public CompositingPolicy(SpaceHandling spaceHandling)
    {
        if (!Enum.IsDefined(spaceHandling))
            throw new ArgumentOutOfRangeException(nameof(spaceHandling), spaceHandling, null);

        SpaceHandling = spaceHandling;
    }

    /// <summary>The default policy: spaces hide the layers below.</summary>
    public static CompositingPolicy Default { get; } = new(SpaceHandling.HidesLayersBelow);

    /// <summary>How spaces are treated. Only U+0020 counts as a space.</summary>
    public SpaceHandling SpaceHandling { get; }

    /// <summary>True when <paramref name="cell"/>, on a visible layer, hides whatever is below it.</summary>
    public bool HidesLayersBelow(Cell cell)
    {
        if (cell.IsEmpty)
            return false;

        return SpaceHandling == SpaceHandling.HidesLayersBelow || cell.Glyph.Value != SpaceCodePoint;
    }
}
