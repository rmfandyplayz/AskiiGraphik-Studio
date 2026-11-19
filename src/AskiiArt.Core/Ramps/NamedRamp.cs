// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>A ramp with the name it was saved under, as a ramp file holds it (<see cref="RampFile"/>).</summary>
/// <param name="Name">The name, never blank.</param>
/// <param name="Ramp">The ramp.</param>
public sealed record NamedRamp(string Name, Ramp Ramp);
