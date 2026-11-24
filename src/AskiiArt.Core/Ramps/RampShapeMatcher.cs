// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>
/// Picks the level of a ramp whose glyph best shows a cell whose parts should be unevenly dark (ARCHITECTURE §7,
/// ADRs 0068 and 0075): the PATS matching (perceptually accurate text-based shapes), which M6's image conversion builds
/// on.
/// <para>
/// The target is an intensity for each <see cref="GlyphShape"/> part, from 0 to 1. A level is judged by two things:
/// how far its intensity is from the target's average, which keeps the ramp in charge of how dark things get, and how
/// much of the way the target strays from its average the glyph's ink can show. A glyph shows its ink's pattern scaled
/// down as far as fits best, never up, so a glyph is never blamed for being bolder than a faint edge, and one whose
/// ink lies the wrong way shows nothing of the edge. An even target has nothing to show, so it always gets the level
/// nearest its intensity, as without matching.
/// </para>
/// <para>
/// The Brush and the Soft Eraser also judge where the ink lies across the cells around (<see cref="CellNeighbours"/>),
/// and can be strict about faint edges; both are internal to them.
/// </para>
/// </summary>
public sealed class RampShapeMatcher
{
    // How much more where the ink lies counts than how dark the level is, part for part. Tuned by eye on the bundled
    // fonts' Fine and Standard ramps: at 1, a faint edge cell keeps a glyph whose ink is on the wrong side, because a
    // level's worth of darkness outweighs it.
    private const double ShapeWeight = 4;

    // Strict edges: below this average intensity, where the ink lies counts for more, by the square of how much fainter
    // the edge is, up to MostFaintEdgeBoost times.
    private const double FaintEdgeIntensity = 0.5;
    private const double MostFaintEdgeBoost = 25;

    // Strict edges: a target or a glyph whose parts stray from its average by less than this share of it, as a sum of
    // squares, counts as even, so it has no side for ink to be on.
    private const double UnevenShare = 0.05;

    // The share of a target's unevenness a glyph must show for its edge to count as shown. Tuned by eye: at 0.5 hardly
    // any edge counted, and the brush looked as it did before step 4.7.
    private const double ClearShare = 0.25;

    // How many parts of the neighbouring cells the comparison with them reaches into, across and down: half a cell
    // each way.
    private const int ContextColumns = GlyphShape.Columns / 2;
    private const int ContextRows = GlyphShape.Rows / 2;
    private const int WindowColumns = GlyphShape.Columns + 2 * ContextColumns;
    private const int WindowRows = GlyphShape.Rows + 2 * ContextRows;
    private const int WindowParts = WindowColumns * WindowRows;

    // For each part of the window, which of the nine cells it lies in, numbered across then down from the top left,
    // and which of that cell's parts it is.
    private static readonly int[] _windowCells = new int[WindowParts];
    private static readonly int[] _windowCellParts = new int[WindowParts];

    private readonly double[] _intensities;

    // Each level's ink, part by part, less its intensity: how the glyph strays from an even spread of its darkness.
    private readonly double[][] _strays;
    private readonly double[] _strayWeights;

    // Each level's ink, unblurred, scaled to average the level's intensity: how its cell looks next to others.
    private readonly double[][] _looks;

    // Each level's look less its intensity, in the middle of an empty window and blurred, with its sum of squares. The
    // blur is linear, so a window with neighbours in it blurs to this plus the blurred neighbours.
    private readonly double[][] _windowLooks;
    private readonly double[] _windowLookWeights;

    static RampShapeMatcher()
    {
        for (var row = 0; row < WindowRows; row++)
        {
            for (var column = 0; column < WindowColumns; column++)
            {
                var (dx, dy, part) = Locate(column, row);
                _windowCells[row * WindowColumns + column] = (dy + 1) * 3 + dx + 1;
                _windowCellParts[row * WindowColumns + column] = part;
            }
        }
    }

