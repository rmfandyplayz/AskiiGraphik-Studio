// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tests.Editing;

public sealed class EditingSessionActiveLayerTests
{
    private readonly EditingSession _session = new(Document.Create(new CanvasSize(4, 2), "First"));

    [Fact]
    public void SetActiveLayer_ChoosesTheLayer_AndSaysSo()
    {
        var first = _session.ActiveLayer.Id;
        var added = AddLayer();
        var raised = 0;
        _session.ActiveLayerChanged += (_, _) => raised++;

        _session.SetActiveLayer(added);
        Assert.Equal(added, _session.ActiveLayer.Id);
        Assert.Equal(1, raised);

        _session.SetActiveLayer(added);
        Assert.Equal(1, raised);

        _session.SetActiveLayer(first);
        Assert.Equal(first, _session.ActiveLayer.Id);
        Assert.Equal(2, raised);
    }

    [Fact]
    public void SetActiveLayer_RejectsALayerTheDocumentDoesntHave()
    {
        Assert.Throws<KeyNotFoundException>(() => _session.SetActiveLayer(Guid.NewGuid()));
    }

    [Fact]
    public void WhenTheActiveLayerIsRemoved_TheTopLayerTakesOver()
    {
        var added = AddLayer();
        _session.SetActiveLayer(added);

        _session.History.Undo();

        Assert.Equal(_session.Document.ActiveFrame.Layers[^1].Id, _session.ActiveLayer.Id);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void CanEditActiveLayer_NeedsItVisibleAndUnlocked(bool isVisible, bool isUnlocked, bool expected)
    {
        var layer = _session.ActiveLayer;
        var properties = layer.Properties with { IsVisible = isVisible, IsLocked = !isUnlocked };
        _session.History.Execute(new SetLayerPropertiesCommand(layer.Id, properties));

        Assert.Equal(expected, _session.CanEditActiveLayer);
    }

    private Guid AddLayer()
    {
        var command = new AddLayerCommand(1, new LayerProperties("Second"));
        _session.History.Execute(command);
        return command.LayerId;
    }
}
