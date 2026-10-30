// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Actions;

public sealed class ActionRegistryTests
{
    [Fact]
    public void Get_ReturnsTheActionRegisteredWithThatId()
    {
        var action = CreateAction("test.first");
        var registry = new ActionRegistry([new ListActionProvider(action)]);

        Assert.Same(action, registry.Get("test.first"));
        Assert.True(registry.Contains("test.first"));
    }

    [Fact]
    public void Constructor_CollectsActionsFromEveryProvider()
    {
        var registry = new ActionRegistry(
        [
            new ListActionProvider(CreateAction("test.first")),
            new ListActionProvider(CreateAction("test.second")),
        ]);

        Assert.Equal(["test.first", "test.second"], registry.All.Select(action => action.Id).Order());
    }

    [Fact]
    public void Constructor_RejectsTwoActionsWithTheSameId()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ActionRegistry(
        [
            new ListActionProvider(CreateAction("test.duplicate")),
            new ListActionProvider(CreateAction("test.duplicate")),
        ]));

        Assert.Contains("test.duplicate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_WithAnUnknownId_ThrowsNamingTheId()
    {
        var registry = new ActionRegistry([]);

        var exception = Assert.Throws<KeyNotFoundException>(() => registry.Get("test.missing"));

        Assert.Contains("test.missing", exception.Message, StringComparison.Ordinal);
        Assert.False(registry.Contains("test.missing"));
    }

    private static AppAction CreateAction(string id)
    {
        return new AppAction(id, nameof(Strings.ActionQuit), new RelayCommand(() => { }));
    }

    private sealed class ListActionProvider(params AppAction[] actions) : IActionProvider
    {
        public IEnumerable<AppAction> CreateActions() => actions;
    }
}