    /// <summary>
    /// Creates a matcher for <paramref name="ramp"/>'s glyphs, shaped as <paramref name="glyphs"/> measures them.
    /// </summary>
    public RampShapeMatcher(Ramp ramp, IGlyphDensityProvider glyphs)
    {
        ArgumentNullException.ThrowIfNull(ramp);
        ArgumentNullException.ThrowIfNull(glyphs);

        Ramp = ramp;
        var levels = ramp.TopLevel + 1;
        _intensities = new double[levels];
        _strays = new double[levels][];
        _strayWeights = new double[levels];
        _looks = new double[levels][];
        _windowLooks = new double[levels][];
        _windowLookWeights = new double[levels];
        for (var level = 0; level < levels; level++)
        {
            _intensities[level] = ramp.IntensityOf(level);
            var shape = ramp.GlyphOf(level) is { } glyph ? glyphs.GetShape(glyph) : GlyphShape.Empty;
            _strays[level] = StraysOf(shape, _intensities[level]);
            _strayWeights[level] = SumOfSquares(_strays[level]);
            _looks[level] = LookOf(shape, _intensities[level]);
            _windowLooks[level] = WindowLookOf(_looks[level], _intensities[level]);
            _windowLookWeights[level] = SumOfSquares(_windowLooks[level]);
        }
    }

    /// <summary>The ramp whose levels this picks.</summary>
    public Ramp Ramp { get; }

    /// <summary>The level that best shows <paramref name="target"/>, from any of the ramp's levels.</summary>
    /// <param name="target">The intensity each part of the cell should have, from 0 to 1.</param>
    public int LevelFor(ReadOnlySpan<double> target) => LevelFor(target, 0, Ramp.TopLevel);

    /// <summary>
    /// The level from <paramref name="lowest"/> to <paramref name="highest"/> that best shows
    /// <paramref name="target"/>. Where two match equally, the darker wins, as <see cref="Ramp.LevelAt"/> rounds.
    /// </summary>
    /// <param name="target">The intensity each part of the cell should have, from 0 to 1.</param>
    /// <param name="lowest">The lightest level allowed.</param>
    /// <param name="highest">The darkest level allowed.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="target"/> doesn't have <see cref="GlyphShape.PartCount"/> parts, or the levels are out of order
    /// or outside the ramp.
    /// </exception>
    public int LevelFor(ReadOnlySpan<double> target, int lowest, int highest) =>
        Match(target, lowest, highest, isStrict: false, neighbours: null).Level;

    /// <summary>
    /// As <see cref="LevelFor(ReadOnlySpan{double}, int, int)"/>, and says whether the level's glyph shows the edge.
    /// Where <paramref name="neighbours"/> is given, where the ink lies is judged across the cell and half of each
    /// neighbour (ADR 0075).
    /// </summary>
    /// <param name="target">The intensity each part of the cell should have, from 0 to 1.</param>
    /// <param name="lowest">The lightest level allowed.</param>
    /// <param name="highest">The darkest level allowed.</param>
    /// <param name="isStrict">
    /// Whether where the ink lies counts for more the fainter the edge, and a glyph inked on the wrong side of an uneven
    /// target is never picked. If that rules out every level, the cell gets <paramref name="lowest"/>.
    /// </param>
    /// <param name="neighbours">The cells around, filled in by the caller, or null to judge the cell alone.</param>
    internal ShapeMatch Match(ReadOnlySpan<double> target, int lowest, int highest, bool isStrict,
        CellNeighbours? neighbours)
    {
        if (target.Length != GlyphShape.PartCount)
        {
            throw new ArgumentException($"A target has {GlyphShape.PartCount} parts, not {target.Length}.",
                nameof(target));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(lowest);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(highest, Ramp.TopLevel);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lowest, highest);

        var mean = Average(target);
        Span<double> targetStrays = stackalloc double[GlyphShape.PartCount];
        target.CopyTo(targetStrays);
        Blur(targetStrays, GlyphShape.Columns, GlyphShape.Rows);
        var targetStrayWeight = Center(targetStrays);
        if (lowest == highest)
            return new ShapeMatch(lowest, ShowsEdge(lowest, targetStrays, targetStrayWeight));

        neighbours?.Prepare(target, mean);
        var judge = new Judge(this, mean, targetStrayWeight, isStrict, neighbours);

        // A level scores at least its tone gap's share, so the search starts at the level nearest in tone and stops,
        // each way, at the first level whose tone gap alone scores worse than the best so far: those beyond it are
        // further off still.
        var nearest = Math.Clamp(Ramp.LevelAt(mean), lowest, highest);
        var best = nearest;
        var bestScore = judge.ScoreOf(nearest, targetStrays);
        for (var level = nearest + 1; level <= highest && ToneScoreOf(level, mean) <= bestScore; level++)
            Consider(level, judge.ScoreOf(level, targetStrays), ref best, ref bestScore);

        for (var level = nearest - 1; level >= lowest && ToneScoreOf(level, mean) <= bestScore; level--)
            Consider(level, judge.ScoreOf(level, targetStrays), ref best, ref bestScore);

        if (double.IsPositiveInfinity(bestScore))
            best = lowest;

        return new ShapeMatch(best, ShowsEdge(best, targetStrays, targetStrayWeight));
    }

