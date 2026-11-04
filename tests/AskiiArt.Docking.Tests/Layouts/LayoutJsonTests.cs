// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using CsCheck;
using static AskiiArt.Docking.Tests.TestLayouts;

namespace AskiiArt.Docking.Tests.Layouts;

public sealed class LayoutJsonTests
{
    [Fact]
    public void AnyLayout_SurvivesSavingAndLoading()
    {
        Valid.Sample(layout =>
        {
            var loaded = LayoutJson.TryRead(LayoutJson.Write(layout));

            Assert.NotNull(loaded);
            Assert.Equal(Text(layout), Text(loaded));
        });
    }

    [Fact]
    public void TheSavedText_UsesNames_SoItSurvivesReorderingAnEnum()
    {
        var text = LayoutJson.Write(Widgets(SideEdge.Right, new WindowGroup(["a"])));

        Assert.Contains("\"right\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"widgets\"", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("""{ "sides": [], "toolbars": [] }""")]
    [InlineData("""{ "sides": null, "toolbars": [], "closedWindows": [] }""")]
    [InlineData(
        """{ "sides": [{ "edge": "Sideways", "kind": "Widgets", "columns": [] }], "toolbars": [], "closedWindows": [] }""")]
    public void TextWithTheWrongShape_ReadsAsNothing(string text)
    {
        Assert.Null(LayoutJson.TryRead(text));
    }

    [Fact]
    public void AnEntryWithoutAnOptionalField_GetsItsDefault()
    {
        const string Text = """
                            { "sides": [{ "edge": "Right", "kind": "Widgets", "columns": [{ "width": 200, "groups": [{ "windowIds": ["a"] }] }] }],
                              "toolbars": [{ "toolbarId": "file", "anchor": "TopLeft", "orientation": "Horizontal" }],
                              "closedWindows": [] }
                            """;

        var layout = LayoutJson.TryRead(Text);

        Assert.NotNull(layout);
        Assert.Equal(1, layout.Sides[0].Columns[0].Groups[0].Share);
        Assert.True(layout.Toolbars[0].IsVisible);
    }

    [Fact]
    public void NullsInsideALayoutThatLoads_AreRepairedAway()
    {
        const string Text = """
                            { "sides": [null, { "edge": "Right", "kind": "Widgets", "columns": [null, { "width": 200, "groups": [null, { "windowIds": [null, "w0"] }] }] }],
                              "toolbars": [null],
                              "closedWindows": [null] }
                            """;

        var layout = LayoutJson.TryRead(Text);

        Assert.NotNull(layout);
        Assert.True(LayoutRepair.Repair(layout, Catalog).IsWindowOpen("w0"));
    }
}
