// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// Every tool group's option values, shared by every document as the colours are (ADR 0047). A group's values are
/// made the first time the group is asked for.
/// </summary>
public sealed class ToolSettings
{
    private readonly Dictionary<string, ToolOptionValues> _valuesByGroupId = new(StringComparer.Ordinal);

    /// <summary>The values of <paramref name="group"/>.</summary>
    /// <exception cref="ArgumentException">Another group with the same id was asked for before.</exception>
    public ToolOptionValues For(ToolOptionGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (_valuesByGroupId.TryGetValue(group.Id, out var values))
        {
            if (!ReferenceEquals(values.Group, group))
                throw new ArgumentException($"Two option groups have the id '{group.Id}'.", nameof(group));

            return values;
        }

        values = new ToolOptionValues(group);
        _valuesByGroupId.Add(group.Id, values);
        return values;
    }
}
