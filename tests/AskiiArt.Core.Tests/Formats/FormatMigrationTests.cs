// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using System.Text.Json.Nodes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;

namespace AskiiArt.Core.Tests.Formats;

public sealed class FormatMigrationTests
{
    [Fact]
    public void EveryOlderVersion_HasExactlyOneMigration()
    {
        // Fails when CurrentVersion is bumped without a migration from the version before it.
        for (var version = 1; version < DocumentFileFormat.CurrentVersion; version++)
            Assert.Single(FormatMigrations.All, migration => migration.FromVersion == version);

        Assert.DoesNotContain(FormatMigrations.All,
            migration => migration.FromVersion >= DocumentFileFormat.CurrentVersion);
    }

    [Fact]
    public void Decode_OlderVersion_RunsTheMigrationsBeforeReading()
    {
        // Pretend version 1 called the font "typeface" and version 2 renamed it.
        var current = DocumentJson.Parse(DocumentJson.Write(TestCells.CreateDocument(2, 1)));
        current["typeface"] = "Fira Code";
        current.Remove("font");
        var file = Encoding.UTF8.GetBytes("ASKIIART 1\n" + current.ToJsonString());

        var contents = DocumentFileFormat.Decode(file, currentVersion: 2, [new RenameTypefaceToFont()]);

        Assert.Equal(new FontReference("Fira Code"), contents.Document.Font);
    }

    [Fact]
    public void Apply_WithAMissingMigration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FormatMigrations.Apply(new JsonObject(), 1, 3,
            [new RenameTypefaceToFont()]));
    }

    private sealed class RenameTypefaceToFont : IFormatMigration
    {
        public int FromVersion => 1;

        public void Migrate(JsonObject root)
        {
            root["font"] = root["typeface"]?.DeepClone();
            root.Remove("typeface");
        }
    }
}
