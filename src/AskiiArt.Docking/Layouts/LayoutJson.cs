// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskiiArt.Docking.Layouts;

/// <summary>
/// Saves a layout as text. Text with the wrong shape reads as null. Text with the right shape can still hold
/// nonsense, which is <see cref="LayoutRepair"/>'s job. A field added later needs a default value, or layouts
/// saved before it stop being readable.
/// </summary>
public static class LayoutJson
{
    /// <summary>The layout as text.</summary>
    public static string Write(DockingLayout layout) =>
        JsonSerializer.Serialize(layout, LayoutJsonContext.Default.DockingLayout);

    /// <summary>The layout in <paramref name="text"/>, or null when it can't be read.</summary>
    public static DockingLayout? TryRead(string text)
    {
        try
        {
            return JsonSerializer.Deserialize(text, LayoutJsonContext.Default.DockingLayout);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(DockingLayout))]
internal sealed partial class LayoutJsonContext : JsonSerializerContext;
