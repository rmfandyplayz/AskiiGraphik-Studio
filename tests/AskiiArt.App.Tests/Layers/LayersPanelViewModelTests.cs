// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Layers;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Layers;

public sealed class LayersPanelViewModelTests : IDisposable
{
    private readonly TestCanvas _canvas = new();
    private readonly LayersPanelViewModel _panel;

    public LayersPanelViewModelTests()
    {
        _panel = new LayersPanelViewModel(_canvas.ActiveDocument);
    }

    public void Dispose() => _canvas.Dispose();

    private EditingSession Session => _canvas.Session;

    private IReadOnlyList<Layer> Layers => _canvas.Document.ActiveFrame.Layers;

    private string[] RowNames => [.. _panel.Rows.Select(row => row.Name)];

    [AvaloniaFact]
    public void Rows_ShowTheTopLayerFirst()
    {
        _panel.AddLayer();
        _panel.AddLayer();

        Assert.Equal([Layers[2].Properties.Name, Layers[1].Properties.Name, Layers[0].Properties.Name], RowNames);
    }

    [AvaloniaFact]
    public void AddLayer_GoesAboveTheActiveLayer_AndBecomesActive()
    {
        _panel.AddLayer();
        var second = Session.ActiveLayer.Id;
        Session.SetActiveLayer(Layers[0].Id);

        _panel.AddLayer();

        Assert.Equal(3, Layers.Count);
        Assert.Equal(Session.ActiveLayer.Id, Layers[1].Id);
        Assert.Equal(second, Layers[2].Id);
        Assert.Equal(1, _panel.ActiveRow);
    }

    [AvaloniaFact]
    public void AddLayer_PicksAnUnusedName()
    {
        _panel.AddLayer();
        Assert.Equal(LayerName(2), Session.ActiveLayer.Properties.Name);

        _panel.AddLayer();
        _panel.Rows[0].BeginRename();
        _panel.Rows[0].NameBeingEdited = LayerName(4);
        _panel.FinishRename(_panel.Rows[0], keepNewName: true);

        _panel.AddLayer();
        Assert.Equal(LayerName(5), Session.ActiveLayer.Properties.Name);
    }

    [AvaloniaFact]
    public void ActiveRow_MakesThatRowsLayerActive_AndIgnoresNoSelection()
    {
        _panel.AddLayer();
        var bottom = Layers[0].Id;

        _panel.ActiveRow = 1;
        Assert.Equal(bottom, Session.ActiveLayer.Id);

        _panel.ActiveRow = -1;
        Assert.Equal(bottom, Session.ActiveLayer.Id);
        Assert.Equal(1, _panel.ActiveRow);
    }

    [AvaloniaFact]
    public void DeleteActiveLayer_LetsTheOneBelowTakeOver()
    {
        _panel.AddLayer();
        var bottom = Layers[0].Id;

        _panel.DeleteActiveLayer();

        Assert.Single(Layers);
        Assert.Equal(bottom, Session.ActiveLayer.Id);
        Assert.Single(_panel.Rows);
    }

    [AvaloniaFact]
    public void DeletingTheBottomLayer_LetsTheOneAboveTakeOver()
    {
        _panel.AddLayer();
        var top = Layers[1].Id;
        Session.SetActiveLayer(Layers[0].Id);

        _panel.DeleteActiveLayer();

        Assert.Equal(top, Session.ActiveLayer.Id);
    }

    [AvaloniaFact]
    public void TheLastLayer_CantBeDeleted()
    {
        Assert.False(_panel.CanDeleteActiveLayer);

        _panel.DeleteActiveLayer();

        Assert.Single(Layers);
    }

    [AvaloniaFact]
    public void ALockedLayer_CantBeDeleted()
    {
        _panel.AddLayer();
        _panel.Rows[0].IsLocked = true;

        Assert.False(_panel.CanDeleteActiveLayer);
    }

    [AvaloniaFact]
    public void MoveUpAndDown_MoveTheActiveLayer_AsFarAsItCanGo()
    {
        _panel.AddLayer();
        var top = Session.ActiveLayer.Id;
        Assert.False(_panel.CanMoveActiveLayerUp);
        Assert.True(_panel.CanMoveActiveLayerDown);

        _panel.MoveActiveLayerDown();

        Assert.Equal(top, Layers[0].Id);
        Assert.Equal(top, Session.ActiveLayer.Id);
        Assert.Equal(1, _panel.ActiveRow);
        Assert.False(_panel.CanMoveActiveLayerDown);
        Assert.True(_panel.CanMoveActiveLayerUp);

        _panel.MoveActiveLayerUp();

        Assert.Equal(top, Layers[1].Id);
        Assert.Equal(0, _panel.ActiveRow);
    }

