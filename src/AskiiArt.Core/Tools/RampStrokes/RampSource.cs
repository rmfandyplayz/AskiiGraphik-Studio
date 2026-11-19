// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>
/// The ramp the Ramp option chooses, read when it's needed, so a stroke uses the ramp as it is now, even one the user
/// is editing (ADR 0052).
/// </summary>
public sealed class RampSource
{
    private readonly RampLibrary _ramps;

    /// <summary>Creates a source of the ramp that <paramref name="values"/> choose among <paramref name="ramps"/>.
    /// </summary>
    /// <param name="values">The values of <see cref="RampOptions.Group"/>.</param>
    /// <param name="ramps">The ramps the option chooses among.</param>
    /// <exception cref="ArgumentException"><paramref name="values"/> aren't the ramp's.</exception>
    public RampSource(ToolOptionValues values, RampLibrary ramps)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(ramps);
        if (values.Group != RampOptions.Group)
            throw new ArgumentException("These aren't the ramp's options.", nameof(values));

        Values = values;
        _ramps = ramps;
    }

    /// <summary>The values of <see cref="RampOptions.Group"/>.</summary>
    public ToolOptionValues Values { get; }

    /// <summary>The chosen ramp, or the default one when the chosen one is gone.</summary>
    public Ramp Ramp => _ramps.Find(Values.Get(RampOptions.Ramp)) ?? BuiltInRamps.Default.Ramp;

    /// <summary>Whether the chosen ramp can be used in <paramref name="document"/>: whether its character set holds
    /// every character of the ramp.</summary>
    public bool FitsIn(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Ramp.FitsIn(document.CharacterSet);
    }
}
