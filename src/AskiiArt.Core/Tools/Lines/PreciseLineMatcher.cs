// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// Picks <see cref="LineStyle.Precise"/> characters: for each cell, the candidate whose measured ink (ADR 0068) lies
/// most like a thin stroke along the path through that cell.
/// </summary>
internal sealed class PreciseLineMatcher
{
    /// <summary>The characters Precise lines choose from.</summary>
    public const string CandidateCharacters = "-_.,'`\"|/\\";

    // The stroke the path is measured as, in cell heights: as thick as one part is tall.
    private const double StrokeHalfWidth = 0.5 / GlyphShape.Rows;

    // Each part is measured at this many points across and down.
    private const int SamplesPerPartSide = 4;

    // An open path's ends are carried on this many cells further, so its first and last cells look crossed right
    // through, as the cells between do, rather than ending at their middles.
    private const double EndExtension = 1.5;

    private readonly double _cellWidthToHeight;
    private readonly (Rune Glyph, double[] Look, double Length)[] _candidates;

    private PreciseLineMatcher(double cellWidthToHeight, (Rune Glyph, double[] Look, double Length)[] candidates)
    {
        _cellWidthToHeight = cellWidthToHeight;
        _candidates = candidates;
    }

    /// <summary>
    /// A matcher for one font, cell shape and character set, or null when the character set allows none of the
    /// candidates or none of them shows any ink.
    /// </summary>
    public static PreciseLineMatcher? Create(double cellWidthToHeight, IGlyphDensityProvider glyphs,
        CharacterSet characterSet)
    {
        var candidates = new List<(Rune, double[], double)>();
        foreach (var character in CandidateCharacters)
        {
            var glyph = new Rune(character);
            if (!characterSet.Allows(glyph))
                continue;

            var look = glyphs.GetShape(glyph).Parts.ToArray();
            Blur(look);
            var length = Length(look);
            if (length > 0)
                candidates.Add((glyph, look, length));
        }

        return candidates.Count == 0 ? null : new PreciseLineMatcher(cellWidthToHeight, [.. candidates]);
    }

    /// <summary>Gets ready to match the cells of <paramref name="path"/>.</summary>
    public PreparedPath Prepare(LinePath path) => new(this, path);

