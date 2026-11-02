// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Formats;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell;

public sealed class MainWindowLifetimeTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly FakeAppSettings _settings = new();
    private readonly TestCanvas _canvas = new();
    private readonly List<MainWindowLifetime> _lifetimes = [];
    private readonly ActionRegistry _actions;

    public MainWindowLifetimeTests()
    {
        _actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs, isMacOS: true));
    }

    public void Dispose()
    {
        foreach (var lifetime in _lifetimes)
            lifetime.Window?.Close();

        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void ShowWindow_KeepsToOneWindow()
    {
        var lifetime = CreateLifetime(keepsRunningWithoutWindow: false);

        var first = lifetime.ShowWindow();
        var second = lifetime.ShowWindow();

        Assert.Same(first, second);
        Assert.Same(first, lifetime.Window);
        Assert.True(first.IsVisible);
    }

    [AvaloniaFact]
    public void KeepingRunning_ClosingTheWindowClosesItsDocument_AndTheNextWindowStartsEmpty()
    {
        var lifetime = CreateLifetime(keepsRunningWithoutWindow: true);
        var changes = 0;
        lifetime.WindowChanged += (_, _) => changes++;
        var window = lifetime.ShowWindow();
        var file = new DocumentFile(Path.Combine("folder", "art.asciiart"), DocumentFileVariant.Plain);
        _canvas.ActiveDocument.SetFile(file);
        var closedSession = _canvas.ActiveDocument.Session;

        window.Close();

        Assert.Null(lifetime.Window);
        Assert.NotSame(closedSession, _canvas.ActiveDocument.Session);
        Assert.Null(_canvas.ActiveDocument.File);
        Assert.Equal(NewDocumentSettings.Default.Size, _canvas.Document.CanvasSize);

        Assert.NotSame(window, lifetime.ShowWindow());
        Assert.Equal(3, changes);
    }

    [AvaloniaFact]
    public void QuittingWithTheWindow_ClosingItLeavesTheDocumentAlone()
    {
        var lifetime = CreateLifetime(keepsRunningWithoutWindow: false);
        var window = lifetime.ShowWindow();
        var session = _canvas.ActiveDocument.Session;

        window.Close();

        Assert.Null(lifetime.Window);
        Assert.Same(session, _canvas.ActiveDocument.Session);
    }

    [AvaloniaFact]
    public void RunInWindow_WithNoWindow_OpensOneFirst_ThenRunsTheAction()
    {
        var lifetime = CreateLifetime(keepsRunningWithoutWindow: true);
        var windowsWhenRun = new List<MainWindow?>();
        var action = new AppAction("test.recordWindow", nameof(Strings.ActionNew),
            new RelayCommand(() => windowsWhenRun.Add(lifetime.Window)));

        lifetime.RunInWindow(action);
        Assert.Empty(windowsWhenRun);
        Dispatcher.UIThread.RunJobs();

        var window = Assert.Single(windowsWhenRun);
        Assert.NotNull(window);

        // With a window open, it runs straight away.
        lifetime.RunInWindow(action);
        Assert.Equal([window, window], windowsWhenRun);
    }

    private MainWindowLifetime CreateLifetime(bool keepsRunningWithoutWindow)
    {
        var builder = new MainWindowBuilder(_actions, MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false),
            ToolbarLayout.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell,
            new ShellDock(_settings, NullLogger<ShellDock>.Instance), _settings);
        var lifetime = new MainWindowLifetime(builder, _canvas.ActiveDocument, KeyModifiers.Meta,
            keepsRunningWithoutWindow);
        _lifetimes.Add(lifetime);
        return lifetime;
    }
}
