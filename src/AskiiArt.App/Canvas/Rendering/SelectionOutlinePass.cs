// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>One outline of a frame's selection display, ready to paint.</summary>
/// <param name="Strips">The outline's visible pieces, each a strip along the inside of its cells.</param>
/// <param name="Line">The dashes' colour.</param>
/// <param name="Backdrop">The colour between the dashes.</param>
/// <param name="DashOffset">How far the dashes have run round the outline, in device pixels; 0 when still.</param>
internal sealed record SelectionOutlinePass(
    IReadOnlyList<SelectionStrip> Strips,
    SKColor Line,
    SKColor Backdrop,
    int DashOffset);
