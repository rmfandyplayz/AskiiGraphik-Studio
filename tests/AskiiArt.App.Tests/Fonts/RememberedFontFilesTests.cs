// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.App.Tests.Fakes;

namespace AskiiArt.App.Tests.Fonts;

public sealed class RememberedFontFilesTests
{
    private readonly FakeAppSettings _settings = new();

    [Fact]
    public void Load_WithNothingSaved_IsEmpty() => Assert.Empty(new RememberedFontFiles(_settings).Load());

    [Fact]
    public void Add_KeepsFullPathsInOrder_WithoutRepeats()
    {
        var files = new RememberedFontFiles(_settings);
        var first = Path.GetFullPath("first \"font\" é.ttf");
        var second = Path.GetFullPath("second.otf");

        files.Add(first);
        files.Add(second);
        files.Add("first \"font\" é.ttf");

        Assert.Equal([first, second], new RememberedFontFiles(_settings).Load());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"path\": \"a.ttf\"}")]
    [InlineData("\"a.ttf\"")]
    public void Load_WithAValueItCannotRead_IsEmpty(string saved)
    {
        _settings.SetText(RememberedFontFiles.SettingKey, saved);

        Assert.Empty(new RememberedFontFiles(_settings).Load());
    }

    [Fact]
    public void Load_SkipsEntriesThatAreNotText()
    {
        _settings.SetText(RememberedFontFiles.SettingKey, "[1, null, \"a.ttf\", {}]");

        Assert.Equal(["a.ttf"], new RememberedFontFiles(_settings).Load());
    }
}
