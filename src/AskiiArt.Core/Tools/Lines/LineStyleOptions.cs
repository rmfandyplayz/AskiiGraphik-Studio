// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// The Shapes group's Style option (ADR 0079): how lines and shape outlines choose their characters. The shape tools
/// share it, so a style chosen for lines carries to the others.
/// </summary>
public static class LineStyleOptions
{
    private const string Standard = "standard";
    private const string StandardByStep = "standardByStep";
    private const string Precise = "precise";
    private const string SingleCharacter = "singleCharacter";

    /// <summary>The style, Standard by default.</summary>
    public static ChoiceToolOption Style { get; } = new("style", "ToolOptionLineStyle",
        [
            new(Standard, "ToolOptionLineStyleStandard"),
            new(StandardByStep, "ToolOptionLineStyleStandardByStep"),
            new(Precise, "ToolOptionLineStylePrecise"),
            new(SingleCharacter, "ToolOptionLineStyleSingleCharacter")
        ],
        Standard);

    /// <summary>The group holding just the style, shared by the shape tools.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.shapes", [Style]);

    /// <summary>The style <paramref name="values"/> hold.</summary>
    public static LineStyle ReadStyle(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Get(Style) switch
        {
            StandardByStep => LineStyle.StandardByStep,
            Precise => LineStyle.Precise,
            SingleCharacter => LineStyle.SingleCharacter,
            _ => LineStyle.Standard
        };
    }

    /// <summary>Makes Single character the style, as choosing a character in the Characters window does.</summary>
    public static void ChooseSingleCharacter(ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.For(Group).Set(Style, SingleCharacter);
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/>: the Character Brush's while the
    /// style is Single character and the document can't hold the character, otherwise null.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (ReadStyle(settings.For(Group)) != LineStyle.SingleCharacter)
            return null;

        return CharacterBrushOptions.WarningFor(settings, document);
    }
}
