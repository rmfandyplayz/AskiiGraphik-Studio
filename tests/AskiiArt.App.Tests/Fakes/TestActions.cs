// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Hosting;
using AskiiArt.App.Layers;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Every action provider the real app registers, wired to fakes and a test canvas.</summary>
internal static class TestActions
{
    public static IActionProvider[] CreateProviders(TestCanvas canvas, FakeAppShell shell,
        FakeDocumentDialogs? dialogs = null, bool includeDebugActions = false, bool isMacOS = false,
        LayersPanelViewModel? layers = null) =>
    [
        new ShellActions(shell, new AppDataLocations("root"), includeDebugActions, isMacOS),
        new CanvasActions(canvas.ViewModel),
        new ToolActions(canvas.ViewModel),
        new EditActions(canvas.ActiveDocument, canvas.TextExporter, shell),
        new WindowMenuActions(new ShellDocking(new FakeAppSettings(), NullLogger<ShellDocking>.Instance)),
        new ToolbarMenuActions(new ShellDocking(new FakeAppSettings(), NullLogger<ShellDocking>.Instance)),
        new SubToolbarSampleActions(new ShellDocking(new FakeAppSettings(), NullLogger<ShellDocking>.Instance),
            includeDebugActions),
        new LayerActions(layers ?? new LayersPanelViewModel(canvas.ActiveDocument)),
        CreateDocumentActions(canvas, shell, dialogs ?? new FakeDocumentDialogs(), includeDebugActions)
    ];

    public static DocumentActions CreateDocumentActions(TestCanvas canvas, FakeAppShell shell,
        FakeDocumentDialogs dialogs, bool includeDebugActions = false) =>
        new(canvas.ActiveDocument, canvas.CreateDocumentFiles(dialogs, shell), dialogs, shell, includeDebugActions);
}
