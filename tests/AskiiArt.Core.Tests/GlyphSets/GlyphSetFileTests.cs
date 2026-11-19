// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Tests.GlyphSets;

public sealed class GlyphSetFileTests
{
    private const string Arrows = "←↑→↓";

    [Fact]
    public void AWrittenSet_ReadsBackTheSame()
    {
        var glyphs = GlyphSet.FromText("░▒▓█\U0001F642");

        var read = GlyphSetFile.TryRead(GlyphSetFile.Write("  Shades ", glyphs), out var named, out var problem);

        Assert.True(read);
        Assert.Equal(LibraryFileProblem.None, problem);
        Assert.Equal(new NamedGlyphSet("Shades", glyphs), named);
    }

    [Fact]
    public void TheFile_IsReadableJson_WithTheCharactersAsTheyAre()
    {
        var text = GlyphSetFile.Write("Arrows", GlyphSet.FromText(Arrows));

        Assert.Equal(
            "{\n  \"format\": \"askiiart.charset\",\n  \"version\": 1,\n  \"name\": \"Arrows\",\n"
            + "  \"characters\": \"" + Arrows + "\"\n}\n",
            text);
        Assert.Equal(".asciicharset", GlyphSetFile.Extension);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("{\"format\": \"askiiart.ramp\", \"version\": 1, \"name\": \"A\", \"characters\": \"ab\"}")]
    public void SomethingElse_IsNotACharacterSetFile(string text)
    {
        Assert.False(GlyphSetFile.TryRead(text, out _, out var problem));
        Assert.Equal(LibraryFileProblem.WrongKind, problem);
    }

    [Fact]
    public void ANewerVersion_IsRefused()
    {
        var text = "{\"format\": \"askiiart.charset\", \"version\": 2, \"name\": \"A\", \"characters\": \"ab\"}";

        Assert.False(GlyphSetFile.TryRead(text, out _, out var problem));
        Assert.Equal(LibraryFileProblem.NewerVersion, problem);
    }

    [Theory]
    [InlineData("\"version\": 0, \"name\": \"A\", \"characters\": \"ab\"")]
    [InlineData("\"version\": 1, \"name\": \" \", \"characters\": \"ab\"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"characters\": \"  \"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"characters\": \"a\\u0007\"")]
    [InlineData("\"version\": 1, \"name\": \"A\"")]
    [InlineData("\"version\": 1, \"characters\": \"ab\"")]
    public void AFileThatCantBeRead_IsDamaged(string fields)
    {
        var text = "{\"format\": \"askiiart.charset\", " + fields + "}";

        Assert.False(GlyphSetFile.TryRead(text, out _, out var problem));
        Assert.Equal(LibraryFileProblem.Damaged, problem);
    }

    [Fact]
    public void SpacesAndRepeatsInTheCharacters_AreSkipped_AsWhenTyped()
    {
        var text = "{\"format\": \"askiiart.charset\", \"version\": 1, \"name\": \"A\", \"characters\": \"a b a\"}";

        Assert.True(GlyphSetFile.TryRead(text, out var named, out _));
        Assert.Equal("ab", named.Glyphs.ToString());
    }

    [Fact]
    public void SavedSets_ReadBackTheSame()
    {
        UserGlyphSet[] sets =
        [
            new("user.a", "Arrows", GlyphSet.FromText(Arrows)),
            new("user.b", "Faces", GlyphSet.FromText("\U0001F642\U0001F643")),
        ];

        Assert.Equal(sets, UserGlyphSetsJson.Read(UserGlyphSetsJson.Write(sets)));
    }

    [Fact]
    public void ASavedSetThatCantBeRead_IsLeftOut_AndTheRestKept()
    {
        var text = "[{\"id\": \"user.a\", \"name\": \"A\", \"characters\": \" \"},"
            + " 5, {\"name\": \"No id\", \"characters\": \"ab\"},"
            + " {\"id\": \"user.b\", \"name\": \"B\", \"characters\": \"ab\"}]";

        Assert.Equal([new UserGlyphSet("user.b", "B", GlyphSet.FromText("ab"))], UserGlyphSetsJson.Read(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[")]
    public void TextThatIsntAnArray_HoldsNoSets(string text)
    {
        Assert.Empty(UserGlyphSetsJson.Read(text));
    }
}
