// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Documents;

public sealed class DocumentActionsTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 2));
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly Dictionary<string, AppAction> _actions;

    public DocumentActionsTests()
    {
        _actions = new DocumentActions(_canvas.ActiveDocument, _dialogs, _shell).CreateActions()
            .ToDictionary(action => action.Id);
    }

    private AppAction AsciiOnly => _actions[DocumentActionIds.AsciiOnly];

    private Document Document => _canvas.Document;

    private Layer Layer => Document.ActiveFrame.Layers[0];

    [Fact]
    public async Task New_ReplacesTheDocument_WithTheChosenSizeAndCharacterSet_AndAnEmptyHistory()
    {
        Type("ab");
        _dialogs.NewDocumentAnswer = new NewDocumentSettings(new CanvasSize(80, 25), CharacterSet.Ascii);

        await ExecuteAsync(DocumentActionIds.New);

        Assert.Equal(new CanvasSize(80, 25), Document.CanvasSize);
        Assert.Equal(CharacterSet.Ascii, Document.CharacterSet);
        Assert.Equal(FontReference.Default, Document.Font);
        Assert.False(_canvas.Session.History.CanUndo);
        Assert.Same(_canvas.Session, _canvas.ViewModel.Session);
    }

    [Fact]
    public async Task New_Cancelled_KeepsTheDocument()
    {
        var before = _canvas.Session;

        await ExecuteAsync(DocumentActionIds.New);

        Assert.Equal(1, _dialogs.NewDocumentShownCount);
        Assert.Same(before, _canvas.Session);
    }

    [Fact]
    public async Task ResizeCanvas_ResizesAroundTheAnchor_AsOneUndoStep()
    {
        Type("ab");
        _dialogs.ResizeCanvasAnswer = new CanvasResizeRequest(new CanvasSize(6, 2), ResizeAnchor.Right);

        await ExecuteAsync(DocumentActionIds.ResizeCanvas);

        Assert.Equal([new CanvasSize(4, 2)], _dialogs.ResizeCanvasShownWith);
        Assert.Equal(new CanvasSize(6, 2), Document.CanvasSize);
        Assert.Equal("a", Layer.GetCell(new CellPoint(2, 0)).Glyph.ToString());

        _canvas.Session.History.Undo();
        Assert.Equal(new CanvasSize(4, 2), Document.CanvasSize);
    }

    [Fact]
    public async Task ResizeCanvas_ToTheSameSize_AddsNoUndoStep()
    {
        _dialogs.ResizeCanvasAnswer = new CanvasResizeRequest(new CanvasSize(4, 2), ResizeAnchor.Center);

        await ExecuteAsync(DocumentActionIds.ResizeCanvas);

        Assert.False(_canvas.Session.History.CanUndo);
    }

    [Fact]
    public async Task Font_SwitchesTheDocumentsFont_AndTheCanvasRedrawsWithIt()
    {
        _dialogs.FontPickerAnswer = new FontReference("Fira Code");

        await ExecuteAsync(DocumentActionIds.Font);

        Assert.Equal([FontReference.Default], _dialogs.FontPickerShownWith);
        Assert.Equal(new FontReference("Fira Code"), Document.Font);
        Assert.Equal("Fira Code", _canvas.ViewModel.Font.FamilyName);

        _canvas.Session.History.Undo();
        Assert.Equal("JetBrains Mono", _canvas.ViewModel.Font.FamilyName);
    }

    [Fact]
    public async Task Font_Cancelled_ChangesNothing()
    {
        await ExecuteAsync(DocumentActionIds.Font);

        Assert.False(_canvas.Session.History.CanUndo);
    }

    [Fact]
    public async Task AsciiOnly_WithNonAsciiContent_AsksFirst_AndCancellingKeepsUnicode()
    {
        Type("aé漢");
        var changes = 0;
        AsciiOnly.CheckedState!.Changed += (_, _) => changes++;

        await ExecuteAsync(DocumentActionIds.AsciiOnly);

        Assert.Single(_shell.ConfirmationMessages);
        Assert.Contains("2", _shell.ConfirmationMessages[0], StringComparison.Ordinal);
        Assert.Equal(CharacterSet.Unicode, Document.CharacterSet);
        Assert.False(AsciiOnly.CheckedState.IsChecked);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task AsciiOnly_Confirmed_RemovesNonAsciiGlyphs_AsOneUndoStep()
    {
        Type("aé");
        _shell.ConfirmationAnswer = true;

        await ExecuteAsync(DocumentActionIds.AsciiOnly);

        Assert.Equal(CharacterSet.Ascii, Document.CharacterSet);
        Assert.True(AsciiOnly.CheckedState!.IsChecked);
        Assert.True(Layer.GetCell(new CellPoint(1, 0)).IsEmpty);

        _canvas.Session.History.Undo();
        Assert.Equal(CharacterSet.Unicode, Document.CharacterSet);
        Assert.Equal("é", Layer.GetCell(new CellPoint(1, 0)).Glyph.ToString());
    }

    [Fact]
    public async Task AsciiOnly_WithOnlyAsciiContent_SwitchesWithoutAsking_AndAgainSwitchesBack()
    {
        Type("ab");

        await ExecuteAsync(DocumentActionIds.AsciiOnly);
        Assert.Equal(CharacterSet.Ascii, Document.CharacterSet);

        await ExecuteAsync(DocumentActionIds.AsciiOnly);
        Assert.Equal(CharacterSet.Unicode, Document.CharacterSet);
        Assert.Empty(_shell.ConfirmationMessages);
    }

    [Fact]
    public void AsciiOnly_FollowsTheNewDocument()
    {
        var changes = 0;
        AsciiOnly.CheckedState!.Changed += (_, _) => changes++;

        _canvas.ActiveDocument.Replace(new EditingSession(
            Document.Create(new CanvasSize(2, 2), "Layer", CharacterSet.Ascii)));

        Assert.True(AsciiOnly.CheckedState.IsChecked);
        Assert.Equal(1, changes);
    }

    public void Dispose() => _canvas.Dispose();

    private Task ExecuteAsync(string actionId) => ((IAsyncRelayCommand)_actions[actionId].Command).ExecuteAsync(null);

    private void Type(string text)
    {
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered(text);
    }
}