    /// <summary>How a level's cell looks next to others: each part's intensity.</summary>
    internal ReadOnlySpan<double> LookOf(int level) => _looks[level];

    // Whether the level's glyph shows at least ClearShare of how the target strays from its average. An even target
    // has no edge to show.
    private bool ShowsEdge(int level, ReadOnlySpan<double> targetStrays, double targetStrayWeight)
    {
        if (targetStrayWeight <= 0)
            return false;

        var (agreement, scale) = FitOf(level, targetStrays);
        var unshown = UnshownStray(level, agreement, scale, targetStrayWeight);
        return 1 - unshown / targetStrayWeight >= ClearShare;
    }

    // Where two levels score the same, the darker wins.
    private static void Consider(int level, double score, ref int best, ref double bestScore)
    {
        var isBetter = level > best ? score <= bestScore : score < bestScore;
        if (!isBetter)
            return;

        best = level;
        bestScore = score;
    }

    private double ToneScoreOf(int level, double mean)
    {
        var toneGap = _intensities[level] - mean;
        return GlyphShape.PartCount * toneGap * toneGap;
    }

    // How much the glyph's strays agree with the target's, and how far down they scale to fit best, from 0 to 1.
    private (double Agreement, double Scale) FitOf(int level, ReadOnlySpan<double> targetStrays)
    {
        var strays = _strays[level];
        var strayWeight = _strayWeights[level];
        if (strayWeight <= 0)
            return (0, 0);

        var agreement = 0.0;
        for (var part = 0; part < strays.Length; part++)
            agreement += strays[part] * targetStrays[part];

        return (agreement, Math.Clamp(agreement / strayWeight, 0, 1));
    }

    // How much of the target's stray from its average is left once the glyph's strays, scaled down as far as fits
    // best, are taken away from it. It can't be below 0, but rounding could take it there, and the search relies on it.
    private double UnshownStray(int level, double agreement, double scale, double targetStrayWeight) =>
        Math.Max(targetStrayWeight - 2 * scale * agreement + scale * scale * _strayWeights[level], 0);

    // The glyph's ink spread as its blurred shape spreads it, scaled to average the level's intensity, less that
    // intensity.
    private static double[] StraysOf(GlyphShape shape, double intensity)
    {
        var strays = new double[GlyphShape.PartCount];
        if (shape.Ink <= 0)
            return strays;

        Span<double> parts = stackalloc double[GlyphShape.PartCount];
        shape.Parts.CopyTo(parts);
        Blur(parts, GlyphShape.Columns, GlyphShape.Rows);
        var ink = Average(parts);
        for (var part = 0; part < strays.Length; part++)
            strays[part] = intensity * (parts[part] / ink - 1);

        return strays;
    }

    private static double[] LookOf(GlyphShape shape, double intensity)
    {
        var look = new double[GlyphShape.PartCount];
        if (shape.Ink <= 0)
        {
            Array.Fill(look, intensity);
            return look;
        }

        for (var part = 0; part < look.Length; part++)
            look[part] = intensity * shape.Parts[part] / shape.Ink;

        return look;
    }

    private static double[] WindowLookOf(double[] look, double intensity)
    {
        var window = new double[WindowParts];
        for (var row = 0; row < GlyphShape.Rows; row++)
        {
            for (var column = 0; column < GlyphShape.Columns; column++)
            {
                var part = row * GlyphShape.Columns + column;
                window[(row + ContextRows) * WindowColumns + column + ContextColumns] = look[part] - intensity;
            }
        }

        Blur(window, WindowColumns, WindowRows);
        return window;
    }

    private static double Average(ReadOnlySpan<double> parts)
    {
        var total = 0.0;
        foreach (var part in parts)
            total += part;

        return total / parts.Length;
    }

