// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

/// <summary>Duplicate and merge down, the layer operations step 2.3 added.</summary>
public sealed class LayerOperationsTests
{
    private readonly Document _document = Document.Create(new CanvasSize(4, 2), "Bottom");
    private readonly CommandHistory _history;

    public LayerOperationsTests()
    {
        _history = new CommandHistory(_document);
    }

    private Layer Bottom => _document.ActiveFrame.Layers[0];

    [Fact]
    public void Duplicate_CopiesCellsAndSettings_ToJustAboveTheSource_UnderTheNewName()
    {
        var top = AddLayer("Top");
        Paint(Bottom, 1, 0, 'a');
        var hidden = Bottom.Properties with { IsVisible = false, IsLocked = true };
        _history.Execute(new SetLayerPropertiesCommand(Bottom.Id, hidden));
        var command = new DuplicateLayerCommand(Bottom.Id, "Bottom copy");

        _history.Execute(command);

        var layers = _document.ActiveFrame.Layers;
        Assert.Equal(3, layers.Count);
        Assert.Equal(command.LayerId, layers[1].Id);
        Assert.Equal(top.Id, layers[2].Id);
        Assert.Equal("Bottom copy", layers[1].Properties.Name);
        Assert.False(layers[1].Properties.IsVisible);
        Assert.True(layers[1].Properties.IsLocked);
        Assert.Equal(new Rune('a'), layers[1].GetCell(new CellPoint(1, 0)).Glyph);
        Assert.Equal(UndoStepNames.DuplicateLayer, command.StepNameKey);
    }

    [Fact]
    public void Duplicate_KeepsItsOwnCells()
    {
        Paint(Bottom, 0, 0, 'a');
        var command = new DuplicateLayerCommand(Bottom.Id, "Copy");
        _history.Execute(command);

        Paint(_document.GetLayer(command.LayerId), 0, 0, 'b');

        Assert.Equal(new Rune('a'), Bottom.GetCell(new CellPoint(0, 0)).Glyph);
    }

    [Fact]
    public void Duplicate_UndoRemovesIt_AndRedoBringsItBackWithTheSameId()
    {
        Paint(Bottom, 0, 0, 'a');
        var command = new DuplicateLayerCommand(Bottom.Id, "Copy");
        _history.Execute(command);

        _history.Undo();
        Assert.Single(_document.ActiveFrame.Layers);

        _history.Redo();
        var copy = _document.GetLayer(command.LayerId);
        Assert.Equal(new Rune('a'), copy.GetCell(new CellPoint(0, 0)).Glyph);
    }

    [Fact]
    public void MergeDown_LooksTheSameAsBefore()
    {
        var top = AddLayer("Top");
        Paint(Bottom, 0, 0, 'a');
        Paint(Bottom, 1, 0, 'b');
        Paint(top, 1, 0, 'c');
        Paint(top, 2, 0, ' ');
        Paint(Bottom, 2, 0, 'd');
        var before = Composite();

        _history.Execute(MergeLayerDown.Create(_document, top.Id));

        Assert.Single(_document.ActiveFrame.Layers);
        Assert.Equal(before, Composite());
    }

    [Fact]
    public void MergeDown_DoesntCopySeeThroughSpaces()
    {
        var top = AddLayer("Top");
        Paint(Bottom, 0, 0, 'a');
        Paint(top, 0, 0, ' ');
        _history.Execute(new SetLayerPropertiesCommand(top.Id, top.Properties with { SpaceHandling = SpaceHandling.TreatedAsEmpty }));

        _history.Execute(MergeLayerDown.Create(_document, top.Id));

        Assert.Equal(new Rune('a'), Bottom.GetCell(new CellPoint(0, 0)).Glyph);
    }

