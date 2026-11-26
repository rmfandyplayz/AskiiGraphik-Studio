// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// How a line drawn with Include joins on meets the line characters already in its cells (ADR 0081). A line
/// character is <c>-</c>, <c>|</c>, <c>+</c> or a box-drawing character made of arms from the middle of its cell to
/// its sides; dashed and diagonal box-drawing characters aren't. Where a drawn line character goes over one, the two
/// join: the cell gets the drawn line's arms at their own weights and the other arms the old character had, in the
/// drawn character's kind, so <c>-</c> and <c>|</c> always join as <c>+</c>.
/// </summary>
internal static class LineJoins
{
    // Every box-drawing character made of arms, from U+2500 to U+257F, with its arms up, right, down and left: 0 none,
    // 1 light, 2 heavy, 3 double. Taken from the characters' Unicode names. The rounded corners come after the square
    // ones, so joining never makes a rounded corner.
    private static readonly string[] _boxRows =
    [
        "─0101 ━0202 │1010 ┃2020 ┌0110 ┍0210 ┎0120 ┏0220",
        "┐0011 ┑0012 ┒0021 ┓0022 └1100 ┕1200 ┖2100 ┗2200",
        "┘1001 ┙1002 ┚2001 ┛2002 ├1110 ┝1210 ┞2110 ┟1120",
        "┠2120 ┡2210 ┢1220 ┣2220 ┤1011 ┥1012 ┦2011 ┧1021",
        "┨2021 ┩2012 ┪1022 ┫2022 ┬0111 ┭0112 ┮0211 ┯0212",
        "┰0121 ┱0122 ┲0221 ┳0222 ┴1101 ┵1102 ┶1201 ┷1202",
        "┸2101 ┹2102 ┺2201 ┻2202 ┼1111 ┽1112 ┾1211 ┿1212",
        "╀2111 ╁1121 ╂2121 ╃2112 ╄2211 ╅1122 ╆1221 ╇2212",
        "╈1222 ╉2122 ╊2221 ╋2222 ═0303 ║3030 ╒0310 ╓0130",
        "╔0330 ╕0013 ╖0031 ╗0033 ╘1300 ╙3100 ╚3300 ╛1003",
        "╜3001 ╝3003 ╞1310 ╟3130 ╠3330 ╡1013 ╢3031 ╣3033",
        "╤0313 ╥0131 ╦0333 ╧1303 ╨3101 ╩3303 ╪1313 ╫3131",
        "╬3333 ╭0110 ╮0011 ╯1001 ╰1100 ╴0001 ╵1000 ╶0100",
        "╷0010 ╸0002 ╹2000 ╺0200 ╻0020 ╼0201 ╽1020 ╾0102",
        "╿2010"
    ];

    private static readonly Rune _across = new('-');
    private static readonly Rune _upright = new('|');
    private static readonly Rune _cross = new('+');

    private static readonly (Rune Glyph, LineArms Arms)[] _boxCharacters = ReadBoxCharacters();
    private static readonly Dictionary<Rune, LineArms> _armsOf = ArmsOfEveryLineCharacter();
    private static readonly Dictionary<LineArms, Rune> _boxCharacterFor = BoxCharacterForEachArms();

    /// <summary>The arms <paramref name="glyph"/> has, or false when it isn't a line character.</summary>
    public static bool TryGetArms(Rune glyph, out LineArms arms) => _armsOf.TryGetValue(glyph, out arms);

    /// <summary>The box-drawing character with just <paramref name="arms"/>, or null when there's none.</summary>
    public static Rune? BoxCharacterFor(LineArms arms) =>
        _boxCharacterFor.TryGetValue(arms, out var glyph) ? glyph : null;

    /// <summary>
    /// The arms the character at <paramref name="index"/> of <paramref name="line"/> has along it: all its own, except
    /// at an open line's ends, which have none pointing away from the line. None when it isn't a line character.
    /// </summary>
    public static LineArms Along(IReadOnlyList<LineGlyph> line, int index, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(line);
        var (cell, glyph) = line[index];
        if (!TryGetArms(glyph, out var arms))
            return default;

        if (isClosed || line.Count == 1)
            return arms;

        if (index == 0)
            arms = arms.WithoutArmsAwayFrom(line[1].Cell.X - cell.X, line[1].Cell.Y - cell.Y);

        if (index == line.Count - 1)
            arms = arms.WithoutArmsAwayFrom(line[index - 1].Cell.X - cell.X, line[index - 1].Cell.Y - cell.Y);

        return arms;
    }

    /// <summary>
    /// The character a cell gets when a line draws <paramref name="drawn"/> there, with the arms
    /// <paramref name="along"/> the line, over <paramref name="existing"/>. It's <paramref name="drawn"/> itself
    /// unless both are line characters and joining them adds an arm. Joined box-drawing arms that no character has
    /// together all take the drawn line's weight, or failing that are all light, which every set of arms has.
    /// </summary>
    public static Rune Join(Rune drawn, LineArms along, Rune existing)
    {
        if (!TryGetArms(drawn, out var own) || !TryGetArms(existing, out var under))
            return drawn;

        var joined = along.Over(under);
        if (joined == own)
            return drawn;

        if (drawn.Value is '-' or '|' or '+')
            return AsciiFor(joined);

        return BoxCharacterFor(joined)
               ?? BoxCharacterFor(joined.At(own.Heaviest))
               ?? BoxCharacterFor(joined.At(LineWeight.Light))
               ?? drawn;
    }

    private static Rune AsciiFor(LineArms arms)
    {
        if (arms.HasAcross && arms.HasUpright)
            return _cross;

        return arms.HasAcross ? _across : _upright;
    }

    private static (Rune Glyph, LineArms Arms)[] ReadBoxCharacters() =>
    [
        .. _boxRows.SelectMany(row => row.Split(' ')).Select(entry =>
            (new Rune(entry[0]),
                new LineArms(WeightAt(entry, 1), WeightAt(entry, 2), WeightAt(entry, 3), WeightAt(entry, 4))))
    ];

    private static Dictionary<Rune, LineArms> ArmsOfEveryLineCharacter()
    {
        var armsOf = _boxCharacters.ToDictionary(entry => entry.Glyph, entry => entry.Arms);
        armsOf.Add(_across, new LineArms(LineWeight.None, LineWeight.Light, LineWeight.None, LineWeight.Light));
        armsOf.Add(_upright, new LineArms(LineWeight.Light, LineWeight.None, LineWeight.Light, LineWeight.None));
        armsOf.Add(_cross, new LineArms(LineWeight.Light, LineWeight.Light, LineWeight.Light, LineWeight.Light));
        return armsOf;
    }

    private static LineWeight WeightAt(string entry, int index) => (LineWeight)(entry[index] - '0');

    // The first box-drawing character with each set of arms, so a square corner rather than a rounded one.
    private static Dictionary<LineArms, Rune> BoxCharacterForEachArms()
    {
        var characterFor = new Dictionary<LineArms, Rune>();
        foreach (var (glyph, arms) in _boxCharacters)
            characterFor.TryAdd(arms, glyph);

        return characterFor;
    }
}
