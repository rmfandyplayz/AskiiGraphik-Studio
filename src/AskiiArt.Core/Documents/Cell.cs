// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AskiiArt.Core.Documents;

/// <summary>
/// One cell of a layer (ARCHITECTURE §5.3): either <see cref="Empty"/>, so the layer below shows
/// through, or occupied by a glyph. An occupied space is a deliberate blank, not an empty cell
/// (§5.4).
/// </summary>
/// <remarks>
/// <c>default(Cell)</c> is the empty cell, so a new cell array starts out empty.
/// <see cref="Foreground"/>, <see cref="Background"/>, <see cref="Attributes"/> and
/// <see cref="Alpha"/> are reserved hooks (§18). In v1 they always hold their defaults, so they
/// aren't stored yet; when one becomes real it gets a field. A test keeps the struct within its
/// 16-byte target.
/// </remarks>
public readonly struct Cell : IEquatable<Cell>
{
    /// <summary>The <see cref="Alpha"/> of every occupied cell in v1.</summary>
    public const byte OpaqueAlpha = byte.MaxValue;

    // Reserved members stay instance members so callers don't change when they gain real data.
    private const string ReservedJustification = "Reserved per-cell data (ARCHITECTURE §18).";

    private Cell(Rune glyph)
    {
        Glyph = glyph;
        IsOccupied = true;
    }

    /// <summary>The cell that holds nothing. Same as <c>default(Cell)</c>.</summary>
    public static Cell Empty => default;

    /// <summary>The cell's character. For an empty cell this is U+0000, which is never a real glyph.</summary>
    public Rune Glyph { get; }

    /// <summary>True when the cell holds a glyph, which may be a space.</summary>
    public bool IsOccupied { get; }

    /// <summary>True when the cell holds nothing.</summary>
    public bool IsEmpty => !IsOccupied;

    /// <summary>Reserved; always <see cref="CellColor.Unset"/> in v1.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ReservedJustification)]
    public CellColor Foreground => CellColor.Unset;

    /// <summary>Reserved; always <see cref="CellColor.Unset"/> in v1.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ReservedJustification)]
    public CellColor Background => CellColor.Unset;

    /// <summary>Reserved; always <see cref="CellAttributes.None"/> in v1.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = ReservedJustification)]
    public CellAttributes Attributes => CellAttributes.None;

    /// <summary>Reserved. <see cref="OpaqueAlpha"/> for every occupied cell in v1, and 0 for an empty cell.</summary>
    public byte Alpha => IsOccupied ? OpaqueAlpha : (byte)0;

    /// <summary>Creates an occupied cell holding <paramref name="glyph"/>.</summary>
    /// <exception cref="ArgumentException">The glyph is a control character, which is never stored (§6).</exception>
    public static Cell FromGlyph(Rune glyph)
    {
        if (Rune.IsControl(glyph))
        {
            throw new ArgumentException(
                $"Control characters can't be stored in a cell (U+{glyph.Value:X4}).", nameof(glyph));
        }

        return new Cell(glyph);
    }

    /// <inheritdoc/>
    public bool Equals(Cell other) => IsOccupied == other.IsOccupied && Glyph == other.Glyph;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(IsOccupied, Glyph);

    /// <inheritdoc/>
    public override string ToString() => IsOccupied ? $"'{Glyph}'" : "(empty)";

    /// <summary>True when both cells are equal in every field.</summary>
    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    /// <summary>True when the cells differ in any field.</summary>
    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);
}
