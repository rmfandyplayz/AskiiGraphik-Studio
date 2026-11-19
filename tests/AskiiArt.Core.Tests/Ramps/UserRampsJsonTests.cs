// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class UserRampsJsonTests
{
    [Fact]
    public void SavedRamps_ReadBackTheSame()
    {
        UserRamp[] ramps =
        [
            new("user.a", "Dots", Ramp.FromText(".:")),
            new("user.b", "Braille", Ramp.FromText("⠁⣿", RampZeroLevel.Space)),
        ];

        Assert.Equal(ramps, UserRampsJson.Read(UserRampsJson.Write(ramps)));
    }

    [Fact]
    public void ARampThatCantBeRead_IsLeftOut_AndTheRestKept()
    {
        var text = "[{\"id\": \"user.a\", \"name\": \"A\", \"lightest\": \"empty\", \"characters\": \"aa\"},"
            + " 5, {\"name\": \"No id\", \"lightest\": \"empty\", \"characters\": \"ab\"},"
            + " {\"id\": \"user.b\", \"name\": \"B\", \"lightest\": \"empty\", \"characters\": \"ab\"}]";

        Assert.Equal([new UserRamp("user.b", "B", Ramp.FromText("ab"))], UserRampsJson.Read(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[")]
    public void TextThatIsntAnArray_HoldsNoRamps(string text)
    {
        Assert.Empty(UserRampsJson.Read(text));
    }
}
