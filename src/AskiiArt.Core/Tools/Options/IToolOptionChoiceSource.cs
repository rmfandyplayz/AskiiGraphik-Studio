// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Options;

/// <summary>
/// Choices that a <see cref="ChoiceToolOption"/> offers besides its own, which can change while the app runs, such as
/// the user's ramps (ADR 0052). An option names its source by <see cref="Id"/>, and <see cref="ToolSettings"/> holds
/// the sources.
/// </summary>
public interface IToolOptionChoiceSource
{
    /// <summary>A stable id, for example "askiiart.userRamps". Never shown to the user.</summary>
    string Id { get; }

    /// <summary>The choices, in the order they're shown.</summary>
    IReadOnlyList<ToolOptionChoice> Choices { get; }

    /// <summary>Raised after the choices change: one is added, removed, renamed or changed.</summary>
    event EventHandler? Changed;
}
