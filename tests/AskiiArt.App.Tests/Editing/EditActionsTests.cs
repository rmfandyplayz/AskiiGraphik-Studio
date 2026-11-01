// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Editing;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Editing;

public sealed class EditActionsTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1));
    private readonly FakeAppShell _shell = new();
    private readonly Dictionary<string, AppAction> _actions;

    public EditActionsTests()
    {
        _actions = new EditActions(_canvas.Session, _shell).CreateActions().ToDictionary(action => action.Id);
    }

    private AppAction Undo => _actions[EditActionIds.Undo];

    private AppAction Redo => _actions[EditActionIds.Redo];

    private AppAction AsciiOnly => _actions[EditActionIds.AsciiOnly];

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
    public async Task AsciiOnly_WithNonAsciiContent_AsksFirst_AndCancellingKeepsUnicode()
    {
        Type("aé漢");
        var changes = 0;
        AsciiOnly.CheckedState!.Changed += (_, _) => changes++;

        await ExecuteAsync(AsciiOnly);

        Assert.Single(_shell.ConfirmationMessages);
        Assert.Contains("2", _shell.ConfirmationMessages[0], StringComparison.Ordinal);
        Assert.Equal(CharacterSet.Unicode, _canvas.Document.CharacterSet);
        Assert.False(AsciiOnly.CheckedState.IsChecked);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task AsciiOnly_Confirmed_RemovesNonAsciiGlyphs_AsOneUndoStep()
    {
        Type("aé");
        _shell.ConfirmationAnswer = true;

        await ExecuteAsync(AsciiOnly);

        Assert.Equal(CharacterSet.Ascii, _canvas.Document.CharacterSet);
        Assert.True(AsciiOnly.CheckedState!.IsChecked);
        Assert.True(Layer.GetCell(new CellPoint(1, 0)).IsEmpty);

        Undo.Command.Execute(null);
        Assert.Equal(CharacterSet.Unicode, _canvas.Document.CharacterSet);
        Assert.Equal("é", Layer.GetCell(new CellPoint(1, 0)).Glyph.ToString());
    }

    [Fact]
    public async Task AsciiOnly_WithOnlyAsciiContent_SwitchesWithoutAsking_AndAgainSwitchesBack()
    {
        Type("ab");

        await ExecuteAsync(AsciiOnly);
        Assert.Equal(CharacterSet.Ascii, _canvas.Document.CharacterSet);

        await ExecuteAsync(AsciiOnly);
        Assert.Equal(CharacterSet.Unicode, _canvas.Document.CharacterSet);
        Assert.Empty(_shell.ConfirmationMessages);
    }

    public void Dispose() => _canvas.Dispose();

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    private static Task ExecuteAsync(AppAction action) => ((IAsyncRelayCommand)action.Command).ExecuteAsync(null);

    private void Type(string text)
    {
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered(text);
    }
}
