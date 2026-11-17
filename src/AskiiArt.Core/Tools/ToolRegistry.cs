// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools;

/// <summary>
/// A tool that can be created for any editing session. Tools are made per session because
/// they keep per-document state, such as the text caret.
/// </summary>
public sealed record ToolDefinition(ToolDescriptor Descriptor, Func<ToolContext, ITool> Create)
{
    /// <summary>
    /// The options the Tool Options toolbar shows for this tool, group by group, in order (ADR 0047, ADR 0049). Tools
    /// that list the same <see cref="ToolOptionGroup"/> share its values, as the Brush group's tools share their tip.
    /// </summary>
    public IReadOnlyList<ToolOptionGroup> OptionGroups { get; init; } = [];

    /// <summary>
    /// Why the tool can't be used on a document now, as the App's resource key for a warning the Tool Options
    /// toolbar shows, or null when it can. It's given the tools' option values and the document. Null for a tool
    /// that never warns.
    /// </summary>
    public Func<ToolSettings, Document, string?>? WarningFor { get; init; }
}

/// <summary>
/// Every available tool and tool group, registered through DI (ARCHITECTURE §8.1). Built-in tools register exactly
/// the way a future plugin would (§18).
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, ToolDefinition> _definitionsById = new(StringComparer.Ordinal);
    private readonly List<ToolDefinition> _definitions = [];
    private readonly Dictionary<string, ToolGroup> _groupsById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ToolGroup> _groupsByToolId = new(StringComparer.Ordinal);
    private readonly List<ToolGroup> _groups = [];

    /// <summary>
    /// Creates a registry holding <paramref name="definitions"/>, grouped by <paramref name="groups"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Two definitions, two groups or two groups' letters are the same, a group is empty or names a tool that isn't
    /// registered, or a tool is in no group or in more than one.
    /// </exception>
    public ToolRegistry(IEnumerable<ToolDefinition> definitions, IEnumerable<ToolGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(groups);

        foreach (var definition in definitions)
        {
            if (!_definitionsById.TryAdd(definition.Descriptor.Id, definition))
                throw new ArgumentException($"Two tools have the id '{definition.Descriptor.Id}'.", nameof(definitions));

            _definitions.Add(definition);
        }

        foreach (var group in groups)
            AddGroup(group);

        if (_definitions.FirstOrDefault(tool => !_groupsByToolId.ContainsKey(tool.Descriptor.Id)) is { } loose)
            throw new ArgumentException($"The tool '{loose.Descriptor.Id}' is in no group.", nameof(groups));
    }

    /// <summary>The tools, in registration order.</summary>
    public IReadOnlyList<ToolDefinition> All => _definitions;

    /// <summary>The tool groups, in registration order, which is the order the Tools toolbar shows them.</summary>
    public IReadOnlyList<ToolGroup> Groups => _groups;

    /// <summary>Returns the tool with <paramref name="id"/>.</summary>
    /// <exception cref="KeyNotFoundException">No tool has <paramref name="id"/>.</exception>
    public ToolDefinition Get(string id)
    {
        if (_definitionsById.TryGetValue(id, out var definition))
            return definition;

        throw new KeyNotFoundException($"No tool has the id '{id}'.");
    }

    /// <summary>Returns the group with <paramref name="id"/>.</summary>
    /// <exception cref="KeyNotFoundException">No group has <paramref name="id"/>.</exception>
    public ToolGroup GetGroup(string id)
    {
        if (_groupsById.TryGetValue(id, out var group))
            return group;

        throw new KeyNotFoundException($"No tool group has the id '{id}'.");
    }

    /// <summary>Returns the group the tool with <paramref name="toolId"/> is in.</summary>
    /// <exception cref="KeyNotFoundException">No tool has <paramref name="toolId"/>.</exception>
    public ToolGroup GroupOf(string toolId)
    {
        if (_groupsByToolId.TryGetValue(toolId, out var group))
            return group;

        throw new KeyNotFoundException($"No tool has the id '{toolId}'.");
    }

    private void AddGroup(ToolGroup group)
    {
        if (!_groupsById.TryAdd(group.Id, group))
            throw new ArgumentException($"Two tool groups have the id '{group.Id}'.", nameof(group));

        if (group.ToolIds.Count == 0)
            throw new ArgumentException($"The tool group '{group.Id}' has no tools.", nameof(group));

        if (group.ShortcutLetter is { } letter && _groups.Any(other => other.ShortcutLetter == letter))
            throw new ArgumentException($"Two tool groups have the letter '{letter}'.", nameof(group));

        foreach (var toolId in group.ToolIds)
        {
            if (!_definitionsById.ContainsKey(toolId))
            {
                throw new ArgumentException($"The tool group '{group.Id}' names '{toolId}', which isn't a tool.",
                    nameof(group));
            }

            if (!_groupsByToolId.TryAdd(toolId, group))
                throw new ArgumentException($"The tool '{toolId}' is in two groups.", nameof(group));
        }

        _groups.Add(group);
    }
}
