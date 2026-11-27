// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// The Shapes group's line options (ADR 0079, ADR 0082): the Style lines and shape outlines choose their characters
/// in, and whether they join the lines already there. The Line, Ellipse and Curve tools share one Style, so a style
/// chosen for one carries to the others; the Rectangle has a Style of its own with the box-drawing styles too
/// (<see cref="Shapes.ShapeOptions"/>). The tools that join share Include joins.
/// </summary>
public static class LineStyleOptions
{
    /// <summary>
    /// The resource key of the warning shown while a box-drawing style is chosen in a document that can't hold its
    /// characters, which then draws in Standard.
    /// </summary>
    public const string BoxNeedsUnicodeWarningKey = "ToolWarningBoxLinesNeedUnicode";

    // Every Style option has this id, so a tool's style reads the same from whichever group holds it.
    private const string StyleId = "style";

    // Any box-drawing character: a document that can hold one can hold them all.
    private static readonly Rune _boxCharacter = new('─');

    private static readonly Dictionary<LineStyle, ToolOptionChoice> _choices = new()
    {
        [LineStyle.Standard] = new("standard", "ToolOptionLineStyleStandard"),
        [LineStyle.StandardByStep] = new("standardByStep", "ToolOptionLineStyleStandardByStep"),
        [LineStyle.Precise] = new("precise", "ToolOptionLineStylePrecise"),
        [LineStyle.Box] = new("box", "ToolOptionLineStyleBox"),
        [LineStyle.HeavyBox] = new("heavyBox", "ToolOptionLineStyleHeavyBox"),
        [LineStyle.DoubleBox] = new("doubleBox", "ToolOptionLineStyleDoubleBox"),
        [LineStyle.SingleCharacter] = new("singleCharacter", "ToolOptionLineStyleSingleCharacter")
    };

    /// <summary>The shared style, Standard by default: every style but the box-drawing ones.</summary>
    public static ChoiceToolOption Style { get; } = CreateStyle(
        [LineStyle.Standard, LineStyle.StandardByStep, LineStyle.Precise, LineStyle.SingleCharacter]);

    /// <summary>The group holding the shared style.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.shapes", [Style]);

    /// <summary>
    /// Whether lines join the line characters already in their cells, as <c>┬ ┼ ╋</c> or <c>+</c>, off by default.
    /// </summary>
    public static ToggleToolOption IncludeJoins { get; } = new("includeJoins", "ToolOptionIncludeJoins", false);

    /// <summary>The group holding Include joins, shared by the tools that join.</summary>
    public static ToolOptionGroup JoinsGroup { get; } = new("askiiart.shapes.joins", [IncludeJoins]);

    /// <summary>A Style option offering <paramref name="styles"/>, in that order, with Standard the default.</summary>
    public static ChoiceToolOption CreateStyle(IEnumerable<LineStyle> styles)
    {
        ArgumentNullException.ThrowIfNull(styles);
        return new ChoiceToolOption(StyleId, "ToolOptionLineStyle", [.. styles.Select(style => _choices[style])],
            _choices[LineStyle.Standard].Value);
    }

    /// <summary>The style <paramref name="values"/> hold, from whichever Style option their group has.</summary>
    public static LineStyle ReadStyle(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var chosen = values.Get(StyleOf(values));
        return _choices.First(entry => entry.Value.Value == chosen).Key;
    }

    /// <summary>
    /// Whether lines in <paramref name="style"/> join the line characters already there, as Include joins in
    /// <paramref name="values"/>, those of <see cref="JoinsGroup"/>, says. A Single character line never does.
    /// </summary>
    public static bool ReadJoins(ToolOptionValues values, LineStyle style)
    {
        ArgumentNullException.ThrowIfNull(values);
        return style != LineStyle.SingleCharacter && values.Get(IncludeJoins);
    }

    /// <summary>Makes Single character the shared style, as choosing a character in the Characters window does.</summary>
    public static void ChooseSingleCharacter(ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ChooseSingleCharacter(settings.For(Group));
    }

    /// <summary>Makes Single character the style <paramref name="values"/> hold.</summary>
    public static void ChooseSingleCharacter(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        values.Set(StyleOf(values), _choices[LineStyle.SingleCharacter].Value);
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/> with the shared style: the Character
    /// Brush's while the style is Single character and the document can't hold the character, otherwise null.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ReadStyle(settings.For(Group)) != LineStyle.SingleCharacter)
            return null;

        return CharacterBrushOptions.WarningFor(settings, document);
    }

    /// <summary>
    /// <see cref="BoxNeedsUnicodeWarningKey"/> while <paramref name="style"/> draws box-drawing characters and
    /// <paramref name="document"/> can't hold them, otherwise null.
    /// </summary>
    public static string? BoxStyleWarningFor(LineStyle style, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (style.IsBox() && !CanHoldBoxCharacters(document.CharacterSet))
            return BoxNeedsUnicodeWarningKey;

        return null;
    }

    /// <summary>Whether a document using <paramref name="characterSet"/> can hold box-drawing characters.</summary>
    public static bool CanHoldBoxCharacters(CharacterSet characterSet) => characterSet.Allows(_boxCharacter);

    private static ChoiceToolOption StyleOf(ToolOptionValues values) =>
        values.Group.Options.OfType<ChoiceToolOption>().Single(option => option.Id == StyleId);
}
