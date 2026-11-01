// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools;

/// <summary>
/// A tool that can be created for any editing session. Tools are made per session because
/// they keep per-document state, such as the text caret.
/// </summary>
public sealed record ToolDefinition(ToolDescriptor Descriptor, Func<EditingSession, ITool> Create);

/// <summary>
/// Every available tool, registered through DI (ARCHITECTURE §8.1). Built-in tools register
/// exactly the way a future plugin would (§18).
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, ToolDefinition> _definitionsById = new(StringComparer.Ordinal);
    private readonly List<ToolDefinition> _definitions = [];

    /// <summary>Creates a registry holding <paramref name="definitions"/>.</summary>
    /// <exception cref="ArgumentException">Two definitions share an id.</exception>
    public ToolRegistry(IEnumerable<ToolDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (var definition in definitions)
        {
            if (!_definitionsById.TryAdd(definition.Descriptor.Id, definition))
                throw new ArgumentException($"Two tools have the id '{definition.Descriptor.Id}'.", nameof(definitions));

            _definitions.Add(definition);
        }
    }

    /// <summary>The tools, in registration order.</summary>
    public IReadOnlyList<ToolDefinition> All => _definitions;

    /// <summary>Returns the tool with <paramref name="id"/>.</summary>
    /// <exception cref="KeyNotFoundException">No tool has <paramref name="id"/>.</exception>
    public ToolDefinition Get(string id)
    {
        if (_definitionsById.TryGetValue(id, out var definition))
            return definition;

        throw new KeyNotFoundException($"No tool has the id '{id}'.");
    }
}
