// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// A scale of glyphs from lightest to darkest that turns an intensity into a glyph (ARCHITECTURE §7, ADR 0045).
/// A ramp of n glyphs has n + 1 levels, evenly spaced from intensity 0 to 1: level 0 is no ink (an empty cell or a
/// space, as <see cref="ZeroLevel"/> says), and level k is the k-th glyph. An intensity paints the level nearest it.
/// A ramp is an immutable value. Its name belongs to wherever it's kept, so two ramps with the same glyphs are equal.
/// </summary>
public sealed class Ramp : IEquatable<Ramp>
{
    private static readonly Rune _space = new(' ');

    private readonly Rune[] _glyphs;

    /// <summary>Creates a ramp of <paramref name="glyphs"/>, lightest first.</summary>
    /// <param name="glyphs">At least one glyph, each only once. A space can't be one: it's the zero level's.</param>
    /// <param name="zeroLevel">What intensity 0 paints.</param>
    /// <exception cref="ArgumentException">There are no glyphs, or one is a space, repeated, or can't fill a cell.
    /// </exception>
    public Ramp(IEnumerable<Rune> glyphs, RampZeroLevel zeroLevel = RampZeroLevel.Empty)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        if (!Enum.IsDefined(zeroLevel))
            throw new ArgumentOutOfRangeException(nameof(zeroLevel), zeroLevel, null);

        _glyphs = [.. glyphs];
        var check = CheckGlyphs(_glyphs);
        switch (check.Problem)
        {
            case RampTextProblem.None:
                break;
            case RampTextProblem.Empty:
                throw new ArgumentException("A ramp needs at least one glyph.", nameof(glyphs));
            case RampTextProblem.Repeated:
                throw new ArgumentException($"U+{check.Glyph.Value:X4} is in the ramp more than once.",
                    nameof(glyphs));
            default:
                throw new ArgumentException($"A ramp can't hold U+{check.Glyph.Value:X4}.", nameof(glyphs));
        }

        ZeroLevel = zeroLevel;
    }

    /// <summary>The glyphs, lightest first. Glyph k (counting from 1) is level k's.</summary>
    public IReadOnlyList<Rune> Glyphs => _glyphs;

    /// <summary>What intensity 0 paints.</summary>
    public RampZeroLevel ZeroLevel { get; }

    /// <summary>
    /// The darkest level, the last glyph's. The levels run from 0 to this, which is the number of glyphs.
    /// </summary>
    public int TopLevel => _glyphs.Length;

    /// <summary>Creates a ramp of the characters in <paramref name="glyphs"/>, lightest first.</summary>
    /// <exception cref="ArgumentException">As for the constructor.</exception>
    public static Ramp FromText(string glyphs, RampZeroLevel zeroLevel = RampZeroLevel.Empty)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        return new Ramp(glyphs.EnumerateRunes(), zeroLevel);
    }

    /// <summary>
    /// Whether the characters in <paramref name="glyphs"/> can make a ramp, and if not, the first thing wrong, so
    /// the ramp editor can say why (ADR 0052).
    /// </summary>
    public static RampTextCheck Check(string glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        return CheckGlyphs([.. glyphs.EnumerateRunes()]);
    }

    private static RampTextCheck CheckGlyphs(Rune[] glyphs)
    {
        if (glyphs.Length == 0)
            return new RampTextCheck(RampTextProblem.Empty, default);

        var seen = new HashSet<Rune>();
        foreach (var glyph in glyphs)
        {
            if (glyph == _space)
                return new RampTextCheck(RampTextProblem.Space, glyph);

            if (!CharacterSet.Unicode.Allows(glyph))
                return new RampTextCheck(RampTextProblem.Unusable, glyph);

            if (!seen.Add(glyph))
                return new RampTextCheck(RampTextProblem.Repeated, glyph);
        }

        return new RampTextCheck(RampTextProblem.None, default);
    }

    /// <summary>
    /// The level nearest <paramref name="intensity"/>, which is clamped to 0 to 1 first. Halfway between two levels
    /// goes to the darker one.
    /// </summary>
    public int LevelAt(double intensity)
    {
        if (double.IsNaN(intensity))
            throw new ArgumentOutOfRangeException(nameof(intensity), intensity, null);

        return (int)Math.Round(Math.Clamp(intensity, 0, 1) * TopLevel, MidpointRounding.AwayFromZero);
    }

    /// <summary>The intensity <paramref name="level"/> stands for: 0 for level 0 and 1 for the top level.</summary>
    public double IntensityOf(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, TopLevel);

        return (double)level / TopLevel;
    }

    /// <summary>The glyph <paramref name="level"/> paints, or null for an empty cell.</summary>
    public Rune? GlyphOf(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, TopLevel);

        if (level > 0)
            return _glyphs[level - 1];

        return ZeroLevel == RampZeroLevel.Space ? _space : null;
    }

    /// <summary>The glyph <paramref name="intensity"/> paints, or null for an empty cell.</summary>
    public Rune? GlyphAt(double intensity) => GlyphOf(LevelAt(intensity));

    /// <summary>
    /// The level that paints <paramref name="glyph"/>, or null when the ramp doesn't hold it. A space is level 0 in a
    /// ramp whose zero level is a space.
    /// </summary>
    public int? LevelOf(Rune glyph)
    {
        if (glyph == _space)
            return ZeroLevel == RampZeroLevel.Space ? 0 : null;

        var index = Array.IndexOf(_glyphs, glyph);
        return index >= 0 ? index + 1 : null;
    }

    /// <summary>
    /// How dark <paramref name="glyph"/> counts as, as a level of this ramp (ADR 0049): its own level if the ramp holds
    /// it, 0 for a space, and otherwise the level whose glyph measures nearest it in <paramref name="densities"/>,
    /// with level 0 measuring as no ink. A glyph halfway between two levels counts as the lighter.
    /// </summary>
    public int NearestLevel(Rune glyph, IGlyphDensityProvider densities)
    {
        ArgumentNullException.ThrowIfNull(densities);

        if (glyph == _space)
            return 0;

        if (LevelOf(glyph) is { } level)
            return level;

        var density = densities.GetDensity(glyph);
        var nearest = 0;
        var nearestGap = density;
        for (var candidate = 1; candidate <= TopLevel; candidate++)
        {
            var gap = Math.Abs(densities.GetDensity(_glyphs[candidate - 1]) - density);
            if (gap < nearestGap)
            {
                nearest = candidate;
                nearestGap = gap;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Whether a document using <paramref name="characterSet"/> can hold every glyph this ramp paints.
    /// </summary>
    public bool FitsIn(CharacterSet characterSet) => _glyphs.All(glyph => characterSet.Allows(glyph));

    /// <summary>
    /// The same glyphs ordered from lightest to darkest by <paramref name="densities"/>. Glyphs that measure the same
    /// keep their order.
    /// </summary>
    public Ramp SortedByDensity(IGlyphDensityProvider densities)
    {
        ArgumentNullException.ThrowIfNull(densities);
        return new Ramp(_glyphs.OrderBy(densities.GetDensity), ZeroLevel);
    }

    /// <inheritdoc/>
    public bool Equals(Ramp? other) =>
        other is not null && ZeroLevel == other.ZeroLevel && _glyphs.AsSpan().SequenceEqual(other._glyphs);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Ramp);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ZeroLevel);
        foreach (var glyph in _glyphs)
            hash.Add(glyph);

        return hash.ToHashCode();
    }

    /// <summary>The glyphs as text, lightest first.</summary>
    public override string ToString() => string.Concat(_glyphs.Select(glyph => glyph.ToString()));
}
