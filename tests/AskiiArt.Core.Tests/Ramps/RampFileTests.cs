// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class RampFileTests
{
    [Fact]
    public void AWrittenRamp_ReadsBackTheSame()
    {
        var ramp = Ramp.FromText("░▒▓█", RampZeroLevel.Space);

        var read = RampFile.TryRead(RampFile.Write("  Shades ", ramp), out var named, out var problem);

        Assert.True(read);
        Assert.Equal(RampFileProblem.None, problem);
        Assert.Equal(new NamedRamp("Shades", ramp), named);
    }

    [Fact]
    public void TheFile_IsReadableJson_WithTheCharactersAsTheyAre()
    {
        var text = RampFile.Write("Classic", Ramp.FromText(".:-=+*#%@"));

        Assert.Equal(
            "{\n  \"format\": \"askiiart.ramp\",\n  \"version\": 1,\n  \"name\": \"Classic\",\n"
            + "  \"lightest\": \"empty\",\n  \"characters\": \".:-=+*#%@\"\n}\n",
            text);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("{\"format\": \"askiiart.document\", \"version\": 1}")]
    public void SomethingElse_IsNotARampFile(string text)
    {
        Assert.False(RampFile.TryRead(text, out _, out var problem));
        Assert.Equal(RampFileProblem.NotARampFile, problem);
    }

    [Fact]
    public void ANewerVersion_IsRefused()
    {
        var text = "{\"format\": \"askiiart.ramp\", \"version\": 2, \"name\": \"A\", \"lightest\": \"empty\", "
            + "\"characters\": \"ab\"}";

        Assert.False(RampFile.TryRead(text, out _, out var problem));
        Assert.Equal(RampFileProblem.NewerVersion, problem);
    }

    [Theory]
    [InlineData("\"version\": 0, \"name\": \"A\", \"lightest\": \"empty\", \"characters\": \"ab\"")]
    [InlineData("\"version\": 1, \"name\": \" \", \"lightest\": \"empty\", \"characters\": \"ab\"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"lightest\": \"dark\", \"characters\": \"ab\"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"lightest\": \"empty\", \"characters\": \"a a\"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"lightest\": \"empty\", \"characters\": \"aa\"")]
    [InlineData("\"version\": 1, \"name\": \"A\", \"lightest\": \"empty\"")]
    public void ARampFileWithSomethingWrong_IsDamaged(string properties)
    {
        var text = "{\"format\": \"askiiart.ramp\", " + properties + "}";

        Assert.False(RampFile.TryRead(text, out _, out var problem));
        Assert.Equal(RampFileProblem.Damaged, problem);
    }
}
