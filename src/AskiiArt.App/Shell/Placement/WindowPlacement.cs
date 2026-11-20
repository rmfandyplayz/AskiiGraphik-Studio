// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;

namespace AskiiArt.App.Shell.Placement;

/// <summary>Where a screen has room for windows, in device pixels, and how much it scales them.</summary>
internal readonly record struct ScreenArea(PixelRect WorkingArea, double Scaling, bool IsPrimary);

/// <summary>
/// Where the main window was, kept in settings between runs (ADR 0059): its top-left corner in device pixels and its
/// size in logical pixels when it isn't maximized, and whether it was maximized.
/// </summary>
internal sealed record WindowPlacement(PixelPoint Position, Size Size, bool IsMaximized)
{
    // How far below the window's top edge to look for its title bar, in logical pixels.
    private const double TitleBarDepth = 16;

    private const string XKey = "x";
    private const string YKey = "y";
    private const string WidthKey = "width";
    private const string HeightKey = "height";
    private const string MaximizedKey = "maximized";

    /// <summary>The placement in the form it's saved in.</summary>
    public string ToJson() =>
        new JsonObject
        {
            [XKey] = Position.X,
            [YKey] = Position.Y,
            [WidthKey] = Size.Width,
            [HeightKey] = Size.Height,
            [MaximizedKey] = IsMaximized
        }.ToJsonString();

    /// <summary>Reads a placement saved by <see cref="ToJson"/>, or returns null when it can't.</summary>
    public static WindowPlacement? FromJson(string text)
    {
        try
        {
            if (JsonNode.Parse(text) is not JsonObject json)
                return null;

            var x = json[XKey]?.GetValue<int>();
            var y = json[YKey]?.GetValue<int>();
            var width = json[WidthKey]?.GetValue<double>();
            var height = json[HeightKey]?.GetValue<double>();
            var isMaximized = json[MaximizedKey]?.GetValue<bool>();
            if (x is null || y is null || !IsALength(width) || !IsALength(height) || isMaximized is null)
                return null;

            return new WindowPlacement(new PixelPoint(x.Value, y.Value), new Size(width!.Value, height!.Value),
                isMaximized.Value);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// The placement to open with on <paramref name="screens"/>: this one, no bigger than its screen, when its title
    /// bar is on one of them. Null when it isn't, as when the screen it was on has gone.
    /// </summary>
    public WindowPlacement? OnScreens(IEnumerable<ScreenArea> screens)
    {
        foreach (var screen in screens)
        {
            var titleBar = new PixelPoint(Position.X + (int)(Size.Width * screen.Scaling / 2),
                Position.Y + (int)(TitleBarDepth * screen.Scaling));
            if (screen.WorkingArea.Contains(titleBar))
                return this with { Size = SizeWithin(screen) };
        }

        return null;
    }

    /// <summary>This placement's size, made no bigger than <paramref name="screen"/>'s room for windows.</summary>
    public Size SizeWithin(ScreenArea screen) =>
        new(Math.Min(Size.Width, screen.WorkingArea.Width / screen.Scaling),
            Math.Min(Size.Height, screen.WorkingArea.Height / screen.Scaling));

    private static bool IsALength(double? value) => value is { } length && double.IsFinite(length) && length > 0;
}
