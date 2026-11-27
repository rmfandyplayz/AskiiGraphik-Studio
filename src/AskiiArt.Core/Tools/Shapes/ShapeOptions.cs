// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Shapes;

/// <summary>What fills a rectangle or an ellipse inside its outline (ADR 0080).</summary>
public enum ShapeFill
{
    /// <summary>Nothing: only the outline is drawn.</summary>
    None,

    /// <summary>The Character Brush's character, in the current colours.</summary>
    Character,

    /// <summary>The background colour, which leaves the cells' glyphs as they are.</summary>
    Background
}

/// <summary>
/// The rectangle's and ellipse's own options (ADR 0080, ADR 0082): Fill, which the two share, and the rectangle's
/// Style, with the box-drawing styles, and Rounded corners. The ellipse has the shared Style
/// (<see cref="LineStyleOptions"/>), and both have the Character Brush's character.
/// </summary>
public static class ShapeOptions
{
    private const string NoFill = "none";
    private const string CharacterFill = "character";
    private const string BackgroundFill = "background";

    /// <summary>What fills the shape, nothing by default.</summary>
    public static ChoiceToolOption Fill { get; } = new("fill", "ToolOptionShapeFill",
        [
            new(NoFill, "ToolOptionShapeFillNone"),
            new(CharacterFill, "ToolOptionShapeFillCharacter"),
            new(BackgroundFill, "ToolOptionShapeFillBackground")
        ],
        NoFill);

    /// <summary>The group holding just the fill, shared by the rectangle and the ellipse.</summary>
    public static ToolOptionGroup FillGroup { get; } = new("askiiart.shapes.fill", [Fill]);

    /// <summary>The rectangle's own style: every style, the box-drawing ones included, Standard by default.</summary>
    public static ChoiceToolOption RectangleStyle { get; } = LineStyleOptions.CreateStyle(
    [
        LineStyle.Standard, LineStyle.StandardByStep, LineStyle.Precise, LineStyle.Box, LineStyle.HeavyBox,
        LineStyle.DoubleBox, LineStyle.SingleCharacter
    ]);

    /// <summary>The group holding the rectangle's style.</summary>
    public static ToolOptionGroup RectangleStyleGroup { get; } =
        new("askiiart.shapes.rectangle.style", [RectangleStyle]);

    /// <summary>Whether a rectangle's corners are rounded, off by default.</summary>
    public static ToggleToolOption RoundedCorners { get; } = new("roundedCorners", "ToolOptionRoundedCorners", false);

    /// <summary>The group holding the rectangle's Rounded corners.</summary>
    public static ToolOptionGroup RectangleGroup { get; } = new("askiiart.shapes.rectangle", [RoundedCorners]);

    /// <summary>The group holding the style <paramref name="shape"/> is drawn in.</summary>
    public static ToolOptionGroup StyleGroupOf(BoxShape shape) =>
        shape == BoxShape.Rectangle ? RectangleStyleGroup : LineStyleOptions.Group;

    /// <summary>The fill <paramref name="values"/> hold.</summary>
    public static ShapeFill ReadFill(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Get(Fill) switch
        {
            CharacterFill => ShapeFill.Character,
            BackgroundFill => ShapeFill.Background,
            _ => ShapeFill.None
        };
    }

    /// <summary>
    /// Makes sure a character chosen in the Characters window is drawn by <paramref name="shape"/>: when neither its
    /// style nor the fill uses the Character Brush's character, its style becomes Single character.
    /// </summary>
    public static void ChooseCharacter(BoxShape shape, ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!UsesTheCharacter(shape, settings))
            LineStyleOptions.ChooseSingleCharacter(settings.For(StyleGroupOf(shape)));
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="shape"/> in <paramref name="document"/>: the
    /// Character Brush's while the shape uses its character and the document can't hold it, otherwise the
    /// box-drawing styles' while they're drawn in Standard, otherwise the BG Eraser's while the fill is the background
    /// and the document has no colours, otherwise null.
    /// </summary>
    public static string? WarningFor(BoxShape shape, ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(document);
        if (UsesTheCharacter(shape, settings) && CharacterBrushOptions.WarningFor(settings, document) is { } warning)
            return warning;

        var style = LineStyleOptions.ReadStyle(settings.For(StyleGroupOf(shape)));
        if (LineStyleOptions.BoxStyleWarningFor(style, document) is { } boxWarning)
            return boxWarning;

        if (ReadFill(settings.For(FillGroup)) == ShapeFill.Background && document.ColorMode == ColorMode.Monochrome)
            return EraserTool.NoBackgroundsWarningKey;

        return null;
    }

    // Whether the outline or the fill draws the Character Brush's character.
    private static bool UsesTheCharacter(BoxShape shape, ToolSettings settings) =>
        LineStyleOptions.ReadStyle(settings.For(StyleGroupOf(shape))) == LineStyle.SingleCharacter
        || ReadFill(settings.For(FillGroup)) == ShapeFill.Character;
}
