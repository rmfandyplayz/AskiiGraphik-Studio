// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>A straight piece of the selection's outline, in device pixels, drawn dashed along its length.</summary>
/// <param name="Rect">The strip, along the inside of the selected cells.</param>
/// <param name="IsHorizontal">True when it runs across, along a row; false when it runs down a column.</param>
internal readonly record struct SelectionStrip(PixelRect Rect, bool IsHorizontal);
