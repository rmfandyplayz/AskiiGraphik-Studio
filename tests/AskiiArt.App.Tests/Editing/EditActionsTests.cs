// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Editing;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Editing;

public sealed class EditActionsTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1));
    private readonly FakeAppShell _shell = new();
    private readonly Dictionary<string, AppAction> _actions;

    public EditActionsTests()
    {
        _actions = new EditActions(_canvas.ActiveDocument, _canvas.TextExporter, _shell).CreateActions()
            .ToDictionary(action => action.Id);
    }

    private AppAction Undo => _actions[EditActionIds.Undo];

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
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered(text);
    }
}
