// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools;
using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>A tool's marker, ready to paint: its middle in device pixels, and what it is.</summary>
internal readonly record struct SceneMarker(Point Centre, ToolMarkerKind Kind);

/// <summary>A curve's handle, ready to paint: its anchor and its end in device pixels, and how strongly it shows.</summary>
internal readonly record struct SceneHandle(Point Anchor, Point End, bool IsStrong);
