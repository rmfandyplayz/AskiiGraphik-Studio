// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Settings;

/// <summary>The user's settings (ARCHITECTURE §15, ADR 0026). Tests replace it with an in-memory one.</summary>
internal interface IAppSettings
{
    /// <summary>The text stored under <paramref name="key"/>, or null when there isn't any.</summary>
    string? GetText(string key);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> and saves. Failing to save is only logged.</summary>
    void SetText(string key, string value);

    /// <summary>
    /// True the first time this is asked after a damaged settings file was set aside, so the user
    /// gets told once.
    /// </summary>
    bool TakeDamageNotice();
}
