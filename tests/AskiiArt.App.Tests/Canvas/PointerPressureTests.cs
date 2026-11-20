// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Headless.XUnit;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>Which pressure the tools get from each kind of pointer (ADR 0054).</summary>
public sealed class PointerPressureTests
{
    // Avalonia reports 0.5 for a mouse, which mustn't paint at half strength.
    [Theory]
    [InlineData(PointerType.Mouse)]
    [InlineData(PointerType.Touch)]
    public void AMouseOrTouch_PressesFully_WhateverItReports(PointerType type)
    {
        Assert.Equal(1f, PointerPressure.Of(type, 0.5f, isMouseActingAsPen: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APen_PressesAsHardAsItReports(bool isMouseActingAsPen)
    {
        Assert.Equal(0.3f, PointerPressure.Of(PointerType.Pen, 0.3f, isMouseActingAsPen));
    }

    [Theory]
    [InlineData(1.5f, 1f)]
    [InlineData(-0.2f, 0f)]
    [InlineData(float.NaN, 1f)]
    public void APensPressureOutside0To1_CountsAsTheNearestEnd_AndAnUnknownOneAsFull(float reported, float expected)
    {
        Assert.Equal(expected, PointerPressure.Of(PointerType.Pen, reported, isMouseActingAsPen: false));
    }

    [Fact]
    public void TheMouse_PressesAtHalfPressure_WhileItActsAsAPen()
    {
        Assert.Equal(0.5f, PointerPressure.Of(PointerType.Mouse, 0.5f, isMouseActingAsPen: true));
        Assert.Equal(1f, PointerPressure.Of(PointerType.Touch, 0.5f, isMouseActingAsPen: true));
    }

    [AvaloniaFact]
    public void MouseActsAsAPen_IsADebugActionWhoseCheckMarkFollowsTheCanvas()
    {
        using var canvas = new TestCanvas();
        var mouseActsAsPen = new CanvasActions(canvas.ViewModel, includeDebugActions: true).CreateActions()
            .Single(action => action.Id == CanvasActionIds.MouseActsAsPen);
        var checkMarkChanges = 0;
        mouseActsAsPen.CheckedState!.Changed += (_, _) => checkMarkChanges++;

        mouseActsAsPen.Command.Execute(null);

        Assert.True(canvas.ViewModel.IsMouseActingAsPen);
        Assert.True(mouseActsAsPen.CheckedState.IsChecked);
        Assert.Equal(1, checkMarkChanges);
        Assert.DoesNotContain(new CanvasActions(canvas.ViewModel, includeDebugActions: false).CreateActions(),
            action => action.Id == CanvasActionIds.MouseActsAsPen);
    }
}
