// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Hosting;

namespace AskiiArt.App.Tests.Hosting;

public sealed class AppVersionTests
{
    [Theory]
    [InlineData("0.1.0+fc89da6e1234567890abcdef", "0.1.0 (fc89da6)")]
    [InlineData("0.1.0+abc", "0.1.0 (abc)")]
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("1.2.3-beta.1+0123456789", "1.2.3-beta.1 (0123456)")]
    public void FormatForDisplay_ShortensTheCommitHash(string informationalVersion, string expected)
    {
        Assert.Equal(expected, AppVersion.FormatForDisplay(informationalVersion));
    }

    [Fact]
    public void Informational_StartsWithTheProjectVersion()
    {
        Assert.StartsWith("0.1.0", AppVersion.Informational, StringComparison.Ordinal);
    }
}
