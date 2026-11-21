// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// What the Magic Wand compares, for every cell of the canvas, taken once at the start of a click (ADR 0063), so a
/// drag that changes the tolerance only compares again and fills. Read it on the UI thread.
/// </summary>
internal sealed class MagicWandSample
{
    // A colour key for a cell with no colour to compare: no background, or a text colour left as the default.
    private const int Unset = -1;

    // A glyph or text colour key for a blank cell, empty or holding a space, whose text colour doesn't show.
    private const int NoInk = -2;

    private static readonly Rune _space = new(' ');

    private readonly MagicWandMatch _match;
    private readonly int[] _keys;
    private readonly float[] _lightness;
    private bool[] _matches = [];
    private bool[] _visited = [];

    private MagicWandSample(CanvasSize canvasSize, MagicWandMatch match, int[] keys, float[] lightness)
    {
        CanvasSize = canvasSize;
        _match = match;
        _keys = keys;
        _lightness = lightness;
    }

    /// <summary>The size of the canvas the sample was taken from.</summary>
    public CanvasSize CanvasSize { get; }

    /// <summary>
    /// Takes a sample of <paramref name="session"/>'s active layer, or of its visible layers together, for comparing
    /// cells by <paramref name="match"/>. Glyphs' lightness comes from <paramref name="densities"/>.
    /// </summary>
    public static MagicWandSample Take(EditingSession session, bool samplesVisibleLayers, MagicWandMatch match,
        IGlyphDensityProvider densities)
    {
        var canvasSize = session.Document.CanvasSize;
        var width = canvasSize.Width;
        var keys = match == MagicWandMatch.Lightness ? [] : new int[canvasSize.CellCount];
        var lightness = match == MagicWandMatch.Lightness ? new float[canvasSize.CellCount] : [];

        // A row at a time, so the sample never holds a copy of every cell.
        var row = new Cell[width];
        for (var y = 0; y < canvasSize.Height; y++)
        {
            var rowRect = new CellRect(0, y, width, 1);
            if (samplesVisibleLayers)
                Compositor.CompositeRegion(session.Document.ActiveFrame, rowRect, row);
            else
                session.ActiveLayer.CopyCells(rowRect, row);

            var rowStart = y * width;
            for (var x = 0; x < width; x++)
            {
                var cell = row[x];
                switch (match)
                {
                    case MagicWandMatch.Lightness:
                        lightness[rowStart + x] = HasInk(cell) ? (float)densities.GetDensity(cell.Glyph) : 0;
                        break;
                    case MagicWandMatch.TextColor:
                        keys[rowStart + x] = HasInk(cell) ? KeyOf(cell.Foreground) : NoInk;
                        break;
                    case MagicWandMatch.BackgroundColor:
                        keys[rowStart + x] = KeyOf(cell.Background);
                        break;
                    default:
                        keys[rowStart + x] = HasInk(cell) ? cell.Glyph.Value : NoInk;
                        break;
                }
            }
        }

        return new MagicWandSample(canvasSize, match, keys, lightness);
    }

    /// <summary>
    /// The cells that match the one at <paramref name="seed"/> within <paramref name="tolerance"/> percent: only those
    /// joined to it along their sides when <paramref name="isContiguous"/>, otherwise every one.
    /// </summary>
    public SelectionMask Select(CellPoint seed, int tolerance, bool isContiguous)
    {
        var width = CanvasSize.Width;
        var count = width * CanvasSize.Height;
        if (_matches.Length < count)
            _matches = new bool[count];

        var matches = _matches.AsSpan(0, count);
        FindMatches(seed.Y * width + seed.X, tolerance, matches);
        var builder = new SelectionMaskBuilder(CanvasSize);
        if (isContiguous)
            Fill(seed, matches, builder);
        else
            AddAll(matches, builder);

        return builder.Build();
    }

    // A space and an empty cell both leave their cell blank.
    private static bool HasInk(Cell cell) => cell.HasGlyph && cell.Glyph != _space;

