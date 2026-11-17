// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Eraser;

/// <summary>What a hard eraser takes away from the cells under its tip (ADR 0050).</summary>
public enum EraserTarget
{
    /// <summary>The glyph, its colour and the background: the cell becomes empty. The Eraser.</summary>
    Everything,

    /// <summary>Only the background, so the glyph and its colour stay. The BG Eraser.</summary>
    Background
}
