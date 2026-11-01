// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Fonts;

public sealed class FontPickerViewModelTests : IDisposable
{
    private readonly FontCatalog _catalog = new(BundledFonts.All, () => ["Courier New"], _ => null);

    [Fact]
    public void Opens_WithTheDocumentsFontSelected()
    {
        var picker = CreatePicker(new FontReference("Roboto Mono"));

        Assert.Equal(new FontReference("Roboto Mono"), picker.Result);
        Assert.True(picker.CanAccept);
        Assert.False(picker.IsNotMonospace);
    }

    [Fact]
    public void Search_FiltersByName_AndKeepsTheSelectionWhileItsListed()
    {
        var picker = CreatePicker(FontReference.Default);

        picker.SearchText = "mono";
        Assert.Equal(3, picker.Fonts.Count);
        Assert.Equal(FontReference.Default, picker.Result);

        picker.SearchText = "courier";
        Assert.Equal(["Courier New"], picker.Fonts.Select(item => item.DisplayName));
        Assert.Null(picker.Result);
        Assert.False(picker.CanAccept);
    }

    [Fact]
    public void BundledFonts_AreLabelledAsComingWithTheApp()
    {
        var picker = CreatePicker(FontReference.Default);

        Assert.Equal(string.Format(Strings.Culture, Strings.FontSourceBundledFormat, "JetBrains Mono"),
            picker.Fonts[0].DisplayName);
    }

    [Fact]
    public async Task LoadFromFile_WithAFamilyAlreadyAvailable_SelectsItAndSaysSo()
    {
        var picker = CreatePicker(FontReference.Default, FontCatalogTests.FiraCodeFile);

        await picker.LoadFromFileCommand.ExecuteAsync(null);

        Assert.Equal(new FontReference("Fira Code"), picker.Result);
        Assert.Contains("Fira Code", picker.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadFromFile_WithANewFamily_AddsAndSelectsIt()
    {
        using var catalog = FontCatalogTests.CreateCatalog();
        var picker = new FontPickerViewModel(catalog, FontReference.Default,
            () => Task.FromResult<string?>(FontCatalogTests.FiraCodeFile), NullLogger.Instance);

        await picker.LoadFromFileCommand.ExecuteAsync(null);

        Assert.Equal(new FontReference("Fira Code"), picker.Result);
        Assert.Null(picker.Message);
        Assert.Equal(string.Format(Strings.Culture, Strings.FontSourceFileFormat, "Fira Code"),
            picker.SelectedFont!.DisplayName);
    }

    [Fact]
    public async Task LoadFromFile_ThatIsNotAFont_ShowsAnError_AndKeepsTheSelection()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not a font");
            var picker = CreatePicker(FontReference.Default, path);

            await picker.LoadFromFileCommand.ExecuteAsync(null);

            Assert.Equal(Strings.FontLoadFailed, picker.Message);
            Assert.Equal(FontReference.Default, picker.Result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadFromFile_Cancelled_ChangesNothing()
    {
        var picker = CreatePicker(FontReference.Default);

        await picker.LoadFromFileCommand.ExecuteAsync(null);

        Assert.Null(picker.Message);
        Assert.Equal(FontReference.Default, picker.Result);
    }

    public void Dispose() => _catalog.Dispose();

    private FontPickerViewModel CreatePicker(FontReference current, string? pickedFile = null) =>
        new(_catalog, current, () => Task.FromResult(pickedFile), NullLogger.Instance);
}
