// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// Gives a sized tool its current footprint: the tip from the group's option values, in the shape of the document's
/// cells. The footprint is made again only when either changes, and so is a soft tool's falloff.
/// </summary>
public sealed class BrushTipSource
{
    private readonly Func<double> _cellWidthToHeight;
    private BrushFootprint? _footprint;
    private double _footprintCellWidthToHeight;
    private BrushFalloff? _falloff;

    /// <summary>Creates a source for the tip <paramref name="values"/> describe.</summary>
    /// <param name="values">The tool group's option values, made from <see cref="BrushTipOptions"/>.</param>
    /// <param name="cellWidthToHeight">A cell's width divided by its height in the document's font, now.</param>
    public BrushTipSource(ToolOptionValues values, Func<double> cellWidthToHeight)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(cellWidthToHeight);

        Values = values;
        _cellWidthToHeight = cellWidthToHeight;
    }

    /// <summary>The option values the tip comes from.</summary>
    public ToolOptionValues Values { get; }

    /// <summary>The footprint of the current tip, in the current font.</summary>
    public BrushFootprint Footprint
    {
        get
        {
            var tip = BrushTipOptions.ReadTip(Values);
            var cellWidthToHeight = _cellWidthToHeight();
            if (_footprint is null || _footprint.Tip != tip || _footprintCellWidthToHeight != cellWidthToHeight)
            {
                _footprint = BrushFootprint.Create(tip, cellWidthToHeight);
                _footprintCellWidthToHeight = cellWidthToHeight;
            }

            return _footprint;
        }
    }

    /// <summary>
    /// The current footprint weighed with <paramref name="softness"/>. Weighing a big soft tip takes a moment, so it's
    /// weighed again only when the footprint or the softness changes.
    /// </summary>
    /// <param name="softness">How much of the radius fades, from 0 (a hard tip) to 1.</param>
    /// <param name="shapesEdges">False to weigh every cell as a whole, as before step 4.7 (ADR 0074).</param>
    public BrushFalloff FalloffWith(double softness, bool shapesEdges = true)
    {
        var footprint = Footprint;
        if (_falloff is null || _falloff.Footprint != footprint || !_falloff.Softness.Equals(softness) ||
            _falloff.ShapesEdges != shapesEdges)
        {
            _falloff = BrushFalloff.Create(footprint, softness, shapesEdges);
        }

        return _falloff;
    }
}
