// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Documents;

/// <summary>The temporary Document menu items that change the colour mode (ADR 0036).</summary>
public sealed class ColorModeActionsTests : IDisposable
{
    private static readonly CellColor _orange = CellColor.FromRgb(new RgbColor(255, 136, 0));

    private readonly FakeAppShell _shell = new();
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1), colorMode: ColorMode.Rgb);
    private readonly Dictionary<string, AppAction> _actions;

    public ColorModeActionsTests()
    {
        _actions = new ColorModeActions(_canvas.ActiveDocument, _shell).CreateActions()
            .ToDictionary(action => action.Id);
    }

    private Document Document => _canvas.Document;

    private Layer Layer => Document.ActiveFrame.Layers[0];

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void TheDocumentsColorMode_IsTheOneTicked()
    {
        Assert.Equal([false, false, true], TickedStates());
    }

    [Fact]
    public async Task SwitchingToAnsi_AsksFirst_ThenConvertsAsOneUndoStep()
    {
        Paint(_orange);
        _shell.ConfirmationAnswer = true;

        await ExecuteAsync(ColorModeActionIds.Ansi);

        var expected = string.Format(CultureInfo.CurrentCulture, Strings.ColorModeConvertMessageFormat, 1);
        Assert.Equal([expected], _shell.ConfirmationMessages);
        Assert.Equal(ColorMode.Ansi, Document.ColorMode);
        Assert.Equal(ColorMode.Ansi.Convert(_orange), Layer.GetCell(new CellPoint(0, 0)).Foreground);
        Assert.Equal([false, true, false], TickedStates());

        _canvas.Session.History.Undo();

        Assert.Equal(ColorMode.Rgb, Document.ColorMode);
        Assert.Equal(_orange, Layer.GetCell(new CellPoint(0, 0)).Foreground);
        Assert.Equal([false, false, true], TickedStates());
    }

    [Fact]
    public async Task SwitchingToMonochrome_SaysTheColorsWillBeRemoved()
    {
        Paint(_orange);
        _shell.ConfirmationAnswer = true;

        await ExecuteAsync(ColorModeActionIds.Monochrome);

        var expected = string.Format(CultureInfo.CurrentCulture, Strings.ColorModeRemoveMessageFormat, 1);
        Assert.Equal([expected], _shell.ConfirmationMessages);
        Assert.True(Layer.GetCell(new CellPoint(0, 0)).Foreground.IsUnset);
    }

    [Fact]
    public async Task SayingNo_ChangesNothing_AndTicksTheOldModeAgain()
    {
        Paint(_orange);
        var notified = 0;
        _actions[ColorModeActionIds.Ansi].CheckedState!.Changed += (_, _) => notified++;

        await ExecuteAsync(ColorModeActionIds.Ansi);

        Assert.Equal(ColorMode.Rgb, Document.ColorMode);
        Assert.Equal(_orange, Layer.GetCell(new CellPoint(0, 0)).Foreground);
        Assert.Equal(1, notified);
    }

    [Fact]
    public async Task WhenNoCellWouldLookDifferent_ItDoesntAsk()
    {
        Paint(CellColor.FromRgb(AnsiPalette.Vga[AnsiColor.Red]));

        await ExecuteAsync(ColorModeActionIds.Ansi);

        Assert.Empty(_shell.ConfirmationMessages);
        Assert.Equal(CellColor.FromAnsi(AnsiColor.Red), Layer.GetCell(new CellPoint(0, 0)).Foreground);
    }

    [Fact]
    public async Task ChoosingTheCurrentMode_ChangesNothing()
    {
        await ExecuteAsync(ColorModeActionIds.Rgb);

        Assert.False(_canvas.Session.History.CanUndo);
    }

    [Fact]
    public void TheTicks_FollowANewDocument()
    {
        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(2, 2), "Layer")));

        Assert.Equal([true, false, false], TickedStates());
    }

    private bool[] TickedStates() =>
    [
        _actions[ColorModeActionIds.Monochrome].CheckedState!.IsChecked,
        _actions[ColorModeActionIds.Ansi].CheckedState!.IsChecked,
        _actions[ColorModeActionIds.Rgb].CheckedState!.IsChecked,
    ];

    private void Paint(CellColor foreground)
    {
        var cells = new CellEditBuilder(Document, Layer.Id);
        cells.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('a'), foreground, CellColor.Unset));
        _canvas.Session.History.Execute(cells.Build()!);
    }

    private Task ExecuteAsync(string actionId) => ((IAsyncRelayCommand)_actions[actionId].Command).ExecuteAsync(null);
}
