// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// What the user last chose for the tools is kept between runs (ADR 0052): the options, each group's last tool and the
/// colours. The app still starts on the Text tool.
/// </summary>
public sealed class RememberedToolStateTests : IDisposable
{
    private readonly FakeAppSettings _settings = new();
    private readonly TestCanvas _canvas = new();
    private readonly TestCanvas _nextRun = new();

    public void Dispose()
    {
        _canvas.Dispose();
        _nextRun.Dispose();
    }

    [Fact]
    public void WhatWasSaved_ComesBackNextRun_StillOnTheTextTool()
    {
        _canvas.ToolSettings.For(BrushTool.TipOptions).Set(BrushTipOptions.Size, 7);
        _canvas.ViewModel.SelectTool(SoftEraserTool.Id);
        var foreground = CellColor.FromAnsi(AnsiColor.Red);
        var background = CellColor.FromRgb(new RgbColor(0x12, 0x34, 0x56));
        _canvas.Colors.Set(foreground, background);

        new RememberedToolState(_settings).Save(_canvas.ViewModel);
        new RememberedToolState(_settings).Restore(_nextRun.ViewModel);

        Assert.Equal(TextTool.Id, _nextRun.ViewModel.SelectedTool.Descriptor.Id);
        Assert.Equal(7, _nextRun.ToolSettings.For(BrushTool.TipOptions).Get(BrushTipOptions.Size));
        Assert.Equal((foreground, background), (_nextRun.Colors.Foreground, _nextRun.Colors.Background));

        _nextRun.ViewModel.SelectToolGroup(BuiltInToolGroups.Eraser.Id);
        Assert.Equal(SoftEraserTool.Id, _nextRun.ViewModel.SelectedTool.Descriptor.Id);
    }

    [Fact]
    public void WithNothingSaved_NothingChanges()
    {
        _canvas.Colors.Set(CellColor.FromAnsi(AnsiColor.Blue), CellColor.Unset);

        new RememberedToolState(_settings).Restore(_canvas.ViewModel);

        Assert.Equal(CellColor.FromAnsi(AnsiColor.Blue), _canvas.Colors.Foreground);
        Assert.Empty(_canvas.ViewModel.LastToolOfGroup);
    }

    [Fact]
    public void DamagedSavedState_IsIgnored()
    {
        _settings.SetText(RememberedToolState.ToolOptionsKey, "{ not json");
        _settings.SetText(RememberedToolState.LastToolsKey, "[1, 2]");
        _settings.SetText(RememberedToolState.ColorsKey, "{ \"foreground\": \"plaid\", \"background\": 3 }");

        new RememberedToolState(_settings).Restore(_canvas.ViewModel);

        Assert.Equal(TextTool.Id, _canvas.ViewModel.SelectedTool.Descriptor.Id);
        Assert.Empty(_canvas.ViewModel.LastToolOfGroup);
        Assert.Equal((CellColor.Unset, CellColor.Unset), (_canvas.Colors.Foreground, _canvas.Colors.Background));
    }

    [Fact]
    public void ASavedLastTool_ThatIsntInItsGroup_IsIgnored()
    {
        _canvas.ViewModel.RestoreLastTools(new Dictionary<string, string>
        {
            [BuiltInToolGroups.Eraser.Id] = BrushTool.Id,
            [BuiltInToolGroups.Brush.Id] = ColorBrushTool.BackgroundId,
            ["no.such.group"] = BrushTool.Id,
        });

        Assert.Equal([(BuiltInToolGroups.Brush.Id, ColorBrushTool.BackgroundId)],
            _canvas.ViewModel.LastToolOfGroup.Select(pair => (pair.Key, pair.Value)));
    }
}
