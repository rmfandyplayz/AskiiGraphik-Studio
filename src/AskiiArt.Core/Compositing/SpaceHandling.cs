// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Compositing;

/// <summary>How compositing treats an occupied cell that holds a space (ARCHITECTURE §5.4).</summary>
public enum SpaceHandling
{
    /// <summary>A space is a deliberate blank that hides the layers below it. The default.</summary>
    HidesLayersBelow,

    /// <summary>A space counts as empty, so the layers below show through it.</summary>
    TreatedAsEmpty
}
