// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

/// <summary>Masks drawn as text: '#' is a selected cell, anything else isn't.</summary>
internal static class TestMasks
{
    public const char Selected = '#';

    /// <summary>A mask on a canvas exactly as big as <paramref name="rows"/>.</summary>
    public static SelectionMask Parse(params string[] rows)
    {
        var builder = new SelectionMaskBuilder(new CanvasSize(rows[0].Length, rows.Length));
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] == Selected)
                    builder.Add(new CellPoint(x, y));
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// The whole canvas, in the format <see cref="Parse"/> takes, with '·' for a cell that isn't selected.
    /// </summary>
    public static string[] Describe(SelectionMask mask)
    {
        var rows = new string[mask.CanvasSize.Height];
        for (var y = 0; y < rows.Length; y++)
        {
            var row = new char[mask.CanvasSize.Width];
            for (var x = 0; x < row.Length; x++)
                row[x] = mask.Contains(new CellPoint(x, y)) ? Selected : TestCells.EmptyMarker;

            rows[y] = new string(row);
        }

        return rows;
    }

    /// <summary>A mask of <paramref name="size"/> with each cell selected at random, about half of them.</summary>
    public static SelectionMask Random(CanvasSize size, int seed)
    {
        var random = new Random(seed);
        var builder = new SelectionMaskBuilder(size);
        for (var y = 0; y < size.Height; y++)
        {
            for (var x = 0; x < size.Width; x++)
            {
                if (random.Next(2) == 0)
                    builder.Add(new CellPoint(x, y));
            }
        }

        return builder.Build();
    }
}
