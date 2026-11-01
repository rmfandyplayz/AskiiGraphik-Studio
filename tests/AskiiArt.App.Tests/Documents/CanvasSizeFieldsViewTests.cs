// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Documents;
using AskiiArt.Core.Documents;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Documents;

/// <summary>Typing into the width box of the New and Resize Canvas dialogs.</summary>
public sealed class CanvasSizeFieldsViewTests : IDisposable
{
    private readonly CanvasSizeFields _fields = new(new CanvasSize(80, 25));
    private readonly Window _window;
    private readonly TextBox _widthText;

    public CanvasSizeFieldsViewTests()
    {
        var view = new CanvasSizeFieldsView { DataContext = _fields };
        _window = new Window { Content = view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();

        var widthBox = view.GetVisualDescendants().OfType<NumericUpDown>().Single(box => box.Name == "WidthBox");
        _widthText = widthBox.GetVisualDescendants().OfType<TextBox>().Single();
    }

    [AvaloniaFact]
    public void TypingMoreThanTheMaximum_ClampsToTheMaximum()
    {
        Retype("99999");

        Assert.Equal(CanvasSize.MaximumSide, _fields.Width);
        LeaveTheBox();
        Assert.Equal(CanvasSize.MaximumSide.ToString(CultureInfo.CurrentCulture), _widthText.Text);
    }

    [AvaloniaFact]
    public void TypingLessThanTheMinimum_ClampsToTheMinimum()
    {
        Retype("0");

        Assert.Equal(CanvasSize.MinimumSide, _fields.Width);
        LeaveTheBox();
        Assert.Equal(CanvasSize.MinimumSide.ToString(CultureInfo.CurrentCulture), _widthText.Text);
    }

    [AvaloniaFact]
    public void TypingAZeroFirst_StillLetsTheNumberBeTypedInFull()
    {
        // The value is clamped, but not the text being typed, so "0" doesn't turn into "1".
        Retype("05");

        Assert.Equal(5, _fields.Width);
    }

    [AvaloniaFact]
    public void ClearingTheBox_LeavesNoSize()
    {
        Retype(string.Empty);

        Assert.Null(_fields.Width);
        Assert.False(_fields.IsValid);
    }

    public void Dispose() => _window.Close();

    private void Retype(string text)
    {
        _widthText.Focus();
        _widthText.SelectAll();
        _window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        foreach (var character in text)
            _window.KeyTextInput(character.ToString());

        Dispatcher.UIThread.RunJobs();
    }

    private void LeaveTheBox()
    {
        _window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
}
