// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>A dock window's tab strip on its own, too narrow for its tabs, so it scrolls.</summary>
public sealed class DockTabStripTests : IDisposable
{
    private const double FadeLength = 10;
    private const double Tolerance = 0.5;

    private static readonly DockTab[] _tabs =
        [.. "abcdef".Select(letter => new DockTab(letter.ToString(), letter.ToString()))];

    private DockTabStrip _strip = null!;
    private Window? _window;

    public void Dispose()
    {
        _window?.Close();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheIncomingTab_IsAsLongAsATabWithTheSameTitle()
    {
        ShowStrip();
        _strip.ShowIncomingTab(0, "a", new Border());
        Pump();

        Assert.Equal(_strip.TabFor("a")!.Bounds.Width, _strip.IncomingTab!.Bounds.Width);
    }

    [AvaloniaFact]
    public void PastTheEndOfTheStrip_TheIncomingTabScrollsIntoView_AndTheStripScrollsBackWhenItGoes()
    {
        ShowStrip();
        Assert.Equal(0, _strip.ScrollOffset);

        _strip.ShowIncomingTab(_tabs.Length, "g", new Border());
        Pump();
        Assert.True(_strip.ScrollOffset > 0);

        _strip.HideIncomingTab();
        Pump();
        Assert.Null(_strip.IncomingTab);
        Assert.Equal(0, _strip.ScrollOffset);
    }

    [AvaloniaFact]
    public void TheIncomingTab_StaysWhereItIs_WhenTheTabsChange()
    {
        ShowStrip();
        var marker = new Border();
        _strip.ShowIncomingTab(1, "g", marker);

        _strip.SetTabs(_tabs, "b");
        Pump();

        var tabs = Assert.IsAssignableFrom<Panel>(_strip.IncomingTab!.Parent);
        Assert.Equal(1, tabs.Children.IndexOf(_strip.IncomingTab));
        Assert.Same(_strip.IncomingTab, marker.Parent);
    }

    [AvaloniaFact]
    public void HidingTheIncomingTab_GivesBackItsMarker()
    {
        ShowStrip();
        var marker = new Border();
        _strip.ShowIncomingTab(0, "g", marker);

        _strip.HideIncomingTab();

        Assert.Null(marker.Parent);
    }

    [AvaloniaFact]
    public void ATabChosenBeforeTheStripIsLaidOut_IsScrolledIntoViewOnceItIs()
    {
        ShowStrip(selected: "f");

        Assert.True(_strip.ScrollOffset > 0);
        AssertClearOfTheFades("f");
    }

    [AvaloniaFact]
    public void ChoosingATabUnderTheFade_ScrollsItClearOfIt()
    {
        ShowStrip();
        var underTheFade = _tabs.First(tab => TabEnd(tab.WindowId) > _strip.Bounds.Width - FadeLength).WindowId;

        _strip.SetTabs(_tabs, underTheFade);
        Pump();

        AssertClearOfTheFades(underTheFade);
    }

    [AvaloniaFact]
    public void ChoosingATab_StartsFromWhereTheStripShowingBeforeWas_WhenTheTabIsClearThere()
    {
        ShowStrip();
        var clearOfTheStartFade = _strip.TabFor("c")!.Bounds.X - FadeLength;

        _strip.SetTabs(_tabs, "c", scrolledTo: clearOfTheStartFade);
        Pump();

        Assert.Equal(clearOfTheStartFade, _strip.ScrollOffset, Tolerance);
    }

    [AvaloniaFact]
    public void AfterTheIncomingTabGoes_TheStripStaysWhereItWas_EvenBeforeItsBeenLaidOut()
    {
        ShowStrip(selected: "f");
        var scrolled = _strip.ScrollOffset;

        _strip.ShowIncomingTab(0, "g", new Border());
        _strip.HideIncomingTab();
        Pump();

        Assert.Equal(scrolled, _strip.ScrollOffset, Tolerance);
    }

    private double TabEnd(string windowId) => _strip.TabFor(windowId)!.Bounds.Right - _strip.ScrollOffset;

    // Inside the strip, and clear of a fade on each side that has more to scroll to.
    private void AssertClearOfTheFades(string windowId)
    {
        var tab = _strip.TabFor(windowId)!.Bounds;
        var offset = _strip.ScrollOffset;
        var viewport = _strip.Bounds.Width;
        var isAtTheEnd = offset + viewport >= _strip.TabLengths.Values.Sum() - Tolerance;
        var startFade = offset > Tolerance ? FadeLength : 0;
        var endFade = isAtTheEnd ? 0 : FadeLength;

        Assert.True(tab.X - offset >= startFade - Tolerance, $"{windowId} starts at {tab.X - offset}.");
        Assert.True(tab.Right - offset <= viewport - endFade + Tolerance, $"{windowId} ends at {tab.Right - offset}.");
    }

    // On the UI thread, which the test runs on but its constructor doesn't.
    private void ShowStrip(string selected = "a")
    {
        _strip = new DockTabStrip { FadeLength = FadeLength };
        _strip.SetTabs(_tabs, selected);
        _window = new Window { Width = 80, Height = 40, Content = _strip };
        _window.Show();
        Pump();
    }

    private static void Pump()
    {
        for (var pass = 0; pass < 5; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
