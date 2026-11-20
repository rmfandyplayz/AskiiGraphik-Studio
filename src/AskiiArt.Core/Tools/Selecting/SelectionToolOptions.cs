// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Selecting;

/// <summary>
/// The option every selection tool shares (ADR 0060): how its shape combines with the selection there is. Holding
/// Shift, Alt or both when a gesture starts picks another mode for that gesture only.
/// </summary>
public static class SelectionToolOptions
{
    private const string Replace = "replace";
    private const string Add = "add";
    private const string Subtract = "subtract";
    private const string Intersect = "intersect";

    /// <summary>Replace, Add, Subtract or Intersect.</summary>
    public static ChoiceToolOption Mode { get; } = new("mode", "ToolOptionSelectionMode",
        [
            new(Replace, "ToolOptionSelectionModeReplace"),
            new(Add, "ToolOptionSelectionModeAdd"),
            new(Subtract, "ToolOptionSelectionModeSubtract"),
            new(Intersect, "ToolOptionSelectionModeIntersect")
        ],
        Replace)
    {
        IsShownAsButtons = true
    };

    /// <summary>The selection tools' options.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.select", [Mode]);

    /// <summary>The mode <paramref name="values"/> choose.</summary>
    public static SelectionCombineMode ReadMode(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Get(Mode) switch
        {
            Add => SelectionCombineMode.Add,
            Subtract => SelectionCombineMode.Subtract,
            Intersect => SelectionCombineMode.Intersect,
            _ => SelectionCombineMode.Replace
        };
    }

    /// <summary>
    /// The mode for a gesture that starts with <paramref name="modifiers"/> held: Shift adds, Alt subtracts, and both
    /// intersect, whatever <paramref name="chosen"/> is. With neither, it's <paramref name="chosen"/>.
    /// </summary>
    public static SelectionCombineMode ModeFor(ToolModifiers modifiers, SelectionCombineMode chosen)
    {
        var shift = modifiers.HasFlag(ToolModifiers.Shift);
        var alt = modifiers.HasFlag(ToolModifiers.Alt);
        if (shift && alt)
            return SelectionCombineMode.Intersect;

        if (shift)
            return SelectionCombineMode.Add;

        return alt ? SelectionCombineMode.Subtract : chosen;
    }
}
