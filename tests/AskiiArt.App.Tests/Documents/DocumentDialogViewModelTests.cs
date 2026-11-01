// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Documents;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Tests.Documents;

/// <summary>The New and Resize Canvas dialogs' view models, without windows.</summary>
public sealed class DocumentDialogViewModelTests
{
    [Fact]
    public void New_StartsAt80By25_AsciiOnly()
    {
        var dialog = new NewDocumentViewModel();

        Assert.Equal(new NewDocumentSettings(new CanvasSize(80, 25), CharacterSet.Ascii), dialog.Result);
        Assert.False(dialog.IsUnicode);
    }

    [Fact]
    public void New_ChoosingUnicode_ChangesTheResult()
    {
        var dialog = new NewDocumentViewModel { IsUnicode = true };

        Assert.False(dialog.IsAsciiOnly);
        Assert.Equal(CharacterSet.Unicode, dialog.Result!.CharacterSet);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(0.0, 10)]
    [InlineData(4097.0, 10)]
    [InlineData(10.5, 10)]
    public void SizeFields_OutsideTheLimits_AreInvalid(double? width, double height)
    {
        var fields = new CanvasSizeFields(new CanvasSize(1, 1))
        {
            Width = (decimal?)width,
            Height = (decimal)height
        };

        Assert.False(fields.IsValid);
        Assert.Null(fields.Size);
    }

    [Fact]
    public void SizeFields_AboveTheSoftLimit_WarnButStayValid()
    {
        var fields = new CanvasSizeFields(new CanvasSize(500, 500));
        Assert.False(fields.ExceedsRecommendedSize);

        fields.Height = 501;

        Assert.True(fields.ExceedsRecommendedSize);
        Assert.Equal(new CanvasSize(500, 501), fields.Size);
        Assert.Contains("500", CanvasSizeFields.RecommendedSizeWarning, StringComparison.Ordinal);
    }

    [Fact]
    public void Resize_StartsAtTheCurrentSize_AnchoredInTheCentre()
    {
        var dialog = new ResizeCanvasViewModel(new CanvasSize(40, 20));

        Assert.Equal(new CanvasResizeRequest(new CanvasSize(40, 20), ResizeAnchor.Center), dialog.Result);
        Assert.Equal(Enum.GetValues<ResizeAnchor>(), dialog.Anchors.Select(option => option.Anchor));
        Assert.False(dialog.CropsCanvas);
    }

    [Fact]
    public void Resize_Shrinking_WarnsThatCellsAreRemoved()
    {
        var dialog = new ResizeCanvasViewModel(new CanvasSize(40, 20));
        var changes = new List<string?>();
        dialog.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        dialog.Size.Width = 30;
        dialog.SelectedAnchor = dialog.Anchors[0];

        Assert.True(dialog.CropsCanvas);
        Assert.Contains(nameof(ResizeCanvasViewModel.CropsCanvas), changes);
        Assert.Equal(new CanvasResizeRequest(new CanvasSize(30, 20), ResizeAnchor.TopLeft), dialog.Result);
    }

    [Fact]
    public void Resize_WithAnEmptySizeBox_CantBeAccepted()
    {
        var dialog = new ResizeCanvasViewModel(new CanvasSize(40, 20));

        dialog.Size.Height = null;

        Assert.False(dialog.CanAccept);
        Assert.Null(dialog.Result);
    }
}