    private static double SumOfSquares(ReadOnlySpan<double> parts)
    {
        var total = 0.0;
        foreach (var part in parts)
            total += part * part;

        return total;
    }

    // Takes the average away from every part, and returns the sum of the squares left.
    private static double Center(Span<double> parts)
    {
        var mean = Average(parts);
        var weight = 0.0;
        for (var part = 0; part < parts.Length; part++)
        {
            parts[part] -= mean;
            weight += parts[part] * parts[part];
        }

        return weight;
    }

    // Softens each part with its neighbours, weighing them 1, 2, 1 across and then down, as the eye blurs thin
    // strokes; at the sides a part counts as its own neighbour. Without it, matching follows which part a glyph's thin
    // stroke happens to fall in, and picks a few glyphs over and over.
    private static void Blur(Span<double> parts, int columns, int rows)
    {
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

    // Which cell a window part lies in, relative to the middle one, and which of its parts it is.
    private static (int Dx, int Dy, int Part) Locate(int column, int row)
    {
        var x = column - ContextColumns;
        var y = row - ContextRows;
        var dx = x < 0 ? -1 : x >= GlyphShape.Columns ? 1 : 0;
        var dy = y < 0 ? -1 : y >= GlyphShape.Rows ? 1 : 0;
        return (dx, dy, (y - dy * GlyphShape.Rows) * GlyphShape.Columns + x - dx * GlyphShape.Columns);
    }

    /// <summary>The level <see cref="Match"/> picked, and whether its glyph shows the target's edge.</summary>
    internal readonly record struct ShapeMatch(int Level, bool ShowsEdge);

    /// <summary>
    /// The eight cells around one (ADR 0075): what each should look like, and what it looks like now, part by part, in
    /// the order of <see cref="GlyphShape"/>'s parts. Only where each one's ink lies counts, not how dark it is, which
    /// its own level sees to. Make one and fill it in again for each cell.
    /// </summary>
    internal sealed class CellNeighbours
    {
        private const int MiddleCell = 4;

        private readonly double[][] _targets = new double[9][];
        private readonly double[][] _looks = new double[9][];
        private readonly double[][] _targetStrays = new double[9][];
        private readonly double[][] _lookStrays = new double[9][];

        // How the target's strays across the window, blurred, differ from the neighbours' look strays, blurred, with
        // the middle cell empty: what the middle cell's glyph is left to show.
        private readonly double[] _leftToShow = new double[WindowParts];

        public CellNeighbours()
        {
            for (var index = 0; index < 9; index++)
            {
                _targets[index] = new double[GlyphShape.PartCount];
                _looks[index] = new double[GlyphShape.PartCount];
                _targetStrays[index] = new double[GlyphShape.PartCount];
                _lookStrays[index] = new double[GlyphShape.PartCount];
            }
        }

        /// <summary>The sum of the squares of what the middle cell's glyph is left to show.</summary>
        internal double LeftToShowWeight { get; private set; }

        /// <summary>What the neighbour across <paramref name="dx"/> and down <paramref name="dy"/> should look like.</summary>
        public Span<double> TargetOf(int dx, int dy) => _targets[IndexOf(dx, dy)];

        /// <summary>What the neighbour across <paramref name="dx"/> and down <paramref name="dy"/> looks like now.</summary>
        public Span<double> LookOf(int dx, int dy) => _looks[IndexOf(dx, dy)];

        /// <summary>What the middle cell's glyph is left to show, part by part across the window.</summary>
        internal ReadOnlySpan<double> LeftToShow => _leftToShow;

        // Works out what the middle cell's glyph is left to show, for its target and the neighbours as filled in. Each
        // neighbour's look is scaled down to fit its target, as a glyph in a cell of its own is.
        internal void Prepare(ReadOnlySpan<double> target, double mean)
        {
            for (var cell = 0; cell < 9; cell++)
            {
                if (cell != MiddleCell)
                    PrepareNeighbour(cell);
            }

            Span<double> lookWindow = stackalloc double[WindowParts];
            for (var index = 0; index < WindowParts; index++)
            {
                var cell = _windowCells[index];
                var part = _windowCellParts[index];
                if (cell == MiddleCell)
                {
                    _leftToShow[index] = target[part] - mean;
                    lookWindow[index] = 0;
                }
                else
                {
                    _leftToShow[index] = _targetStrays[cell][part];
                    lookWindow[index] = _lookStrays[cell][part];
                }
            }

            Blur(_leftToShow, WindowColumns, WindowRows);
            Blur(lookWindow, WindowColumns, WindowRows);
            for (var index = 0; index < WindowParts; index++)
                _leftToShow[index] -= lookWindow[index];

            LeftToShowWeight = SumOfSquares(_leftToShow);
        }

        private void PrepareNeighbour(int index)
        {
            var targetStrays = _targetStrays[index];
            _targets[index].CopyTo(targetStrays, 0);
            Center(targetStrays);
            var lookStrays = _lookStrays[index];
            _looks[index].CopyTo(lookStrays, 0);
            var lookWeight = Center(lookStrays);
            if (lookWeight <= 0)
                return;

            var agreement = 0.0;
            for (var part = 0; part < lookStrays.Length; part++)
                agreement += lookStrays[part] * targetStrays[part];

            var scale = Math.Clamp(agreement / lookWeight, 0, 1);
            for (var part = 0; part < lookStrays.Length; part++)
                lookStrays[part] *= scale;
        }

        private static int IndexOf(int dx, int dy) => (dy + 1) * 3 + dx + 1;
    }

    // Scores levels for one target.
    private readonly ref struct Judge
    {
        private readonly RampShapeMatcher _matcher;
        private readonly double _mean;
        private readonly double _targetStrayWeight;
        private readonly bool _isStrict;
        private readonly CellNeighbours? _neighbours;
        private readonly double _shapeWeight;
        private readonly bool _isTargetUneven;

        public Judge(RampShapeMatcher matcher, double mean, double targetStrayWeight, bool isStrict,
            CellNeighbours? neighbours)
        {
            _matcher = matcher;
            _mean = mean;
            _targetStrayWeight = targetStrayWeight;
            _isStrict = isStrict;
            _neighbours = neighbours;
            _shapeWeight = ShapeWeight * (isStrict ? FaintEdgeBoost(mean) : 1);
            _isTargetUneven = targetStrayWeight > UnevenShare * GlyphShape.PartCount * mean * mean;
        }

        public double ScoreOf(int level, ReadOnlySpan<double> targetStrays)
        {
            var (agreement, scale) = _matcher.FitOf(level, targetStrays);
            if (_isStrict && _isTargetUneven && IsUnevenGlyph(level) && agreement <= 0)
                return double.PositiveInfinity;

            double unshown;
            if (_neighbours is null)
                unshown = _matcher.UnshownStray(level, agreement, scale, _targetStrayWeight);
            else
                unshown = UnshownInWindow(level, scale);

            return _matcher.ToneScoreOf(level, _mean) + _shapeWeight * unshown;
        }

        private bool IsUnevenGlyph(int level)
        {
            var intensity = _matcher._intensities[level];
            return _matcher._strayWeights[level] > UnevenShare * GlyphShape.PartCount * intensity * intensity;
        }

        // How far the window's strays, with the level's glyph in the middle scaled down as the cell alone would scale
        // it, are from the target's, counted as one cell's worth of parts: the sum of the squares of what's left to
        // show less the glyph's blurred window, scaled.
        private double UnshownInWindow(int level, double scale)
        {
            var leftToShow = _neighbours!.LeftToShow;
            var windowLook = _matcher._windowLooks[level];
            // The blur spreads the glyph only one part past its cell, so the rest of its window is empty and adds
            // nothing.
            var agreement = 0.0;
            for (var row = ContextRows - 1; row <= ContextRows + GlyphShape.Rows; row++)
            {
                var start = row * WindowColumns + ContextColumns - 1;
                for (var part = start; part <= start + GlyphShape.Columns + 1; part++)
                    agreement += leftToShow[part] * windowLook[part];
            }

            var unshown = _neighbours.LeftToShowWeight - 2 * scale * agreement +
                          scale * scale * _matcher._windowLookWeights[level];
            return Math.Max(unshown, 0) * GlyphShape.PartCount / WindowParts;
        }

        private static double FaintEdgeBoost(double mean)
        {
            if (mean >= FaintEdgeIntensity)
                return 1;

            if (mean <= 0)
                return MostFaintEdgeBoost;

            var fainter = FaintEdgeIntensity / mean;
            return Math.Min(fainter * fainter, MostFaintEdgeBoost);
        }
    }
}