    // Three layers, so a change in what the lower two hide would show the one at the bottom.
    [Theory]
    [InlineData(SpaceHandling.HidesLayersBelow, SpaceHandling.HidesLayersBelow)]
    [InlineData(SpaceHandling.HidesLayersBelow, SpaceHandling.TreatedAsEmpty)]
    [InlineData(SpaceHandling.TreatedAsEmpty, SpaceHandling.HidesLayersBelow)]
    [InlineData(SpaceHandling.TreatedAsEmpty, SpaceHandling.TreatedAsEmpty)]
    public void MergeDown_KeepsThePicture_WhateverTheSpaceOptions(SpaceHandling upperOption, SpaceHandling lowerOption)
    {
        var lower = AddLayer("Lower");
        var upper = AddLayer("Upper");
        for (var x = 0; x < 4; x++)
            Paint(Bottom, x, 0, 'b');

        Paint(lower, 0, 0, ' ');
        Paint(lower, 1, 0, 'l');
        Paint(upper, 1, 0, ' ');
        Paint(upper, 2, 0, ' ');
        Paint(upper, 3, 0, 'u');
        _history.Execute(new SetLayerPropertiesCommand(lower.Id, lower.Properties with { SpaceHandling = lowerOption }));
        _history.Execute(new SetLayerPropertiesCommand(upper.Id, upper.Properties with { SpaceHandling = upperOption }));
        var before = Composite();

        _history.Execute(MergeLayerDown.Create(_document, upper.Id));
        Assert.Equal(before, Composite());

        _history.Undo();
        Assert.Equal(before, Composite());
        Assert.Equal(lowerOption, lower.Properties.SpaceHandling);
    }

    [Fact]
    public void MergeDown_KeepsTheLowerLayersNameAndSettings()
    {
        var top = AddLayer("Top");
        var lowerId = Bottom.Id;

        _history.Execute(MergeLayerDown.Create(_document, top.Id));

        Assert.Equal(lowerId, Bottom.Id);
        Assert.Equal("Bottom", Bottom.Properties.Name);
    }

    [Fact]
    public void MergeDown_IsOneNamedUndoStep_ThatRestoresBothLayers()
    {
        var top = AddLayer("Top");
        Paint(Bottom, 0, 0, 'a');
        Paint(top, 0, 0, 'b');
        var steps = _history.StepCount;

        _history.Execute(MergeLayerDown.Create(_document, top.Id));

        Assert.Equal(steps + 1, _history.StepCount);
        Assert.Equal(UndoStepNames.MergeLayerDown, _history.GetStepNameKey(steps));

        _history.Undo();

        Assert.Equal(2, _document.ActiveFrame.Layers.Count);
        Assert.Equal(new Rune('a'), Bottom.GetCell(new CellPoint(0, 0)).Glyph);
        Assert.Equal(new Rune('b'), _document.GetLayer(top.Id).GetCell(new CellPoint(0, 0)).Glyph);

        _history.Redo();

        Assert.Single(_document.ActiveFrame.Layers);
        Assert.Equal(new Rune('b'), Bottom.GetCell(new CellPoint(0, 0)).Glyph);
    }

    [Fact]
    public void MergeDown_OfAnEmptyLayer_JustRemovesIt()
    {
        var top = AddLayer("Top");
        Paint(Bottom, 0, 0, 'a');

        _history.Execute(MergeLayerDown.Create(_document, top.Id));

        Assert.Single(_document.ActiveFrame.Layers);
        Assert.Equal(new Rune('a'), Bottom.GetCell(new CellPoint(0, 0)).Glyph);
    }

    [Fact]
    public void MergeDown_OfTheBottomLayer_Throws()
    {
        AddLayer("Top");

        Assert.Throws<InvalidOperationException>(() =>
            MergeLayerDown.Create(_document, Bottom.Id));
    }

    [Fact]
    public void SetLayerProperties_TakesItsStepNameFromWhoeverMadeIt()
    {
        var command = new SetLayerPropertiesCommand(Bottom.Id, Bottom.Properties with { Name = "Sky" }, UndoStepNames.RenameLayer);

        Assert.Equal(UndoStepNames.RenameLayer, command.StepNameKey);
        Assert.Equal(UndoStepNames.SetLayerProperties,
            new SetLayerPropertiesCommand(Bottom.Id, Bottom.Properties).StepNameKey);
    }

    private Layer AddLayer(string name)
    {
        var command = new AddLayerCommand(_document.ActiveFrame.Layers.Count, new LayerProperties(name));
        _history.Execute(command);
        return _document.GetLayer(command.LayerId);
    }

    private void Paint(Layer layer, int x, int y, char glyph)
    {
        var builder = new CellEditBuilder(_document, layer.Id);
        builder.Set(new CellPoint(x, y), Cell.FromGlyph(new Rune(glyph)));
        _history.Execute(builder.Build()!);
    }

    private Cell[] Composite()
    {
        var cells = new Cell[_document.CanvasSize.CellCount];
        Compositor.CompositeRegion(_document.ActiveFrame, _document.CanvasSize.Bounds, cells);
        return cells;
    }
}
