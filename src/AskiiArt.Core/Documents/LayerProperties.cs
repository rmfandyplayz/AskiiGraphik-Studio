// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// A layer's settings other than its cells (ARCHITECTURE §5.5). Immutable: to change a setting,
/// make a copy with <c>with</c> and apply it through a command.
/// </summary>
public sealed record LayerProperties
{
    /// <summary>Creates the properties of a visible, unlocked layer.</summary>
    public LayerProperties(string name)
    {
        Name = name;
    }

    /// <summary>The name shown in the layers panel. Any text, including an empty string.</summary>
    public string Name
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    }

    /// <summary>False hides the layer: compositing skips it.</summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>True protects the layer's cells from tools (ARCHITECTURE §8.1).</summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// Reserved (ARCHITECTURE §18): stored, but compositing ignores it, so every layer is fully
    /// opaque in v1. A value from 0 to 1.
    /// </summary>
    public double Opacity
    {
        get;
        init
        {
            if (value is not (>= 0.0 and <= 1.0))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Opacity must be from 0 to 1.");

            field = value;
        }
    } = 1.0;

    /// <summary>Reserved (ARCHITECTURE §18): only <see cref="LayerBlendMode.Normal"/> exists in v1.</summary>
    public LayerBlendMode BlendMode
    {
        get;
        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown blend mode.");

            field = value;
        }
    }
}
