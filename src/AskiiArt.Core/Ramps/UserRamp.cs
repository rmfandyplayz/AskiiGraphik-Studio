// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Ramps;

/// <summary>A ramp the user made, generated or imported, kept in a <see cref="RampLibrary"/> (ADR 0052).</summary>
/// <param name="Id">A stable id, which the Ramp option saves. Never shown to the user.</param>
/// <param name="Name">The name the user sees, unique among the library's ramps.</param>
/// <param name="Ramp">The ramp itself.</param>
public sealed record UserRamp(string Id, string Name, Ramp Ramp) : IUserItem;
