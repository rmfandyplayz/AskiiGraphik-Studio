// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Solving;

/// <summary>
/// Finds room for a drop's marker among a row of lengths, such as a stack's windows or a dock's columns, taking it from
/// the ones beside where it goes, so that only what's next to the marker moves.
/// </summary>
internal static class RoomTaking
{
    /// <summary>
    /// Takes up to <paramref name="amount"/> from <paramref name="lengths"/>, never below <paramref name="floors"/>, for
    /// room before the length at <paramref name="position"/> (or past the last). The two beside it give half each where
    /// they can spare it, then either gives more. With <paramref name="othersToo"/>, the rest come from the others,
    /// nearest first.
    /// </summary>
    /// <returns>How much it found, which is less than <paramref name="amount"/> only if nothing more could be spared.</returns>
    public static double Take(double[] lengths, IReadOnlyList<double> floors, int position, double amount, bool othersToo)
    {
        var order = NearestFirst(lengths.Length, position);
        var neighbours = order.Take(position > 0 && position < lengths.Length ? 2 : 1).ToList();
        var found = 0.0;

        // Half each from the two beside it, so the room opens around where it goes.
        foreach (var index in neighbours)
            found += Give(lengths, floors, index, Math.Min(amount / neighbours.Count, amount - found));

        foreach (var index in othersToo ? order : neighbours)
        {
            if (found >= amount)
                break;

            found += Give(lengths, floors, index, amount - found);
        }

        return found;
    }

    // Before it, then after it, then alternately further out.
    private static List<int> NearestFirst(int count, int position)
    {
        var order = new List<int>();
        for (var distance = 0; order.Count < count; distance++)
        {
            if (position - 1 - distance >= 0)
                order.Add(position - 1 - distance);

            if (position + distance < count)
                order.Add(position + distance);
        }

        return order;
    }

    private static double Give(double[] lengths, IReadOnlyList<double> floors, int index, double wanted)
    {
        var given = Math.Clamp(lengths[index] - floors[index], 0, Math.Max(0, wanted));
        lengths[index] -= given;
        return given;
    }
}
