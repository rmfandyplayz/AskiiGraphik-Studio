// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Errors;
using AskiiArt.App.Hosting;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using Microsoft.Extensions.DependencyInjection;

namespace AskiiArt.App.Tests.Hosting;

public sealed class AppServicesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "AskiiArt.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void CompositionRoot_ResolvesEverythingTheAppNeedsAtStartup()
    {
        using var services = AppServices.Create(new AppDataLocations(_root));

        Assert.NotNull(services.GetRequiredService<UnexpectedErrorHandler>());
        Assert.NotNull(services.GetRequiredService<MainWindowBuilder>());

        var actions = services.GetRequiredService<ActionRegistry>();
        Assert.True(actions.Contains(ShellActionIds.Quit));
        Assert.True(actions.Contains(ShellActionIds.OpenLogFolder));
        Assert.True(actions.Contains(ShellActionIds.About));
    }

    [Fact]
    public void CompositionRoot_RegistersEveryActionTheMenusReferTo()
    {
        using var services = AppServices.Create(new AppDataLocations(_root));
        var actions = services.GetRequiredService<ActionRegistry>();
        var layout = services.GetRequiredService<MenuLayout>();

        Assert.All(layout.ActionIds(), actionId => Assert.True(actions.Contains(actionId), actionId));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
