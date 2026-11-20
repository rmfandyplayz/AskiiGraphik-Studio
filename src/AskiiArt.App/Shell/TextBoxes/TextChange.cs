// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.TextBoxes;

/// <summary>What kind of change a <see cref="TextChange"/> is, for grouping changes into undo steps.</summary>
internal enum TextChangeKind
{
    /// <summary>One character typed, with nothing selected.</summary>
    Typing,

    /// <summary>One character deleted, by Backspace or Delete.</summary>
    Deleting,

    /// <summary>Anything else, such as a paste, a cut, or typing over a selection.</summary>
    Other
}

/// <summary>How one text differs from another: at a position, some characters were replaced by others.</summary>
/// <param name="Position">Where the change starts.</param>
/// <param name="Removed">How many characters went, from <paramref name="Position"/> on.</param>
/// <param name="Inserted">How many characters came in their place.</param>
/// <param name="Kind">What kind of change it is.</param>
internal readonly record struct TextChange(int Position, int Removed, int Inserted, TextChangeKind Kind)
{
    /// <summary>
    /// The change from <paramref name="before"/> to <paramref name="after"/>. Where it's ambiguous, as when a letter is
    /// typed next to the same letter, it's taken to be as late in the text as it could be.
    /// </summary>
    public static TextChange Between(string before, string after)
    {
        var shorter = Math.Min(before.Length, after.Length);

        var prefix = 0;
        while (prefix < shorter && before[prefix] == after[prefix])
            prefix++;

        var suffix = 0;
        while (suffix < shorter - prefix && before[^(suffix + 1)] == after[^(suffix + 1)])
            suffix++;

        var removed = before.Length - prefix - suffix;
        var inserted = after.Length - prefix - suffix;
        var kind = TextChangeKind.Other;
        if (removed == 0 && IsOneCharacter(after, prefix, inserted))
            kind = TextChangeKind.Typing;
        else if (inserted == 0 && IsOneCharacter(before, prefix, removed))
            kind = TextChangeKind.Deleting;

        return new TextChange(prefix, removed, inserted, kind);
    }

    /// <summary>
    /// True when this change carries on from <paramref name="last"/> as part of the same typing or deleting: typing
    /// just after the last character typed, or deleting just before (Backspace) or at (Delete) the last one deleted.
    /// </summary>
    public bool FollowsOn(TextChange last)
    {
        if (Kind != last.Kind)
            return false;

        return Kind switch
        {
            TextChangeKind.Typing => Position == last.Position + last.Inserted,
            TextChangeKind.Deleting => Position == last.Position || Position + Removed == last.Position,
            _ => false
        };
    }

    // A character outside the Basic Multilingual Plane, such as an emoji, takes two chars.
    private static bool IsOneCharacter(string text, int start, int length) =>
        length == 1 || (length == 2 && char.IsSurrogatePair(text[start], text[start + 1]));
}