    // Softens each part with its neighbours, 1, 2, 1 across and then down, as RampShapeMatcher does, so a glyph's
    // thin stroke counts for the parts beside the one it falls in.
    private static void Blur(Span<double> parts)
    {
        const int columns = GlyphShape.Columns;
        const int rows = GlyphShape.Rows;
        Span<double> before = stackalloc double[parts.Length];
        parts.CopyTo(before);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var left = before[row * columns + Math.Max(column - 1, 0)];
                var right = before[row * columns + Math.Min(column + 1, columns - 1)];
                parts[row * columns + column] = (left + 2 * before[row * columns + column] + right) / 4;
            }
        }

        parts.CopyTo(before);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var up = before[Math.Max(row - 1, 0) * columns + column];
                var down = before[Math.Min(row + 1, rows - 1) * columns + column];
                parts[row * columns + column] = (up + 2 * before[row * columns + column] + down) / 4;
            }
        }
    }

    private static double Length(ReadOnlySpan<double> parts)
    {
        var sum = 0.0;
        foreach (var part in parts)
            sum += part * part;

        return Math.Sqrt(sum);
    }

    /// <summary>A path ready to match, with its pieces filed by the cells they pass near.</summary>
    internal sealed class PreparedPath
    {
        private readonly PreciseLineMatcher _matcher;
        private readonly List<(CanvasPoint From, CanvasPoint To)> _pieces = [];
        private readonly Dictionary<CellPoint, List<int>> _piecesNearCell = [];

        public PreparedPath(PreciseLineMatcher matcher, LinePath path)
        {
            _matcher = matcher;
            for (var index = 0; index < path.PieceCount; index++)
            {
                var (from, to) = path.Piece(index);
                if (!path.IsClosed && index == 0)
                    from = Extended(to, from);

                if (!path.IsClosed && index == path.PieceCount - 1)
                    to = Extended(from, to);

                AddPiece(from, to);
            }

            if (path.PieceCount == 0)
                AddPiece(path.Points[0], path.Points[0]);
        }

        /// <summary>
        /// The candidate that best shows the path through <paramref name="cell"/>, or null if the path misses it. The
        /// path is measured as if moved back by <paramref name="offset"/>, which centres it in the cell where it runs
        /// near a side no glyph's ink reaches: there matching would otherwise pick by the little ink that strays.
        /// </summary>
        public Rune? GlyphFor(CellPoint cell, CanvasPoint offset)
        {
            Span<double> target = stackalloc double[GlyphShape.PartCount];
            if (!MeasureTarget(cell, offset, target))
                return null;

            Blur(target);
            var targetLength = Length(target);
            if (targetLength == 0)
                return null;

            Rune? best = null;
            var bestScore = double.NegativeInfinity;
            foreach (var (glyph, look, length) in _matcher._candidates)
            {
                var dot = 0.0;
                for (var part = 0; part < GlyphShape.PartCount; part++)
                    dot += target[part] * look[part];

                var score = dot / (targetLength * length);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = glyph;
                }
            }

            return best;
        }

        // Carries the end of the piece from `from` to `to` on past `to`.
        private static CanvasPoint Extended(CanvasPoint from, CanvasPoint to)
        {
            var across = to.X - from.X;
            var down = to.Y - from.Y;
            var longest = Math.Max(Math.Abs(across), Math.Abs(down));
            if (longest == 0)
                return to;

            return new CanvasPoint(to.X + across / longest * EndExtension, to.Y + down / longest * EndExtension);
        }

        // Files each piece, cut into lengths of a cell at most, under every cell it passes near.
        private void AddPiece(CanvasPoint from, CanvasPoint to)
        {
            var lengths = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y))));
            var reachAcross = StrokeHalfWidth / _matcher._cellWidthToHeight;
            for (var step = 0; step < lengths; step++)
            {
                var start = Lerp(from, to, (double)step / lengths);
                var end = Lerp(from, to, (double)(step + 1) / lengths);
                var index = _pieces.Count;
                _pieces.Add((start, end));
                var left = (int)Math.Floor(Math.Min(start.X, end.X) - reachAcross);
                var right = (int)Math.Floor(Math.Max(start.X, end.X) + reachAcross);
                var top = (int)Math.Floor(Math.Min(start.Y, end.Y) - StrokeHalfWidth);
                var bottom = (int)Math.Floor(Math.Max(start.Y, end.Y) + StrokeHalfWidth);
                for (var y = top; y <= bottom; y++)
                {
                    for (var x = left; x <= right; x++)
                    {
                        var cell = new CellPoint(x, y);
                        if (!_piecesNearCell.TryGetValue(cell, out var list))
                            _piecesNearCell[cell] = list = [];

                        list.Add(index);
                    }
                }
            }
        }

        private static CanvasPoint Lerp(CanvasPoint from, CanvasPoint to, double share) =>
            new(from.X + (to.X - from.X) * share, from.Y + (to.Y - from.Y) * share);

        // How much of each part of the cell a stroke along the path covers, measured at a grid of points in each
        // part, with distances as they look on screen, in cell heights.
        private bool MeasureTarget(CellPoint cell, CanvasPoint offset, Span<double> target)
        {
            var nearby = PiecesNear(cell, offset);
            if (nearby.Count == 0)
                return false;

            const int samples = SamplesPerPartSide * SamplesPerPartSide;
            var aspect = _matcher._cellWidthToHeight;
            for (var row = 0; row < GlyphShape.Rows; row++)
            {
                for (var column = 0; column < GlyphShape.Columns; column++)
                {
                    var inked = 0;
                    for (var down = 0; down < SamplesPerPartSide; down++)
                    {
                        var y = cell.Y + offset.Y + (row + (down + 0.5) / SamplesPerPartSide) / GlyphShape.Rows;
                        for (var across = 0; across < SamplesPerPartSide; across++)
                        {
                            var x = cell.X + offset.X +
                                    (column + (across + 0.5) / SamplesPerPartSide) / GlyphShape.Columns;
                            if (IsOnStroke(x, y, nearby, aspect))
                                inked++;
                        }
                    }

                    target[row * GlyphShape.Columns + column] = (double)inked / samples;
                }
            }

            return true;
        }

        // The pieces filed under the cells the measured area overlaps, once each.
        private List<int> PiecesNear(CellPoint cell, CanvasPoint offset)
        {
            var pieces = new List<int>();
            var left = (int)Math.Floor(cell.X + offset.X);
            var right = (int)Math.Ceiling(cell.X + offset.X + 1) - 1;
            var top = (int)Math.Floor(cell.Y + offset.Y);
            var bottom = (int)Math.Ceiling(cell.Y + offset.Y + 1) - 1;
            for (var y = top; y <= bottom; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    if (_piecesNearCell.TryGetValue(new CellPoint(x, y), out var filed))
                        pieces.AddRange(filed.Where(index => !pieces.Contains(index)));
                }
            }

            return pieces;
        }

        private bool IsOnStroke(double x, double y, List<int> nearby, double aspect)
        {
            foreach (var index in nearby)
            {
                var (from, to) = _pieces[index];
                if (DistanceSquared(x, y, from, to, aspect) <= StrokeHalfWidth * StrokeHalfWidth)
                    return true;
            }

            return false;
        }

        // The squared distance from the point to the piece, as it looks on screen, in cell heights.
        private static double DistanceSquared(double x, double y, CanvasPoint from, CanvasPoint to, double aspect)
        {
            var pieceX = (to.X - from.X) * aspect;
            var pieceY = to.Y - from.Y;
            var pointX = (x - from.X) * aspect;
            var pointY = y - from.Y;
            var lengthSquared = pieceX * pieceX + pieceY * pieceY;
            var share = 0.0;
            if (lengthSquared > 0)
                share = Math.Clamp((pointX * pieceX + pointY * pieceY) / lengthSquared, 0, 1);

            var awayX = pointX - share * pieceX;
            var awayY = pointY - share * pieceY;
            return awayX * awayX + awayY * awayY;
        }
    }
}
