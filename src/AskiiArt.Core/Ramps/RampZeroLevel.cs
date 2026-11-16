// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>What a ramp paints at intensity 0, where there's no ink (ARCHITECTURE §7).</summary>
public enum RampZeroLevel
{
    /// <summary>An empty cell, which shows the layers below.</summary>
    Empty = 0,

    /// <summary>A space, which covers the layers below unless its layer treats spaces as empty (ADR 0033).</summary>
    Space = 1
}
