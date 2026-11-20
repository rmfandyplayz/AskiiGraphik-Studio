// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Numerics;

namespace AskiiArt.Core.Selection;

/// <summary>
/// Helpers for a row of bits stored in 64-bit words, bit <c>i</c> of the row being bit <c>i % 64</c> of word
/// <c>i / 64</c>. A selection keeps a row like this for each row of cells, so these work a word at a time rather than
/// a cell at a time, and never allocate.
/// </summary>
internal static class BitRow
{
    public const int BitsPerWord = 64;

    /// <summary>How many words a row of <paramref name="bitCount"/> bits takes.</summary>
    public static int WordsFor(int bitCount) => (bitCount + BitsPerWord - 1) / BitsPerWord;

    /// <summary>Whether bit <paramref name="index"/> is set.</summary>
    public static bool IsSet(ReadOnlySpan<ulong> row, int index) =>
        (row[index / BitsPerWord] & (1UL << (index % BitsPerWord))) != 0;

    /// <summary>Sets <paramref name="length"/> bits from <paramref name="start"/>.</summary>
    public static void SetRange(Span<ulong> row, int start, int length)
    {
        var end = start + length;
        while (start < end)
        {
            var wordIndex = start / BitsPerWord;
            var firstBit = start % BitsPerWord;
            var bitCount = Math.Min(BitsPerWord - firstBit, end - start);
            var bits = bitCount == BitsPerWord ? ulong.MaxValue : ((1UL << bitCount) - 1) << firstBit;
            row[wordIndex] |= bits;
            start += bitCount;
        }
    }

    /// <summary>
    /// The first set bit at or after <paramref name="from"/>, or <paramref name="length"/> when there's none.
    /// </summary>
    public static int NextSetBit(ReadOnlySpan<ulong> row, int from, int length) =>
        NextBit(row, from, length, invert: false);

    /// <summary>
    /// The first clear bit at or after <paramref name="from"/>, or <paramref name="length"/> when there's none.
    /// </summary>
    public static int NextClearBit(ReadOnlySpan<ulong> row, int from, int length) =>
        NextBit(row, from, length, invert: true);

    /// <summary>The runs of set bits in the first <paramref name="length"/> bits of a row, in order.</summary>
    public static RunEnumerator Runs(ReadOnlySpan<ulong> row, int length) => new(row, length);

    /// <summary>The number of set bits.</summary>
    public static long CountSetBits(ReadOnlySpan<ulong> row)
    {
        long count = 0;
        foreach (var word in row)
            count += BitOperations.PopCount(word);

        return count;
    }

    private static int NextBit(ReadOnlySpan<ulong> row, int from, int length, bool invert)
    {
        if (from >= length)
            return length;

        var wordIndex = from / BitsPerWord;
        var flip = invert ? ulong.MaxValue : 0;
        var word = (row[wordIndex] ^ flip) & (ulong.MaxValue << (from % BitsPerWord));
        while (word == 0)
        {
            wordIndex++;
            if (wordIndex >= row.Length)
                return length;

            word = row[wordIndex] ^ flip;
        }

        return Math.Min(length, wordIndex * BitsPerWord + BitOperations.TrailingZeroCount(word));
    }

    /// <summary>Walks the runs of set bits in a row, with <c>foreach</c> and without allocating.</summary>
    public ref struct RunEnumerator
    {
        private readonly ReadOnlySpan<ulong> _row;
        private readonly int _length;
        private int _position;

        public RunEnumerator(ReadOnlySpan<ulong> row, int length)
        {
            _row = row;
            _length = length;
        }

        /// <summary>The current run: its first bit and how many bits it has.</summary>
        public (int Start, int Length) Current { get; private set; }

        public readonly RunEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var start = NextSetBit(_row, _position, _length);
            if (start >= _length)
                return false;

            var end = NextClearBit(_row, start + 1, _length);
            Current = (start, end - start);
            _position = end;
            return true;
        }
    }
}
