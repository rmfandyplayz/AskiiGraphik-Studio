// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// How a layer combines with the layers below it. Reserved (ARCHITECTURE §18): v1 has only
/// <see cref="Normal"/>.
/// </summary>
public enum LayerBlendMode
{
    /// <summary>The topmost visible cell wins (ARCHITECTURE §5.6).</summary>
    Normal = 0
}
