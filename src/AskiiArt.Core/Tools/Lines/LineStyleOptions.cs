// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// The Shapes group's shared options (ADR 0079, ADR 0081): the Style lines and shape outlines choose their characters
/// in, and whether they join the lines already there. The shape tools share them, so a style chosen for lines carries
/// to the others.
/// </summary>
public static class LineStyleOptions
{
    /// <summary>
    /// The resource key of the warning shown while a box-drawing style is chosen in a document that can't hold its
    /// characters, which then draws in Standard.
    /// </summary>
    public const string BoxNeedsUnicodeWarningKey = "ToolWarningBoxLinesNeedUnicode";

    private const string Standard = "standard";
    private const string StandardByStep = "standardByStep";
    private const string Precise = "precise";
    private const string Box = "box";
    private const string HeavyBox = "heavyBox";
    private const string DoubleBox = "doubleBox";
    private const string SingleCharacter = "singleCharacter";

    // Any box-drawing character: a document that can hold one can hold them all.
    private static readonly Rune _boxCharacter = new('─');

    /// <summary>The style, Standard by default.</summary>
    public static ChoiceToolOption Style { get; } = new("style", "ToolOptionLineStyle",
        [
            new(Standard, "ToolOptionLineStyleStandard"),
            new(StandardByStep, "ToolOptionLineStyleStandardByStep"),
            new(Precise, "ToolOptionLineStylePrecise"),
            new(Box, "ToolOptionLineStyleBox"),
            new(HeavyBox, "ToolOptionLineStyleHeavyBox"),
            new(DoubleBox, "ToolOptionLineStyleDoubleBox"),
            new(SingleCharacter, "ToolOptionLineStyleSingleCharacter")
        ],
        Standard);

    /// <summary>
    /// Whether lines join the line characters already in their cells, as <c>┬ ┼ ╋</c> or <c>+</c>, off by default.
    /// </summary>
    public static ToggleToolOption IncludeJoins { get; } = new("includeJoins", "ToolOptionIncludeJoins", false);

    /// <summary>The group holding the style and Include joins, shared by the shape tools.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.shapes", [Style, IncludeJoins]);

    /// <summary>The style <paramref name="values"/> hold.</summary>
    public static LineStyle ReadStyle(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Get(Style) switch
        {
            StandardByStep => LineStyle.StandardByStep,
            Precise => LineStyle.Precise,
            Box => LineStyle.Box,
            HeavyBox => LineStyle.HeavyBox,
            DoubleBox => LineStyle.DoubleBox,
            SingleCharacter => LineStyle.SingleCharacter,
            _ => LineStyle.Standard
        };
    }

    /// <summary>
    /// Whether lines in <paramref name="style"/> join the line characters already there, with the values
    /// <paramref name="values"/> hold. A Single character line never does.
    /// </summary>
    public static bool ReadJoins(ToolOptionValues values, LineStyle style)
    {
        ArgumentNullException.ThrowIfNull(values);
        return style != LineStyle.SingleCharacter && values.Get(IncludeJoins);
    }

    /// <summary>Makes Single character the style, as choosing a character in the Characters window does.</summary>
    public static void ChooseSingleCharacter(ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.For(Group).Set(Style, SingleCharacter);
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/>: the Character Brush's while the
    /// style is Single character and the document can't hold the character, otherwise
    /// <see cref="BoxStyleWarningFor"/>'s.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ReadStyle(settings.For(Group)) == LineStyle.SingleCharacter)
            return CharacterBrushOptions.WarningFor(settings, document);

        return BoxStyleWarningFor(settings, document);
    }

    /// <summary>
    /// <see cref="BoxNeedsUnicodeWarningKey"/> while the style draws box-drawing characters and
    /// <paramref name="document"/> can't hold them, otherwise null.
    /// </summary>
    public static string? BoxStyleWarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(document);
        if (ReadStyle(settings.For(Group)).IsBox() && !CanHoldBoxCharacters(document.CharacterSet))
            return BoxNeedsUnicodeWarningKey;

        return null;
    }

    /// <summary>Whether a document using <paramref name="characterSet"/> can hold box-drawing characters.</summary>
    public static bool CanHoldBoxCharacters(CharacterSet characterSet) => characterSet.Allows(_boxCharacter);
}
