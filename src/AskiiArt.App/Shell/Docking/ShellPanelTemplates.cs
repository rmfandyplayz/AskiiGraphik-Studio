// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Mvvm.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Turns a panel in the layout into its view, by the panel's id. Panels don't know where they're
/// hosted (ARCHITECTURE §14.4): the same view works docked, tabbed or floating.
/// </summary>
internal sealed class ShellPanelTemplates(IReadOnlyDictionary<string, Func<Control>> views) : IDataTemplate
{
    public bool Match(object? data) => data is Tool { Id: { } id } && views.ContainsKey(id);

    public Control? Build(object? param) =>
        param is Tool { Id: { } id } && views.TryGetValue(id, out var view) ? view() : null;
}
