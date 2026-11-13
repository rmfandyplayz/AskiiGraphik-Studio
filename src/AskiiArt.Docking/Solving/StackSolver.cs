// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Solving;

/// <summary>
/// Shares a column's length among its groups. The groups always fill it. Each is either title-only or at
/// least its minimum, never in between. When there isn't room, groups go title-only from the end of the stack,
/// without changing what the user asked for, so growing the window brings them back.
/// </summary>
internal static class StackSolver
{
    public readonly record struct Entry(double Share, bool IsTitleOnly, double MinimumLength);

    public readonly record struct Slot(double Start, double Length, bool IsTitleOnly);

    public static IReadOnlyList<Slot> Solve(IReadOnlyList<Entry> entries, double length, double gap, double titleLength)
    {
        if (entries.Count == 0)
            return [];

        var isTitleOnly = entries.Select(entry => entry.IsTitleOnly).ToArray();
        if (Array.TrueForAll(isTitleOnly, value => value))
            isTitleOnly[^1] = false;

        var usable = Math.Max(0, length - gap * (entries.Count - 1));
        SqueezeFromTheEnd(entries, isTitleOnly, usable, titleLength);

        var room = Math.Max(0, usable - titleLength * isTitleOnly.Count(value => value));
        var lengths = ShareOut(entries, isTitleOnly, room);

        var slots = new Slot[entries.Count];
        var start = 0.0;
        for (var index = 0; index < entries.Count; index++)
        {
            var wanted = isTitleOnly[index] ? titleLength : lengths[index];

            // Only a window too small for every title can get here, and nothing may leave the column.
            var fitted = Math.Max(0, Math.Min(wanted, length - start));
            slots[index] = new Slot(start, fitted, isTitleOnly[index]);
            start = Math.Min(length, start + fitted + gap);
        }

        return slots;
    }

    /// <summary>
    /// Like <see cref="Solve"/>, with room of <paramref name="thickness"/> before the group at <paramref name="position"/>,
    /// or past the last. It comes from the groups beside it, then the others, never below their minimums (RoomTaking), so
    /// nothing changes title-only for it. With none to spare, the room is smaller.
    /// </summary>
    /// <returns>Where each group goes, and the room.</returns>
    public static (IReadOnlyList<Slot> Slots, Slot Room) SolveWithRoom(IReadOnlyList<Entry> entries, double length,
        double gap, double titleLength, int position, double thickness)
    {
        var slots = Solve(entries, length, gap, titleLength);
        var at = Math.Clamp(position, 0, slots.Count);

        // A group shorter than its minimum already, which only a lone one can be, has nothing to spare.
        var lengths = slots.Select(slot => slot.Length).ToArray();
        var floors = slots
            .Select((slot, index) => slot.IsTitleOnly ? slot.Length : Math.Min(slot.Length, entries[index].MinimumLength))
            .ToArray();
        var extraGap = slots.Count > 0 ? gap : 0;
        var found = RoomTaking.Take(lengths, floors, at, thickness + extraGap, othersToo: true);
        var room = Math.Max(0, found - extraGap);

        var placed = new Slot[slots.Count];
        var roomSlot = new Slot(0, room, IsTitleOnly: false);
        var start = 0.0;
        for (var index = 0; index <= slots.Count; index++)
        {
            if (index == at)
            {
                roomSlot = new Slot(start, room, IsTitleOnly: false);
                start += room + gap;
            }

            if (index == slots.Count)
                break;

            placed[index] = new Slot(start, lengths[index], slots[index].IsTitleOnly);
            start += lengths[index] + gap;
        }

        return (placed, roomSlot);
    }

    private static void SqueezeFromTheEnd(IReadOnlyList<Entry> entries, bool[] isTitleOnly, double usable,
        double titleLength)
    {
        for (var index = entries.Count - 1; index >= 0; index--)
        {
            var open = Enumerable.Range(0, entries.Count).Where(other => !isTitleOnly[other]).ToList();
            var room = usable - titleLength * (entries.Count - open.Count);
            if (open.Count <= 1 || room >= open.Sum(other => entries[other].MinimumLength))
                return;

            isTitleOnly[index] = true;
        }
    }

    // In proportion to their shares, except that a group whose share is below its minimum gets its minimum,
    // and the others split what's left.
    private static double[] ShareOut(IReadOnlyList<Entry> entries, bool[] isTitleOnly, double room)
    {
        var lengths = new double[entries.Count];
        var open = Enumerable.Range(0, entries.Count).Where(index => !isTitleOnly[index]).ToList();

        // A lone group fills the stack, even if there's less room than its minimum.
        if (open.Count == 1)
        {
            lengths[open[0]] = room;
            return lengths;
        }

        var free = open;
        var freeRoom = room;
        while (true)
        {
            var shares = free.Sum(index => ValidShare(entries[index]));
            var currentRoom = freeRoom;
            var tooSmall = free.FindIndex(index =>
                currentRoom * ValidShare(entries[index]) / shares < entries[index].MinimumLength);

            if (tooSmall < 0)
            {
                foreach (var index in free)
                    lengths[index] = freeRoom * ValidShare(entries[index]) / shares;

                return lengths;
            }

            lengths[free[tooSmall]] = entries[free[tooSmall]].MinimumLength;
            freeRoom -= entries[free[tooSmall]].MinimumLength;
            free.RemoveAt(tooSmall);
        }
    }

    private static double ValidShare(Entry entry) =>
        double.IsFinite(entry.Share) && entry.Share > 0 ? entry.Share : 1;
}
