// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.App.Settings;
using AskiiArt.Core.Ramps;

namespace AskiiArt.App.Ramps;

/// <summary>
/// The user's ramps, kept in settings between runs (ARCHITECTURE §15, ADR 0052). They're saved whenever one changes,
/// since they're the user's work.
/// </summary>
internal static class RememberedRamps
{
    internal const string SettingKey = "ramps";

    /// <summary>
    /// A library holding the ramps saved last time, which saves them again whenever they change. A saved ramp that
    /// can't be read is left out.
    /// </summary>
    public static RampLibrary CreateLibrary(IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var saved = settings.GetText(SettingKey) is { } text ? UserRampsJson.Read(text) : [];
        var library = new RampLibrary(BuiltInRamps.All.Select(ramp => UiText.Get(ramp.NameKey)), saved);
        library.Changed += (_, _) => settings.SetText(SettingKey, UserRampsJson.Write(library.UserRamps));
        return library;
    }
}
