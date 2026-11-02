// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Formats;

namespace AskiiArt.Core.Tests.Formats;

public sealed class AtomicFileTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("askiiart-atomic-").FullName;

    private string TargetPath => Path.Combine(_folder, "file.txt");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Write_CreatesTheFile_AndLeavesNoTemporaryFile()
    {
        AtomicFile.Write(TargetPath, "new"u8);

        Assert.Equal("new", File.ReadAllText(TargetPath));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Write_ReplacesAnExistingFile()
    {
        File.WriteAllText(TargetPath, "old");

        AtomicFile.Write(TargetPath, "new"u8);

        Assert.Equal("new", File.ReadAllText(TargetPath));
        Assert.Single(Directory.GetFiles(_folder));
    }

    [Fact]
    public void Write_ToAMissingFolder_FailsAndLeavesNothingBehind()
    {
        var path = Path.Combine(_folder, "missing", "file.txt");

        Assert.Throws<DirectoryNotFoundException>(() => AtomicFile.Write(path, "new"u8));

        Assert.Empty(Directory.GetFileSystemEntries(_folder));
    }
}
