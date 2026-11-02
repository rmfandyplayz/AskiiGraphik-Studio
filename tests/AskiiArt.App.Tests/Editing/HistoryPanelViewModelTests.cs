// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Reflection;
using System.Text;
using AskiiArt.App.Editing;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.App.Tests.Editing;

public sealed class HistoryPanelViewModelTests : IDisposable
{
    private readonly TestCanvas _canvas = new();
    private readonly HistoryPanelViewModel _panel;

    public HistoryPanelViewModelTests()
    {
        _panel = new HistoryPanelViewModel(_canvas.ActiveDocument);
    }

    private CommandHistory History => _canvas.Session.History;

    public void Dispose() => _canvas.Dispose();

    public static TheoryData<string> StepNameKeys()
    {
        var keys = new TheoryData<string>();
        foreach (var field in typeof(UndoStepNames).GetFields(BindingFlags.Public | BindingFlags.Static))
            keys.Add((string)field.GetRawConstantValue()!);

        return keys;
    }

    [Theory]
    [MemberData(nameof(StepNameKeys))]
    public void EveryStepName_HasText(string key)
    {
        Assert.False(string.IsNullOrWhiteSpace(UiText.Get(key)));
    }

    [Fact]
    public void ANewHistory_ShowsOnlyTheStart_AsCurrent()
    {
        Assert.Equal([Strings.HistoryStart], RowNames());
        Assert.Equal(0, _panel.CurrentRow);
    }

    [Fact]
    public void EachStep_AddsANamedRow_AndBecomesCurrent()
    {
        Paint('a');
        History.Execute(new ResizeCanvasCommand(new CanvasSize(50, 30), ResizeAnchor.TopLeft));

        Assert.Equal([Strings.HistoryStart, Strings.UndoStepEditCells, Strings.UndoStepResizeCanvas], RowNames());
        Assert.Equal(2, _panel.CurrentRow);
    }

    [Fact]
    public void UndoAndRedo_MoveTheCurrentRow_AndDimUndoneSteps()
    {
        Paint('a');
        Paint('b');
        var notifications = new List<string?>();
        _panel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        History.Undo();

        Assert.Equal(1, _panel.CurrentRow);
        Assert.Contains(nameof(HistoryPanelViewModel.CurrentRow), notifications);
        Assert.Equal([false, false, true], _panel.Rows.Select(row => row.IsUndone));

        History.Redo();

        Assert.Equal(2, _panel.CurrentRow);
        Assert.Equal([false, false, false], _panel.Rows.Select(row => row.IsUndone));
    }

    [Fact]
    public void ChoosingARow_JumpsThere_InBothDirections()
    {
        Paint('a');
        Paint('b');
        Paint('c');
        var layer = _canvas.Document.ActiveFrame.Layers[0];

        _panel.CurrentRow = 1;

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(2, History.RedoCount);
        Assert.Equal(TestCell('a'), layer.GetCell(new CellPoint(0, 0)));

        _panel.CurrentRow = 3;

        Assert.Equal(3, History.UndoCount);
        Assert.Equal(TestCell('c'), layer.GetCell(new CellPoint(0, 0)));

        _panel.CurrentRow = 0;

        Assert.Equal(0, History.UndoCount);
        Assert.Equal(Cell.Empty, layer.GetCell(new CellPoint(0, 0)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void AnOutOfRangeRow_IsIgnored(int row)
    {
        Paint('a');

        _panel.CurrentRow = row;

        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ANewStepAfterJumpingBack_ReplacesTheUndoneRows()
    {
        Paint('a');
        Paint('b');
        Paint('c');
        _panel.CurrentRow = 1;

        History.Execute(new SetFontCommand(new FontReference("Another")));

        Assert.Equal([Strings.HistoryStart, Strings.UndoStepEditCells, Strings.UndoStepSetFont], RowNames());
        Assert.Equal(2, _panel.CurrentRow);
    }

    [Fact]
    public void ReplacingTheDocument_FollowsTheNewHistory_AndLetsGoOfTheOld()
    {
        Paint('a');
        var oldHistory = History;

        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(10, 10), "Layer")));

        Assert.Equal([Strings.HistoryStart], RowNames());
        Assert.Equal(0, _panel.CurrentRow);

        oldHistory.Undo();

        Assert.Equal([Strings.HistoryStart], RowNames());
    }

    private string[] RowNames() => [.. _panel.Rows.Select(row => row.Name)];

    private static Cell TestCell(char glyph) => Cell.FromGlyph(new Rune(glyph));

    private void Paint(char glyph)
    {
        var builder = new CellEditBuilder(_canvas.Document, _canvas.Document.ActiveFrame.Layers[0].Id);
        builder.Set(new CellPoint(0, 0), TestCell(glyph));
        History.Execute(builder.Build()!);
    }
}
