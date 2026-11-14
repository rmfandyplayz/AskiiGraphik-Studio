// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AskiiArt.Core.Documents;

/// <summary>
/// One cell of a layer (ARCHITECTURE §5.3). It can hold a glyph, a background colour, both, or
/// neither. A glyph may be a space, which is a deliberate blank rather than nothing (§5.4). A
/// cell holding neither is <see cref="Empty"/>, and the layers below show through it.
/// </summary>
/// <remarks>
/// <c>default(Cell)</c> is the empty cell, so a new cell array starts out empty. Only a cell with
/// a glyph has a text colour. <see cref="Attributes"/> and <see cref="Alpha"/> are reserved hooks
/// (§18): they always hold their defaults, so they aren't stored; one that becomes real gets a
/// field. A test keeps the struct within its 16-byte target.
/// </remarks>
public readonly struct Cell : IEquatable<Cell>
{
    /// <summary>The <see cref="Alpha"/> of every cell that isn't empty.</summary>
    public const byte OpaqueAlpha = byte.MaxValue;

    // Reserved members stay instance members so callers don't change when they gain real data.
    private const string ReservedJustification = "Reserved per-cell data (ARCHITECTURE §18).";

    private Cell(Rune glyph, bool hasGlyph, CellColor foreground, CellColor background)
    {
        Glyph = glyph;
        HasGlyph = hasGlyph;
        Foreground = foreground;
        Background = background;
    }

    /// <summary>The cell that holds nothing. Same as <c>default(Cell)</c>.</summary>
    public static Cell Empty => default;

    /// <summary>The cell's character. Without a glyph this is U+0000, which is never a real one.</summary>
    public Rune Glyph { get; }

    /// <summary>True when the cell holds a glyph, which may be a space.</summary>
    public bool HasGlyph { get; }

    /// <summary>True when the cell has a background colour.</summary>
    public bool HasBackground => !Background.IsUnset;

    /// <summary>True when the cell holds neither a glyph nor a background.</summary>
    public bool IsEmpty => !HasGlyph && Background.IsUnset;

    /// <summary>The glyph's colour. Unset means the default text colour; always unset without a glyph.</summary>
    public CellColor Foreground { get; }

    /// <summary>The background colour. Unset means none, so the layers below show their backgrounds (§5.6).</summary>
    public CellColor Background { get; }

    /// <summary>
    /// Reserved; always <see cref="CellAttributes.None"/>. The base app has no text attributes,
    /// but the hook stays for plugins (ADR 0036).
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ReservedJustification)]
    public CellAttributes Attributes => CellAttributes.None;

    /// <summary>Reserved. <see cref="OpaqueAlpha"/> for every cell that isn't empty, and 0 for an empty one.</summary>
    public byte Alpha => IsEmpty ? (byte)0 : OpaqueAlpha;

    /// <summary>Creates a cell holding <paramref name="glyph"/>, with no colours.</summary>
    /// <exception cref="ArgumentException">The glyph is a control character, which is never stored (§6).</exception>
    public static Cell FromGlyph(Rune glyph) => FromGlyph(glyph, CellColor.Unset, CellColor.Unset);

    /// <summary>Creates a cell holding <paramref name="glyph"/> in these colours.</summary>
    /// <exception cref="ArgumentException">The glyph is a control character, which is never stored (§6).</exception>
    public static Cell FromGlyph(Rune glyph, CellColor foreground, CellColor background)
    {
        if (Rune.IsControl(glyph))
        {
            throw new ArgumentException(
                $"Control characters can't be stored in a cell (U+{glyph.Value:X4}).", nameof(glyph));
        }

        return new Cell(glyph, hasGlyph: true, foreground, background);
    }

    /// <summary>
    /// Creates a cell holding only <paramref name="background"/>, which colours behind the glyphs
    /// of the layers below without hiding them (ADR 0036). An unset background gives <see cref="Empty"/>.
    /// </summary>
    public static Cell FromBackground(CellColor background) =>
        new(default, hasGlyph: false, CellColor.Unset, background);

    /// <summary>This cell with its text colour changed.</summary>
    /// <exception cref="InvalidOperationException">The colour is set but the cell has no glyph to colour.</exception>
    public Cell WithForeground(CellColor foreground)
    {
        if (!HasGlyph && !foreground.IsUnset)
            throw new InvalidOperationException("A cell without a glyph has no text colour.");

        return new Cell(Glyph, HasGlyph, foreground, Background);
    }

    /// <summary>This cell with its background changed. Without a glyph, an unset background leaves it empty.</summary>
    public Cell WithBackground(CellColor background) => new(Glyph, HasGlyph, Foreground, background);

    /// <summary>This cell without its glyph and text colour, keeping its background.</summary>
    public Cell WithoutGlyph() => FromBackground(Background);

    /// <inheritdoc/>
    public bool Equals(Cell other) =>
        HasGlyph == other.HasGlyph && Glyph == other.Glyph && Foreground == other.Foreground
        && Background == other.Background;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(HasGlyph, Glyph, Foreground, Background);

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsEmpty)
            return "(empty)";

        var text = new StringBuilder(HasGlyph ? $"'{Glyph}'" : "(no glyph)");
        if (!Foreground.IsUnset)
            text.Append(" in ").Append(Foreground);

        if (HasBackground)
            text.Append(" on ").Append(Background);

        return text.ToString();
    }

    /// <summary>True when both cells are equal in every field.</summary>
    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    /// <summary>True when the cells differ in any field.</summary>
    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
}
