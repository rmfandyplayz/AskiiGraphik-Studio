// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas.FrameRate;

/// <summary>
/// One frame the canvas painted: when it finished, and how long the canvas spent building and painting it, both in
/// <see cref="System.Diagnostics.Stopwatch"/> ticks.
/// </summary>
internal readonly record struct FrameTiming(long PaintedAt, long DrawTicks);
