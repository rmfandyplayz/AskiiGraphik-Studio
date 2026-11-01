// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Tests.Fonts;
using AskiiArt.Core.Documents;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Documents;

/// <summary>
/// The New, Resize Canvas and font picker windows on Avalonia's headless platform: they open,
/// and OK returns what the view model built. Their layout is a placeholder and isn't checked.
/// </summary>
public sealed class DocumentDialogWindowTests : IDisposable
{
    private readonly Window _owner = new();

    public DocumentDialogWindowTests()
    {
        _owner.Show();
    }

    [AvaloniaFact]
    public void NewDocument_Ok_ReturnsTheSettings()
    {
        var dialog = new NewDocumentWindow { DataContext = new NewDocumentViewModel() };

        var result = ShowAndClickOk<NewDocumentSettings?>(dialog);

        Assert.Equal(new NewDocumentSettings(new CanvasSize(80, 25), CharacterSet.Ascii), result);
    }

    [AvaloniaFact]
    public void ResizeCanvas_Ok_ReturnsTheResize()
    {
        var viewModel = new ResizeCanvasViewModel(new CanvasSize(40, 20));
        viewModel.Size.Width = 50;

        var result = ShowAndClickOk<CanvasResizeRequest?>(new ResizeCanvasWindow { DataContext = viewModel });

        Assert.Equal(new CanvasResizeRequest(new CanvasSize(50, 20), ResizeAnchor.Center), result);
    }

    [AvaloniaFact]
    public void FontPicker_Ok_ReturnsTheSelectedFont()
    {
        using var catalog = FontCatalogTests.CreateCatalog();
        var viewModel = new FontPickerViewModel(catalog, FontReference.Default, () => Task.FromResult<string?>(null));

        var result = ShowAndClickOk<FontReference?>(new FontPickerWindow { DataContext = viewModel });

        Assert.Equal(FontReference.Default, result);
    }

    public void Dispose() => _owner.Close();

    private TResult ShowAndClickOk<TResult>(Window dialog)
    {
        var closed = dialog.ShowDialog<TResult>(_owner);
        Dispatcher.UIThread.RunJobs();

        var ok = dialog.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "OkButton");
        Assert.True(ok.IsEffectivelyEnabled);
        ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        Assert.True(closed.IsCompleted);
        return closed.Result;
    }
}
