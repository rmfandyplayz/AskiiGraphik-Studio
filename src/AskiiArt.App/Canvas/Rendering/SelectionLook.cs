// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// How the selection's hatching, Subtract's and Intersect's colours, and a tool's line and markers look (ADR 0061),
/// from the theme tokens. Lengths are in logical pixels until <see cref="ScaledBy"/> makes them device pixels. The
/// default look draws no hatching, no faint lines and no markers.
/// </summary>
/// <param name="HatchLine">The darker of the hatching's two lines over cells that aren't selected.</param>
/// <param name="HatchBackdrop">The lighter of the hatching's two lines, so it shows on any art.</param>
/// <param name="SubtractLine">Subtract's outline dashes, and its hatching.</param>
/// <param name="SubtractBackdrop">Between Subtract's dashes, and beside its hatching's lines.</param>
/// <param name="IntersectLine">Intersect's outline dashes, and its hatching.</param>
/// <param name="IntersectBackdrop">Between Intersect's dashes, and beside its hatching's lines.</param>
/// <param name="HatchPeriod">How far apart the hatching's lines are, along a row.</param>
/// <param name="HatchThickness">How thick each of the hatching's lines is.</param>
/// <param name="FaintOpacity">How strongly a faint outline shows, from 0 to 1.</param>
/// <param name="LostHatchOpacity">How strongly the hatching over cells about to be deselected shows.</param>
/// <param name="ClosingLineOpacity">How strongly the line showing how a lasso would close shows.</param>
/// <param name="CornerMarkerRadius">The radius of the dot on each corner of a shape being drawn.</param>
/// <param name="FinishMarkerRadius">The radius of the marker that finishes a shape when clicked.</param>
/// <param name="CornerMarkerOpacity">How strongly the corners' dots show.</param>
internal readonly record struct SelectionLook(
    SKColor HatchLine,
    SKColor HatchBackdrop,
    SKColor SubtractLine,
    SKColor SubtractBackdrop,
    SKColor IntersectLine,
    SKColor IntersectBackdrop,
    double HatchPeriod,
    double HatchThickness,
    double FaintOpacity,
    double LostHatchOpacity,
    double ClosingLineOpacity,
    double CornerMarkerRadius,
    double FinishMarkerRadius,
    double CornerMarkerOpacity)
{
    /// <summary>The look with its lengths in device pixels, <paramref name="scaling"/> to a logical pixel.</summary>
    public SelectionLook ScaledBy(double scaling) => this with
    {
        HatchPeriod = HatchPeriod * scaling,
        HatchThickness = HatchThickness * scaling,
        CornerMarkerRadius = CornerMarkerRadius * scaling,
        FinishMarkerRadius = FinishMarkerRadius * scaling
    };

    /// <summary>The dashes' and the backdrop's colours for <paramref name="style"/>.</summary>
    public (SKColor Line, SKColor Backdrop) ColoursOf(SelectionLineStyle style, CanvasColors colors) => style switch
    {
        SelectionLineStyle.Subtract => (SubtractLine, SubtractBackdrop),
        SelectionLineStyle.Intersect => (IntersectLine, IntersectBackdrop),
        _ => (colors.SelectionLine, colors.SelectionBackdrop)
    };

    /// <summary><paramref name="color"/> showing only as strongly as <paramref name="opacity"/>, from 0 to 1.</summary>
    public static SKColor Faded(SKColor color, double opacity) =>
        color.WithAlpha((byte)Math.Round(color.Alpha * Math.Clamp(opacity, 0, 1)));

    /// <summary>Reads the look from the theme tokens in <paramref name="resources"/>.</summary>
    /// <exception cref="InvalidOperationException">A token is missing.</exception>
    public static SelectionLook FromResources(IResourceNode resources, ThemeVariant? theme) => new(
        Colour(resources, "AppCanvasSelectionHatchBrush", theme),
        Colour(resources, "AppCanvasSelectionHatchBackdropBrush", theme),
        Colour(resources, "AppCanvasSelectionSubtractBrush", theme),
        Colour(resources, "AppCanvasSelectionSubtractBackdropBrush", theme),
        Colour(resources, "AppCanvasSelectionIntersectBrush", theme),
        Colour(resources, "AppCanvasSelectionIntersectBackdropBrush", theme),
        Token(resources, "AppCanvasSelectionHatchPeriod", theme),
        Token(resources, "AppCanvasSelectionHatchThickness", theme),
        Token(resources, "AppCanvasSelectionFaintOpacity", theme),
        Token(resources, "AppCanvasSelectionLostHatchOpacity", theme),
        Token(resources, "AppCanvasSelectionClosingLineOpacity", theme),
        Token(resources, "AppCanvasCornerMarkerRadius", theme),
        Token(resources, "AppCanvasFinishMarkerRadius", theme),
        Token(resources, "AppCanvasCornerMarkerOpacity", theme));

    private static SKColor Colour(IResourceNode resources, string key, ThemeVariant? theme)
    {
        if (!resources.TryGetResource(key, theme, out var value) || value is not ISolidColorBrush brush)
            throw new InvalidOperationException($"The theme token {key} is missing.");

        var color = brush.Color;
        return Faded(new SKColor(color.R, color.G, color.B, color.A), brush.Opacity);
    }

    private static double Token(IResourceNode resources, string key, ThemeVariant? theme)
    {
        if (resources.TryGetResource(key, theme, out var value) && value is double size)
            return size;

        throw new InvalidOperationException($"The theme token {key} is missing.");
    }
}
