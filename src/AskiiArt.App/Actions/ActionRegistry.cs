// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Actions;

/// <summary>
/// Every action in the app, looked up by id.
/// </summary>
internal sealed class ActionRegistry
{
    private readonly Dictionary<string, AppAction> _actionsById = new(StringComparer.Ordinal);

    public ActionRegistry(IEnumerable<IActionProvider> providers)
    {
        foreach (var provider in providers)
        {
            foreach (var action in provider.CreateActions())
            {
                if (!_actionsById.TryAdd(action.Id, action))
                    throw new InvalidOperationException($"Two actions were registered with the id '{action.Id}'.");
            }
        }
    }

    public IReadOnlyCollection<AppAction> All => _actionsById.Values;

    public AppAction Get(string id)
    {
        if (_actionsById.TryGetValue(id, out var action))
            return action;

        throw new KeyNotFoundException($"No action is registered with the id '{id}'.");
    }

    public bool Contains(string id) => _actionsById.ContainsKey(id);
}