    private static int KeyOf(CellColor color)
    {
        if (color.IsUnset)
            return Unset;

        if (!color.TryGetRgb(out var rgb) && color.TryGetAnsi(out var ansi))
            rgb = AnsiPalette.Vga[ansi];

        return rgb.Red << 16 | rgb.Green << 8 | rgb.Blue;
    }

    private void FindMatches(int seedIndex, int tolerance, Span<bool> matches)
    {
        var share = Math.Clamp(tolerance, 0, 100) / 100.0;
        if (_match == MagicWandMatch.Lightness)
        {
            // A hair over the share, so lightnesses that differ by exactly the tolerance match despite rounding.
            var seedLightness = _lightness[seedIndex];
            var reachOfLightness = (float)share + 1e-5f;
            for (var index = 0; index < matches.Length; index++)
                matches[index] = Math.Abs(_lightness[index] - seedLightness) <= reachOfLightness;

            return;
        }

        var seedKey = _keys[seedIndex];
        if (_match == MagicWandMatch.Glyph || seedKey < 0)
        {
            for (var index = 0; index < matches.Length; index++)
                matches[index] = _keys[index] == seedKey;

            return;
        }

        var reach = (int)Math.Round(share * byte.MaxValue);
        for (var index = 0; index < matches.Length; index++)
            matches[index] = _keys[index] >= 0 && IsWithin(_keys[index], seedKey, reach);
    }

    // Each of red, green and blue differs by at most the reach.
    private static bool IsWithin(int key, int seedKey, int reach) =>
        Math.Abs((key >> 16) - (seedKey >> 16)) <= reach
        && Math.Abs((key >> 8 & 0xFF) - (seedKey >> 8 & 0xFF)) <= reach
        && Math.Abs((key & 0xFF) - (seedKey & 0xFF)) <= reach;

    private void AddAll(ReadOnlySpan<bool> matches, SelectionMaskBuilder builder)
    {
        var width = CanvasSize.Width;
        for (var row = 0; row < CanvasSize.Height; row++)
        {
            var cells = matches.Slice(row * width, width);
            var column = 0;
            while (column < width)
            {
                if (!cells[column])
                {
                    column++;
                    continue;
                }

                var start = column;
                while (column < width && cells[column])
                    column++;

                builder.AddRun(row, start, column - start);
            }
        }
    }

    // A scanline fill: each run of matching cells is added whole, and the rows above and below are searched along it
    // for runs to fill next. Cells meet only along their sides.
    private void Fill(CellPoint seed, ReadOnlySpan<bool> matches, SelectionMaskBuilder builder)
    {
        var width = CanvasSize.Width;
        var height = CanvasSize.Height;
        if (_visited.Length < matches.Length)
            _visited = new bool[matches.Length];

        var visited = _visited.AsSpan(0, matches.Length);
        visited.Clear();
        var pending = new Stack<CellPoint>();
        pending.Push(seed);
        while (pending.TryPop(out var point))
        {
            var rowStart = point.Y * width;
            if (visited[rowStart + point.X] || !matches[rowStart + point.X])
                continue;

            var left = point.X;
            while (left > 0 && matches[rowStart + left - 1] && !visited[rowStart + left - 1])
                left--;

            var right = point.X;
            while (right < width - 1 && matches[rowStart + right + 1] && !visited[rowStart + right + 1])
                right++;

            visited.Slice(rowStart + left, right - left + 1).Fill(true);
            builder.AddRun(point.Y, left, right - left + 1);
            if (point.Y > 0)
                PushRuns(matches, visited, point.Y - 1, left, right, pending);

            if (point.Y < height - 1)
                PushRuns(matches, visited, point.Y + 1, left, right, pending);
        }
    }

    // Pushes the first cell of each run of matching cells not yet filled in the row, between the columns.
    private void PushRuns(ReadOnlySpan<bool> matches, ReadOnlySpan<bool> visited, int row, int left, int right,
        Stack<CellPoint> pending)
    {
        var rowStart = row * CanvasSize.Width;
        var isInRun = false;
        for (var column = left; column <= right; column++)
        {
            var isOpen = matches[rowStart + column] && !visited[rowStart + column];
            if (isOpen && !isInRun)
                pending.Push(new CellPoint(column, row));

            isInRun = isOpen;
        }
    }
}
