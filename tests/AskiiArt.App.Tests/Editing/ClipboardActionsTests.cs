// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Editing;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Text;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Editing;

/// <summary>The Edit menu's Cut, Copy and Paste (ADR 0067).</summary>
public sealed class ClipboardActionsTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(6, 3));
    private readonly FakeAppShell _shell = new();
    private readonly Dictionary<string, AppAction> _actions;

    public ClipboardActionsTests()
    {
        _actions = new ClipboardActions(_canvas.ActiveDocument, _canvas.ViewModel, _shell).CreateActions()
            .ToDictionary(action => action.Id);

        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        for (var x = 0; x < 6; x++)
            cells.Set(new CellPoint(x, 0), Cell.FromGlyph(new Rune("abcdef"[x])));

        History.Execute(cells.Build(UndoStepNames.Typing)!);
    }

    private AppAction Cut => _actions[EditActionIds.Cut];

    private AppAction Copy => _actions[EditActionIds.Copy];

    private AppAction Paste => _actions[EditActionIds.Paste];

    private EditingSession Session => _canvas.Session;

    private CommandHistory History => Session.History;

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void TheShortcuts_AreTheUsualOnes()
    {
        Assert.Equal([new ActionShortcut(Key.X, ShortcutModifiers.Command)], Cut.DefaultShortcuts);
        Assert.Equal([new ActionShortcut(Key.C, ShortcutModifiers.Command)], Copy.DefaultShortcuts);
        Assert.Equal([new ActionShortcut(Key.V, ShortcutModifiers.Command)], Paste.DefaultShortcuts);
    }

    [Fact]
    public void Copy_PutsTheSelectedCellsOnTheClipboard_AsTextAndAsCells()
    {
        Select(new CellRect(1, 0, 2, 1));
        var steps = History.UndoCount;

        Copy.Command.Execute(null);

        Assert.Equal("bc", Assert.Single(_shell.CopiedTexts));
        var clip = CellClipFormat.TryRead(_shell.Clipboard.Cells!);
        Assert.NotNull(clip);
        Assert.Equal(new CellPoint(1, 0), clip.Origin);
        Assert.Equal(new CanvasSize(2, 1), clip.Size);
        Assert.Equal(steps, History.UndoCount);
    }

    [Fact]
    public void CutAndCopy_AreGreyedOut_UntilSomethingIsSelected()
    {
        Assert.False(Cut.Command.CanExecute(null));
        Assert.False(Copy.Command.CanExecute(null));

        Select(new CellRect(0, 0, 1, 1));

        Assert.True(Cut.Command.CanExecute(null));
        Assert.True(Copy.Command.CanExecute(null));
        Assert.True(Paste.Command.CanExecute(null));
    }

    [Fact]
    public void OnALockedLayer_CopyWorks_ButCutAndPasteAreGreyedOut()
    {
        Select(new CellRect(0, 0, 1, 1));

        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));

        Assert.True(Copy.Command.CanExecute(null));
        Assert.False(Cut.Command.CanExecute(null));
        Assert.False(Paste.Command.CanExecute(null));
    }

    [Fact]
    public void OnAHiddenLayer_AllThreeAreGreyedOut()
    {
        Select(new CellRect(0, 0, 1, 1));

        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsVisible = false }));

        Assert.False(Copy.Command.CanExecute(null));
        Assert.False(Cut.Command.CanExecute(null));
        Assert.False(Paste.Command.CanExecute(null));
    }

    [Fact]
    public void Cut_CopiesTheSelectedCellsAndEmptiesThem_AsOneCutStep_KeepingTheSelection()
    {
        Select(new CellRect(1, 0, 2, 1));
        var selection = Session.Selection.Mask;

        Cut.Command.Execute(null);

        Assert.Equal("bc", Assert.Single(_shell.CopiedTexts));
        Assert.Equal("a··def", Row(0));
        Assert.Equal(UndoStepNames.Cut, History.GetStepNameKey(History.UndoCount - 1));
        Assert.Equal(selection, Session.Selection.Mask);
    }

    [Fact]
    public void Paste_FloatsWhatsOnTheClipboard_CentredOnThePointer_SelectedWithTheMoveTool()
    {
        _shell.Clipboard = new ClipboardContents(null, "xy");
        _canvas.ViewModel.PointerCanvasPosition = new CanvasPoint(3.2, 1.6);

        Paste.Command.Execute(null);

        Assert.Equal("··xy··", Row(1));
        var moveTool = Assert.IsType<MoveTool>(_canvas.ViewModel.ActiveTool);
        Assert.True(moveTool.IsMoving);
        Assert.Equal(SelectionMask.Rectangle(_canvas.Document.CanvasSize, new CellRect(2, 1, 2, 1)),
            Session.Selection.Mask);
        Assert.Equal(UndoStepNames.Paste, History.GetStepNameKey(History.UndoCount - 1));
    }

    [Fact]
    public void Paste_WithThePointerElsewhere_PutsCopiedCellsBackWhereTheyCameFrom()
    {
        Select(new CellRect(1, 0, 2, 1));
        Cut.Command.Execute(null);
        _canvas.ViewModel.PointerCanvasPosition = null;

        Paste.Command.Execute(null);

        Assert.Equal("abcdef", Row(0));
    }

    [Fact]
    public void Paste_OfTextFromAnotherApp_WithThePointerElsewhere_GoesInTheMiddleOfTheView()
    {
        _shell.Clipboard = new ClipboardContents(null, "xy");
        _canvas.ViewModel.ViewCentre = new CanvasPoint(3, 2);

        Paste.Command.Execute(null);

        Assert.Equal("··xy··", Row(2));
    }

    [Fact]
    public void Paste_AtTheTextToolsCaret_TypesTheTextIn()
    {
        _canvas.ViewModel.SelectTool(TextTool.Id);
        _canvas.ViewModel.ActiveTool.PointerPressed(
            new ToolPointerInput(new CellPoint(1, 1), 0.5, 0.5, ToolModifiers.None));
        _shell.Clipboard = new ClipboardContents(null, "pq");

        Paste.Command.Execute(null);

        Assert.Equal("·pq···", Row(1));
        Assert.IsType<TextTool>(_canvas.ViewModel.ActiveTool);
    }

    [Fact]
    public void Paste_WithNothingItCanUseOnTheClipboard_DoesNothing()
    {
        var steps = History.UndoCount;
        _shell.Clipboard = new ClipboardContents("not cells", "   ");

        Paste.Command.Execute(null);

        Assert.IsType<HandTool>(_canvas.ViewModel.ActiveTool);
        Assert.Equal(steps, History.UndoCount);
    }

    [Fact]
    public void AfterANewDocument_TheCommandsFollowItsSelection()
    {
        Select(new CellRect(0, 0, 1, 1));

        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(2, 2), "Layer")));

        Assert.False(Copy.Command.CanExecute(null));
        Select(new CellRect(0, 0, 1, 1));
        Assert.True(Copy.Command.CanExecute(null));
    }

    private void Select(CellRect rect) =>
        SelectionEdits.Select(Session, SelectionMask.Rectangle(_canvas.Document.CanvasSize, rect),
            UndoStepNames.RectangleSelect);

    private string Row(int y)
    {
        var row = new StringBuilder();
        for (var x = 0; x < _canvas.Document.CanvasSize.Width; x++)
        {
            var cell = Layer.GetCell(new CellPoint(x, y));
            row.Append(cell.IsEmpty ? "·" : cell.Glyph.ToString());
        }

        return row.ToString();
    }
}
