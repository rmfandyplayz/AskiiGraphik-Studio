// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections;
using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;

namespace AskiiArt.App.Tests.Resources;

public sealed class StringsTests
{
    [Fact]
    public void EveryString_HasText()
    {
        var resources = Strings.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        Assert.NotNull(resources);

        foreach (DictionaryEntry entry in resources)
            Assert.False(string.IsNullOrWhiteSpace(entry.Value as string), $"The string '{entry.Key}' is empty.");
    }

    [Fact]
    public void EveryShellAction_HasALabelInStringsResx()
    {
        var registry = new ActionRegistry([new ShellActions(new FakeAppShell(), new AppDataLocations("root"), includeDebugActions: true)]);

        foreach (var action in registry.All)
            Assert.False(string.IsNullOrWhiteSpace(UiText.Get(action.LabelKey)), action.Id);
    }

    [Fact]
    public void UiTextGet_WithAMissingKey_ThrowsNamingTheKey()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() => UiText.Get("NoSuchString"));

        Assert.Contains("NoSuchString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AppDisplayName_IsTheCurrentProductName()
    {
        Assert.Equal("AskiiGraphiks Studio", Strings.AppDisplayName);
    }
}