    [AvaloniaFact]
    public void DuplicateActiveLayer_CopiesItToJustAbove_AndMakesTheCopyActive()
    {
        Paint(Session.ActiveLayer.Id, 'a');
        var original = Session.ActiveLayer;

        _panel.DuplicateActiveLayer();

        Assert.Equal(2, Layers.Count);
        Assert.Equal(Session.ActiveLayer.Id, Layers[1].Id);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.LayerCopyNameFormat, original.Properties.Name), Session.ActiveLayer.Properties.Name);
        Assert.Equal(original.GetCell(new CellPoint(0, 0)), Session.ActiveLayer.GetCell(new CellPoint(0, 0)));
    }

    [AvaloniaFact]
    public void MergeActiveLayerDown_MakesTheLowerLayerActive_AndUndoBringsItBack()
    {
        _panel.AddLayer();
        var top = Session.ActiveLayer.Id;
        var bottom = Layers[0].Id;
        Paint(top, 'a');

        _panel.MergeActiveLayerDown();

        Assert.Single(Layers);
        Assert.Equal(bottom, Session.ActiveLayer.Id);
        Assert.Equal(new Rune('a'), Session.ActiveLayer.GetCell(new CellPoint(0, 0)).Glyph);

        Session.History.Undo();

        Assert.Equal(2, Layers.Count);
        Assert.Equal(2, _panel.Rows.Count);
        Assert.True(Layers[0].GetCell(new CellPoint(0, 0)).IsEmpty);
    }

    [AvaloniaTheory]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public void MergeDown_NeedsBothLayersVisibleAndUnlocked(bool hideUpper, bool lockUpper, bool lockLower)
    {
        _panel.AddLayer();
        Assert.True(_panel.CanMergeActiveLayerDown);

        if (hideUpper)
            _panel.Rows[0].IsShown = false;

        if (lockUpper)
            _panel.Rows[0].IsLocked = true;

        if (lockLower)
            _panel.Rows[1].IsLocked = true;

        Assert.False(_panel.CanMergeActiveLayerDown);
    }

    [AvaloniaFact]
    public void MergeDown_NeedsALayerBelow()
    {
        Assert.False(_panel.CanMergeActiveLayerDown);
    }

    [AvaloniaFact]
    public void TheRowsCheckboxes_ChangeTheLayer_AsNamedUndoSteps()
    {
        var row = _panel.Rows[0];

        row.IsShown = false;
        Assert.False(Session.ActiveLayer.Properties.IsVisible);
        Assert.False(row.IsShown);
        Assert.Equal(UndoStepNames.ShowOrHideLayer, Session.History.GetStepNameKey(0));

        row.IsLocked = true;
        Assert.True(Session.ActiveLayer.Properties.IsLocked);
        Assert.Equal(UndoStepNames.LockOrUnlockLayer, Session.History.GetStepNameKey(1));

        Session.History.Undo();
        Session.History.Undo();

        Assert.True(row.IsShown);
        Assert.False(row.IsLocked);
    }

    [AvaloniaFact]
    public void FinishRename_KeepsOnlyARealChange()
    {
        var row = _panel.Rows[0];
        var name = row.Name;

        row.BeginRename();
        Assert.Equal(name, row.NameBeingEdited);
        _panel.FinishRename(row, keepNewName: true);
        Assert.False(row.IsRenaming);
        Assert.False(Session.History.CanUndo);

        row.BeginRename();
        row.NameBeingEdited = "   ";
        _panel.FinishRename(row, keepNewName: true);
        Assert.Equal(name, row.Name);
        Assert.False(Session.History.CanUndo);

        row.BeginRename();
        row.NameBeingEdited = "Sky";
        _panel.FinishRename(row, keepNewName: false);
        Assert.Equal(name, row.Name);

        row.BeginRename();
        row.NameBeingEdited = "  Sky ";
        _panel.FinishRename(row, keepNewName: true);
        Assert.Equal("Sky", row.Name);
        Assert.Equal(UndoStepNames.RenameLayer, Session.History.GetStepNameKey(0));
    }

    [AvaloniaFact]
    public void FinishRename_DoesNothingTheSecondTime()
    {
        var row = _panel.Rows[0];
        row.BeginRename();
        row.NameBeingEdited = "Sky";
        _panel.FinishRename(row, keepNewName: false);

        // Closing the box takes its focus away, which reports the same finish again.
        _panel.FinishRename(row, keepNewName: true);

        Assert.NotEqual("Sky", row.Name);
    }

    [AvaloniaFact]
    public void Rows_FollowUndoAndRedo_IncludingTheActiveRow()
    {
        _panel.AddLayer();
        Assert.Equal(2, _panel.Rows.Count);

        Session.History.Undo();
        Assert.Single(_panel.Rows);
        Assert.Equal(0, _panel.ActiveRow);

        Session.History.Redo();
        Assert.Equal(2, _panel.Rows.Count);
    }

    [AvaloniaFact]
    public void Rows_AreReusedAcrossAReorder()
    {
        _panel.AddLayer();
        var top = _panel.Rows[0];
        var bottom = _panel.Rows[1];

        _panel.MoveActiveLayerDown();

        Assert.Same(bottom, _panel.Rows[0]);
        Assert.Same(top, _panel.Rows[1]);
    }

    [AvaloniaFact]
    public void StateChanged_FiresWhenTheActiveLayerChanges()
    {
        _panel.AddLayer();
        var raised = 0;
        _panel.StateChanged += (_, _) => raised++;

        _panel.ActiveRow = 1;

        Assert.Equal(1, raised);
    }

    [AvaloniaFact]
    public void ARenameRequest_NeedsAShowingPanel()
    {
        LayerRow? requested = null;
        _panel.RenameRequested += (_, row) => requested = row;

        _panel.RenameActiveLayer();

        Assert.Same(_panel.Rows[_panel.ActiveRow], requested);
    }

    [AvaloniaFact]
    public void ANewDocument_ReplacesTheRows_AndTheOldOneIsLetGo()
    {
        _panel.AddLayer();
        var oldSession = Session;

        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(10, 10), "Only")));

        Assert.Equal(["Only"], RowNames);

        oldSession.History.Undo();
        Assert.Equal(["Only"], RowNames);
    }

    private static string LayerName(int number) =>
        string.Format(CultureInfo.CurrentCulture, Strings.LayerNameFormat, number);

    private void Paint(Guid layerId, char glyph)
    {
        var builder = new CellEditBuilder(_canvas.Document, layerId);
        builder.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune(glyph)));
        Session.History.Execute(builder.Build()!);
    }
}
