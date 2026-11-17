// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Painting;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;

namespace AskiiArt.App.Tests.Painting;

/// <summary>The FG Brush and the BG Brush, kept in step with the chosen colour (ADR 0049).</summary>
public sealed class ColorBrushTargetSyncTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 4));
    private readonly ColorBrushTargetSync _sync;

    public ColorBrushTargetSyncTests()
    {
        _sync = new ColorBrushTargetSync(_canvas.ViewModel);
    }

    private PaintColors Colors => _canvas.Colors;

    private string ActiveToolId => _canvas.ViewModel.ActiveTool.Descriptor.Id;

    public void Dispose()
    {
        _sync.Dispose();
        _canvas.Dispose();
    }

    [Fact]
    public void ChoosingTheBGBrush_ChoosesTheBGColor_AndTheFGBrushTheFGColor()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.BackgroundId);
        Assert.Equal(PaintColorTarget.Background, Colors.Target);

        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        Assert.Equal(PaintColorTarget.Foreground, Colors.Target);
    }

    [Fact]
    public void ChoosingTheOtherColor_WhileAColorBrushIsActive_SwitchesToTheOtherBrush()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);

        Colors.Target = PaintColorTarget.Background;
        Assert.Equal(ColorBrushTool.BackgroundId, ActiveToolId);

        Colors.Target = PaintColorTarget.Foreground;
        Assert.Equal(ColorBrushTool.ForegroundId, ActiveToolId);
    }

    [Theory]
    [InlineData(BrushTool.Id)]
    [InlineData(EraserTool.Id)]
    public void ChoosingAColor_WithAnyOtherTool_OnlyChoosesIt(string toolId)
    {
        _canvas.ViewModel.SelectTool(toolId);

        Colors.Target = PaintColorTarget.Background;

        Assert.Equal(toolId, ActiveToolId);
        Assert.Equal(PaintColorTarget.Background, Colors.Target);
    }

    [Fact]
    public void ChoosingTheRampBrush_LeavesTheChosenColor()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.BackgroundId);

        _canvas.ViewModel.SelectTool(BrushTool.Id);

        Assert.Equal(PaintColorTarget.Background, Colors.Target);
    }

    [Fact]
    public void AColorBrushChosenBeforehand_ChoosesItsColorAtOnce()
    {
        using var canvas = new TestCanvas(new CanvasSize(4, 4));
        canvas.ViewModel.SelectTool(ColorBrushTool.BackgroundId);

        using var sync = new ColorBrushTargetSync(canvas.ViewModel);

        Assert.Equal(PaintColorTarget.Background, canvas.Colors.Target);
    }

    [Fact]
    public void OnceDisposed_ItFollowsNothing()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        _sync.Dispose();

        Colors.Target = PaintColorTarget.Background;

        Assert.Equal(ColorBrushTool.ForegroundId, ActiveToolId);
    }
}
