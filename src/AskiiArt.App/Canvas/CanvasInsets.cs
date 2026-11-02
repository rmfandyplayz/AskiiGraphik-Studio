// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas;

/// <summary>
/// How much of each edge of the canvas viewport docked widgets cover, in device pixels (ADR 0025).
/// </summary>
internal readonly record struct CanvasInsets(int Left, int Top, int Right, int Bottom);
