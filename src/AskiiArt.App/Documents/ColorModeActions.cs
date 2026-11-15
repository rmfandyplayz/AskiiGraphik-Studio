// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Documents;

/// <summary>
/// The temporary Document menu items that change the active document's colour mode (ADR 0036), each ticked
/// while it's the document's mode. A change that would make cells look different asks first, and either way
/// it's one undo step.
/// </summary>
internal sealed class ColorModeActions(ActiveDocument activeDocument, IAppShell shell) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var modes = new (string Id, string LabelKey, ColorMode Mode)[]
        {
            (ColorModeActionIds.Monochrome, nameof(Strings.ActionColorModeMonochrome), ColorMode.Monochrome),
            (ColorModeActionIds.Ansi, nameof(Strings.ActionColorModeAnsi), ColorMode.Ansi),
            (ColorModeActionIds.Rgb, nameof(Strings.ActionColorModeRgb), ColorMode.Rgb),
        };
        var states = modes.Select(mode => new ActionCheckedState(() => Document.ColorMode == mode.Mode)).ToArray();

        // New brings another document, so the check marks follow whichever document is current.
        var followedDocument = Document;
        followedDocument.Changed += OnDocumentChanged;
        activeDocument.SessionChanged += (_, _) =>
        {
            followedDocument.Changed -= OnDocumentChanged;
            followedDocument = Document;
            followedDocument.Changed += OnDocumentChanged;
            NotifyAll();
        };

        for (var index = 0; index < modes.Length; index++)
        {
            var (id, labelKey, mode) = modes[index];
            var state = states[index];
            yield return new AppAction(id, labelKey, new AsyncRelayCommand(() => SwitchAsync(mode, state)),
                checkedState: state);
        }

        void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            if (e.SettingsChanged)
                NotifyAll();
        }

        void NotifyAll()
        {
            foreach (var state in states)
                state.NotifyChanged();
        }
    }

    private Document Document => activeDocument.Session.Document;

    private async Task SwitchAsync(ColorMode colorMode, ActionCheckedState state)
    {
        var session = activeDocument.Session;
        if (session.Document.ColorMode == colorMode)
        {
            // A menu may have unticked the item; tick it again.
            state.NotifyChanged();
            return;
        }

        var changedCount = SetColorModeCommand.CountCellsThatLookDifferent(session.Document, colorMode);
        if (changedCount > 0 && !await ConfirmAsync(colorMode, changedCount))
        {
            state.NotifyChanged();
            return;
        }

        session.History.Execute(new SetColorModeCommand(colorMode));
    }

    // Only monochrome and ANSI can change how a cell looks: RGB holds every ANSI colour exactly.
    private Task<bool> ConfirmAsync(ColorMode colorMode, int changedCount)
    {
        var removes = colorMode == ColorMode.Monochrome;
        var format = removes ? Strings.ColorModeRemoveMessageFormat : Strings.ColorModeConvertMessageFormat;
        var confirmLabel = removes ? Strings.ColorModeConfirmRemove : Strings.ColorModeConfirmConvert;
        var message = string.Format(CultureInfo.CurrentCulture, format, changedCount);
        return shell.ConfirmAsync(Strings.ColorModeConfirmTitle, message, confirmLabel);
    }
}
