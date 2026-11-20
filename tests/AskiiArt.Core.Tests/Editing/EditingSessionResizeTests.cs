// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Selection;

namespace AskiiArt.Core.Tests.Editing;

/// <summary>Resizing the canvas through the session, which keeps the selection with the art (ADR 0055).</summary>
public sealed class EditingSessionResizeTests
{
    private readonly EditingSession _session = new(TestCells.CreateDocument(3, 2));

    private CommandHistory History => _session.History;

    [Fact]
    public void Resizing_MovesTheSelectionWithTheArt()
    {
        TestCells.Paint(_session.ActiveLayer, "ab·", "···");
        Select("##·", "···");

        _session.ResizeCanvas(new CanvasSize(5, 3), ResizeAnchor.BottomRight);

        Assert.Equal(["·····", "··ab·", "·····"], TestCells.Read(_session.ActiveLayer));
        Assert.Equal(["·····", "··##·", "·····"], TestMasks.Describe(_session.Selection.Mask));
    }

    [Fact]
    public void Cropping_DropsTheSelectedCellsThatAreCut()
    {
        Select("###", "#··");

        _session.ResizeCanvas(new CanvasSize(2, 2), ResizeAnchor.Right);

        Assert.Equal(["##", "··"], TestMasks.Describe(_session.Selection.Mask));
    }

    [Fact]
    public void Resizing_IsOneStep_NamedResizeCanvas_AndUndoBringsTheSelectionBack()
    {
        var before = Select("·#·", "·#·");

        _session.ResizeCanvas(new CanvasSize(1, 1), ResizeAnchor.TopLeft);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(2, History.UndoCount);
        Assert.Equal(UndoStepNames.ResizeCanvas, History.GetStepNameKey(1));
        Assert.True(History.HasUnsavedChanges);

        History.Undo();
        Assert.Equal(new CanvasSize(3, 2), _session.Document.CanvasSize);
        Assert.Same(before, _session.Selection.Mask);

        History.Redo();
        Assert.Equal(new CanvasSize(1, 1), _session.Selection.Mask.CanvasSize);
    }

    [Fact]
    public void Resizing_WithNothingSelected_LeavesNothingSelected_OnTheNewCanvas()
    {
        _session.ResizeCanvas(new CanvasSize(4, 4), ResizeAnchor.Center);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(new CanvasSize(4, 4), _session.Selection.Mask.CanvasSize);
    }

    private SelectionMask Select(params string[] rows)
    {
        var mask = TestMasks.Parse(rows);
        SelectionEdits.Select(_session, mask, UndoStepNames.RectangleSelect);
        return mask;
    }
}
