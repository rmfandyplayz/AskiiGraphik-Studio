// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>
/// The user's ramps as choices of the Ramp option, after the built-in ones (ADR 0052), each with its characters shown
/// beside its name.
/// </summary>
internal sealed class UserRampChoices : IToolOptionChoiceSource
{
    private readonly RampLibrary _ramps;

    public UserRampChoices(RampLibrary ramps)
    {
        _ramps = ramps;
        Choices = CreateChoices();
        ramps.Changed += OnRampsChanged;
    }

    public event EventHandler? Changed;

    public string Id => RampOptions.UserRampsSourceId;

    public IReadOnlyList<ToolOptionChoice> Choices { get; private set; }

    private ToolOptionChoice[] CreateChoices() =>
    [
        .. _ramps.UserRamps.Select(ramp =>
            new ToolOptionChoice(ramp.Id, NameKey: "") { Name = ramp.Name, Detail = ramp.Ramp.ToString() })
    ];

    private void OnRampsChanged(object? sender, EventArgs e)
    {
        Choices = CreateChoices();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
