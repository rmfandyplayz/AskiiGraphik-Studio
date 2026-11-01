// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Documents;
using AskiiArt.Core.Formats;

namespace AskiiArt.App.Tests.Documents;

public sealed class DocumentFileTests
{
    [Theory]
    [InlineData("art.asciiart", true, "art.asciiart", false)]
    [InlineData("art.ASCIIARTZ", false, "art.ASCIIARTZ", true)]
    [InlineData("art", true, "art.asciiartz", true)]
    [InlineData("art", false, "art.asciiart", false)]
    [InlineData("art.txt", true, "art.txt", true)]
    public void ForSavePath_TheNameWinsOverTheChosenType_AndAMissingExtensionIsAdded(string path,
        bool choseCompressed, string expectedPath, bool expectCompressed)
    {
        var file = DocumentFile.ForSavePath(path, Variant(choseCompressed));

        Assert.Equal(new DocumentFile(expectedPath, Variant(expectCompressed)), file);
    }

    private static DocumentFileVariant Variant(bool isCompressed) =>
        isCompressed ? DocumentFileVariant.Compressed : DocumentFileVariant.Plain;
}
