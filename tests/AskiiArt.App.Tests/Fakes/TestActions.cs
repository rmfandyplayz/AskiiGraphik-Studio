// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Hosting;
using AskiiArt.App.Shell;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Every action provider the real app registers, wired to fakes and a test canvas.</summary>
internal static class TestActions
{
    public static IActionProvider[] CreateProviders(TestCanvas canvas, FakeAppShell shell,
        FakeDocumentDialogs? dialogs = null, bool includeDebugActions = false) =>
    [
        new ShellActions(shell, new AppDataLocations("root"), includeDebugActions),
        new CanvasActions(canvas.ViewModel),
        new EditActions(canvas.ActiveDocument),
        new DocumentActions(canvas.ActiveDocument, dialogs ?? new FakeDocumentDialogs(), shell)
    ];
}
