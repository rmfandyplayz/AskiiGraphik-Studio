// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;

namespace AskiiArt.App.Resources;

/// <summary>
/// Looks up strings in Strings.resx by key, for code that only has a key (action labels, menu
/// titles). Code that knows the string at compile time uses the generated Strings class instead.
/// </summary>
internal static class UiText
{
    public static string Get(string key)
    {
        return Strings.ResourceManager.GetString(key, Strings.Culture)
               ?? throw new KeyNotFoundException($"Strings.resx has no string with the key '{key}'.");
    }

    /// <summary>
    /// Removes the underscore that marks a menu label's access key ("_New…" becomes "New…"), for
    /// places that show the label without one, such as toolbar buttons.
    /// </summary>
    public static string WithoutAccessKey(string label) =>
        label.Replace("__", "\0").Replace("_", "").Replace("\0", "_");

    /// <summary>Formats the string with the app's display name as <c>{0}</c>.</summary>
    public static string GetWithAppName(string key)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key), Strings.AppDisplayName);
    }
}
