// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>Which kind of value a <see cref="CellColor"/> holds.</summary>
public enum CellColorKind : byte
{
    /// <summary>No colour (see <see cref="CellColor.Unset"/>).</summary>
    Unset = 0,

    /// <summary>One of the 16 standard ANSI colours.</summary>
    Ansi = 1,

    /// <summary>A 24-bit RGB colour.</summary>
    Rgb = 2
}

/// <summary>
/// A cell's text colour or background (ARCHITECTURE §5.11, ADR 0036): unset, one of the 16 ANSI
/// colours, or an RGB colour. Which kinds a document may hold depends on its <see cref="ColorMode"/>.
/// </summary>
/// <remarks>
/// Packed into four bytes, so <see cref="Cell"/> stays within its 16-byte target: the kind in the
/// top byte, and the ANSI number or the red, green and blue bytes below it. <c>default</c> is
/// <see cref="Unset"/>.
/// </remarks>
public readonly record struct CellColor
{
    private const int KindShift = 24;
    private const int RedShift = 16;
    private const int GreenShift = 8;
    private const uint ValueMask = 0x00FF_FFFF;
    private const uint ByteMask = 0xFF;

    private readonly uint _packed;

    private CellColor(CellColorKind kind, uint value)
    {
        _packed = ((uint)kind << KindShift) | value;
    }

    /// <summary>
    /// No colour. As a text colour it means the default one; as a background it means none, so
    /// the backgrounds of the layers below show through (§5.6). The defaults are drawn in the
    /// canvas's own ink and paper, and exported with no colour code.
    /// </summary>
    public static CellColor Unset => default;

    /// <summary>Which kind of value this is.</summary>
    public CellColorKind Kind => (CellColorKind)(_packed >> KindShift);

    /// <summary>True when no colour is set.</summary>
    public bool IsUnset => _packed == 0;

    /// <summary>One of the 16 ANSI colours.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There's no such ANSI colour.</exception>
    public static CellColor FromAnsi(AnsiColor color)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color), color, null);

        return new CellColor(CellColorKind.Ansi, (uint)color);
    }

    /// <summary>An RGB colour.</summary>
    public static CellColor FromRgb(RgbColor color) =>
        new(CellColorKind.Rgb, ((uint)color.Red << RedShift) | ((uint)color.Green << GreenShift) | color.Blue);

    /// <summary>Gets the ANSI colour, when this is one.</summary>
    public bool TryGetAnsi(out AnsiColor color)
    {
        color = Kind == CellColorKind.Ansi ? (AnsiColor)(_packed & ValueMask) : default;
        return Kind == CellColorKind.Ansi;
    }

    /// <summary>Gets the RGB colour, when this is one.</summary>
    public bool TryGetRgb(out RgbColor color)
    {
        color = Kind == CellColorKind.Rgb ? UnpackRgb() : default;
        return Kind == CellColorKind.Rgb;
    }

    /// <inheritdoc/>
    public override string ToString() => Kind switch
    {
        CellColorKind.Ansi => ((AnsiColor)(_packed & ValueMask)).ToString(),
        CellColorKind.Rgb => UnpackRgb().ToString(),
        _ => "unset"
    };

    private RgbColor UnpackRgb() =>
        new((byte)((_packed >> RedShift) & ByteMask), (byte)((_packed >> GreenShift) & ByteMask),
            (byte)(_packed & ByteMask));
}
