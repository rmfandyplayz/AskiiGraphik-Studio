// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Ramps;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Ramps;

public sealed class RampActionsTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly TestCanvas _canvas = new();

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void TheDensitySample_IsOnlyThereInDebugBuilds()
    {
        Assert.Empty(CreateActions(includeDebugActions: false));
        Assert.Contains(CreateActions(includeDebugActions: true),
            action => action.Id == RampActionIds.OpenDensitySample);
    }

    [Fact]
    public async Task OpeningTheDensitySample_ReplacesTheDocument_KeepingItsFont()
    {
        var firaCode = new FontReference("Fira Code");
        _canvas.ActiveDocument.Replace(new EditingSession(
            Document.Create(new CanvasSize(2, 2), "Layer", CharacterSet.Ascii, firaCode)));
        var before = _canvas.Document;

        var action = CreateActions(includeDebugActions: true).Single();
        await ((IAsyncRelayCommand)action.Command).ExecuteAsync(null);

        Assert.NotSame(before, _canvas.Document);
        Assert.Equal(firaCode, _canvas.Document.Font);
        Assert.Equal(CharacterSet.Unicode, _canvas.Document.CharacterSet);
    }

    private List<AppAction> CreateActions(bool includeDebugActions) =>
        new RampActions(_canvas.ActiveDocument, _canvas.CreateDocumentFiles(new FakeDocumentDialogs(), _shell),
            _canvas.Fonts, _canvas.Densities, includeDebugActions).CreateActions().ToList();
}
