// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// Every tool group's option values, shared by every document as the colours are (ADR 0047), and the ramps the Ramp
/// option chooses among (ADR 0052). A group's values are made the first time the group is asked for.
/// </summary>
public sealed class ToolSettings
{
    private readonly Dictionary<string, ToolOptionValues> _valuesByGroupId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IToolOptionChoiceSource> _choiceSources = new(StringComparer.Ordinal);

    // Saved values of groups nobody has asked for yet, by group id, kept so saving again doesn't lose them.
    private readonly Dictionary<string, JsonObject> _savedByGroupId = new(StringComparer.Ordinal);

    /// <summary>Creates settings whose ramps are only the built-in ones.</summary>
    public ToolSettings()
        : this(new RampLibrary())
    {
    }

    /// <summary>Creates settings whose Ramp option chooses among <paramref name="ramps"/>.</summary>
    public ToolSettings(RampLibrary ramps)
    {
        ArgumentNullException.ThrowIfNull(ramps);

        Ramps = ramps;
        var userRamps = new UserRampChoices(ramps);
        _choiceSources.Add(userRamps.Id, userRamps);
    }

    /// <summary>The built-in ramps and the user's, which the Ramp option chooses among.</summary>
    public RampLibrary Ramps { get; }

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

        values = new ToolOptionValues(group, _choiceSources);
        if (_savedByGroupId.Remove(group.Id, out var saved))
            values.Restore(saved);

        _valuesByGroupId.Add(group.Id, values);
        return values;
    }

    /// <summary>
    /// Every group's values as JSON, to save between runs: an object with a property for each group, by id. Values
    /// restored for groups that haven't been asked for since are kept as they were.
    /// </summary>
    public string ToJson()
    {
        var json = new JsonObject();
        foreach (var (groupId, saved) in _savedByGroupId)
            json[groupId] = saved.DeepClone();

        foreach (var (groupId, values) in _valuesByGroupId)
            json[groupId] = values.ToJson();

        return json.ToJsonString();
    }

    /// <summary>
    /// Sets the values <paramref name="json"/>, made by <see cref="ToJson"/>, holds: now for groups already asked for,
    /// and when they're first asked for for the rest. Anything that can't be read is ignored, so the option keeps its
    /// value.
    /// </summary>
    public void Restore(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonObject? groups;
        try
        {
            groups = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return;
        }

        if (groups is null)
            return;

        foreach (var (groupId, node) in groups)
        {
            if (node is not JsonObject saved)
                continue;

            if (_valuesByGroupId.TryGetValue(groupId, out var values))
                values.Restore(saved);
            else
                _savedByGroupId[groupId] = (JsonObject)saved.DeepClone();
        }
    }
}
