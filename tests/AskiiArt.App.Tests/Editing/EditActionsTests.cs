// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Editing;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Editing;

public sealed class EditActionsTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1));
    private readonly FakeAppShell _shell = new();
    private readonly Dictionary<string, AppAction> _actions;

    public EditActionsTests()
    {
        _actions = new EditActions(_canvas.ActiveDocument, _canvas.ViewModel, _canvas.TextExporter,
                _canvas.AnsiExporter, _shell)
            .CreateActions()
            .ToDictionary(action => action.Id);
    }

    private AppAction Undo => _actions[EditActionIds.Undo];

    // A tool building a shape point by point takes Undo while it does, even with nothing in the history, and Redo
    // does nothing meanwhile (ADR 0083).
    [Fact]
    public void Undo_IsTheToolsWhileItTakesUndo_AndRedoWaits()
    {
        _canvas.ViewModel.SelectTool(PolygonalLassoTool.Id);
        var lasso = (PolygonalLassoTool)_canvas.ViewModel.ActiveTool;
        Assert.False(Undo.Command.CanExecute(null));

        lasso.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        Assert.True(Undo.Command.CanExecute(null));
        Assert.False(Redo.Command.CanExecute(null));

        Undo.Command.Execute(null);

        Assert.False(lasso.IsDrawing);
        Assert.False(Undo.Command.CanExecute(null));
    }

    [Fact]
    public void Redo_ComesBack_WhenTheToolStopsTakingUndo()
    {
        var session = _canvas.Session;
        SelectionEdits.Select(session, SelectionMask.Rectangle(session.Document.CanvasSize, new CellRect(0, 0, 1, 1)),
            UndoStepNames.RectangleSelect);
        session.History.Undo();
        _canvas.ViewModel.SelectTool(PolygonalLassoTool.Id);
        var lasso = _canvas.ViewModel.ActiveTool;

        lasso.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        Assert.False(Redo.Command.CanExecute(null));

        lasso.KeyPressed(ToolKey.Escape, ToolModifiers.None);
        Assert.True(Redo.Command.CanExecute(null));
    }

    [Fact]
    public void CopyAsText_CopiesTheVisibleArt_WithThisPlatformsLineBreaks()
    {
        var document = Document.Create(new CanvasSize(2, 2), "Layer");
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        cells.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('a')));
        cells.Set(new CellPoint(1, 1), Cell.FromGlyph(new Rune('b')));
        cells.Build()!.Apply(document);
        _canvas.ActiveDocument.Replace(new EditingSession(document));
        var copyAsText = _actions[EditActionIds.CopyAsText];

        copyAsText.Command.Execute(null);

        Assert.Equal("a " + Environment.NewLine + " b", Assert.Single(_shell.CopiedTexts));
        Assert.Equal([new ActionShortcut(Key.C, ShortcutModifiers.Command | ShortcutModifiers.Shift)],
            copyAsText.DefaultShortcuts);
    }

    [Fact]
    public void CopyAsText_WithASelection_CopiesItsBounds_WithUnselectedCellsAsSpaces()
    {
        var document = Document.Create(new CanvasSize(3, 2), "Layer");
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        cells.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('a')));
        cells.Set(new CellPoint(1, 0), Cell.FromGlyph(new Rune('b')));
        cells.Set(new CellPoint(2, 1), Cell.FromGlyph(new Rune('c')));
        cells.Build()!.Apply(document);
        var session = new EditingSession(document);
        _canvas.ActiveDocument.Replace(session);
        SelectionEdits.Select(session, SelectionMask.Rectangle(document.CanvasSize, new CellRect(1, 0, 2, 2))
            .CombinedWith(SelectionMask.Rectangle(document.CanvasSize, new CellRect(2, 0, 1, 1)),
                SelectionCombineMode.Subtract), UndoStepNames.RectangleSelect);

        _actions[EditActionIds.CopyAsText].Command.Execute(null);

        Assert.Equal("b " + Environment.NewLine + " c", Assert.Single(_shell.CopiedTexts));
    }

    [Fact]
    public void CopyAsAnsi_CopiesTheVisibleArtWithItsColours_WithThisPlatformsLineBreaks()
    {
        var document = Document.Create(new CanvasSize(2, 2), "Layer", colorMode: ColorMode.Ansi);
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        var red = CellColor.FromAnsi(AnsiColor.Red);
        cells.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('a'), red, CellColor.Unset));
        cells.Set(new CellPoint(1, 1), Cell.FromGlyph(new Rune('b')));
        cells.Build()!.Apply(document);
        _canvas.ActiveDocument.Replace(new EditingSession(document));
        var copyAsAnsi = _actions[EditActionIds.CopyAsAnsi];

        copyAsAnsi.Command.Execute(null);

        Assert.Equal("\u001b[31ma \u001b[0m" + Environment.NewLine + " b", Assert.Single(_shell.CopiedTexts));
        Assert.Empty(copyAsAnsi.DefaultShortcuts);
    }

    private AppAction Redo => _actions[EditActionIds.Redo];

    [Fact]
    public void UndoAndRedo_FollowTheHistory()
    {
        Assert.False(Undo.Command.CanExecute(null));

        Type("ab");
        Assert.True(Undo.Command.CanExecute(null));

        Undo.Command.Execute(null);
        Assert.True(Layer.GetCell(new CellPoint(0, 0)).IsEmpty);
        Assert.True(Redo.Command.CanExecute(null));

        Redo.Command.Execute(null);
        Assert.Equal("a", Layer.GetCell(new CellPoint(0, 0)).Glyph.ToString());
    }

    [Fact]
    public void AfterANewDocument_UndoFollowsTheNewHistory()
    {
        Type("ab");
        var refreshes = 0;
        Undo.Command.CanExecuteChanged += (_, _) => refreshes++;

        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(2, 2), "Layer")));

        Assert.False(Undo.Command.CanExecute(null));
        Assert.True(refreshes > 0);

        Type("c");
        Assert.True(Undo.Command.CanExecute(null));
    }

    public void Dispose() => _canvas.Dispose();

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    private void Type(string text)
    {
        _canvas.ViewModel.SelectTool(TextTool.Id);
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered(text);
    }
}
