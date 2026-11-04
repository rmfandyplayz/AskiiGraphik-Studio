// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas;

/// <summary>
/// How much of each edge of the canvas viewport windows and toolbars cover, in device pixels (ADR 0035).
/// </summary>
internal readonly record struct CanvasInsets(int Left, int Top, int Right, int Bottom);
