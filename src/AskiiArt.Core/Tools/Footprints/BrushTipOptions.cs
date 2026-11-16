// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// The options of a tool that paints with a <see cref="BrushTip"/> (ADR 0047): size, shape and proportions. Each tool
/// group gets its own <see cref="ToolOptionGroup"/> of them, so the Brush and the Eraser keep separate sizes.
/// </summary>
public static class BrushTipOptions
{
    /// <summary>The key that makes the tip one cell smaller.</summary>
    public const string SmallerKey = "[";

    /// <summary>The key that makes the tip one cell bigger.</summary>
    public const string BiggerKey = "]";

    /// <summary>The size, in cells.</summary>
    public static NumberToolOption Size { get; } =
        new("size", "ToolOptionSize", BrushTip.MinimumSize, BrushTip.MaximumSize, BrushTip.MinimumSize);

    /// <summary>Square or circle.</summary>
    public static ChoiceToolOption Shape { get; } = new("shape", "ToolOptionShape",
        [new("square", "ToolOptionShapeSquare"), new("circle", "ToolOptionShapeCircle")], "circle");

    /// <summary>Perceptual or mathematical.</summary>
    public static ChoiceToolOption Proportions { get; } = new("proportions", "ToolOptionProportions",
        [
            new("perceptual", "ToolOptionProportionsPerceptual"),
            new("mathematical", "ToolOptionProportionsMathematical")
        ],
        "perceptual");

    /// <summary>A group of the tip options, with the id <paramref name="groupId"/>.</summary>
    public static ToolOptionGroup CreateGroup(string groupId) => new(groupId, [Size, Shape, Proportions]);

    /// <summary>The tip <paramref name="values"/> describe.</summary>
    public static BrushTip ReadTip(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var shape = values.Get(Shape) == "square" ? BrushShape.Square : BrushShape.Circle;
        var proportions = values.Get(Proportions) == "mathematical"
            ? BrushProportions.Mathematical
            : BrushProportions.Perceptual;

        return new BrushTip(values.Get(Size), shape, proportions);
    }

    /// <summary>
    /// Makes the tip a cell smaller for <see cref="SmallerKey"/> or bigger for <see cref="BiggerKey"/>. They're typed
    /// text rather than keys, so they work with any keyboard layout that types them. Returns false for other text.
    /// </summary>
    public static bool TryUseSizeKey(ToolOptionValues values, string text)
    {
        ArgumentNullException.ThrowIfNull(values);

        var step = text switch
        {
            SmallerKey => -1,
            BiggerKey => 1,
            _ => 0
        };

        if (step == 0)
            return false;

        values.Set(Size, values.Get(Size) + step);
        return true;
    }
}
