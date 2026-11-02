// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Settings;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Settings kept in memory, so tests never touch the user's settings file.</summary>
internal sealed class FakeAppSettings : IAppSettings
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public bool HasDamageNotice { get; set; }

    public string? GetText(string key) => _values.GetValueOrDefault(key);

    public void SetText(string key, string value) => _values[key] = value;

    public bool TakeDamageNotice()
    {
        var hadNotice = HasDamageNotice;
        HasDamageNotice = false;
        return hadNotice;
    }
}
