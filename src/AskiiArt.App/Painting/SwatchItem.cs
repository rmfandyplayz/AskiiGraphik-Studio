// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using Avalonia.Media;

namespace AskiiArt.App.Painting;

/// <summary>One of the 16 ANSI colours in the Swatches window.</summary>
/// <param name="Brush">How the colour looks, from the palette the canvas draws with.</param>
/// <param name="Name">The colour's localized name, shown as the swatch's tooltip.</param>
/// <param name="Choose">Makes it the colour that the Colors toolbar says the windows change.</param>
internal sealed record SwatchItem(IBrush Brush, string Name, ICommand Choose);
