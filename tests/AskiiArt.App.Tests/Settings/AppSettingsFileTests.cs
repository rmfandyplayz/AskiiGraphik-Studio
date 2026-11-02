// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Settings;

public sealed class AppSettingsFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "askiiart-settings-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void WithNoFile_ThereAreNoValues_AndNothingToReport()
    {
        var settings = CreateSettings();

        Assert.Null(settings.GetText("anything"));
        Assert.False(settings.TakeDamageNotice());
    }

    [Fact]
    public void AValue_SurvivesReopening()
    {
        CreateSettings().SetText("key", "value");

        Assert.Equal("value", CreateSettings().GetText("key"));
    }

    [Fact]
    public void Saving_WritesAVersion_AndOnlyLinefeeds()
    {
        CreateSettings().SetText("key", "value");

        var text = File.ReadAllText(SettingsPath);
        Assert.Contains($"\"version\": {AppSettingsFile.CurrentVersion}", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', text);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"key\": \"value\" }")]
    [InlineData("{ \"version\": \"one\" }")]
    [InlineData("{ \"version\": 999 }")]
    public void ADamagedFile_IsSetAside_AndReportedOnce(string contents)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, contents);

        var settings = CreateSettings();

        Assert.Null(settings.GetText("key"));
        Assert.True(settings.TakeDamageNotice());
        Assert.False(settings.TakeDamageNotice());
        Assert.False(File.Exists(SettingsPath));
        var keptCopy = Assert.Single(Directory.GetFiles(_folder, "settings.json.damaged-*"));
        Assert.Equal(contents, File.ReadAllText(keptCopy));
    }

    [Fact]
    public void AfterDamage_SettingsCanBeSavedAgain()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, "{ not json");

        CreateSettings().SetText("key", "value");

        var reopened = CreateSettings();
        Assert.Equal("value", reopened.GetText("key"));
        Assert.False(reopened.TakeDamageNotice());
    }

    private AppSettingsFile CreateSettings() => new(SettingsPath, NullLogger<AppSettingsFile>.Instance);
}
