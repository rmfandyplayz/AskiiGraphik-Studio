// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// Gives a sized tool its current footprint: the tip from the group's option values, in the shape of the document's
/// cells. The footprint is made again only when either changes.
/// </summary>
public sealed class BrushTipSource
{
    private readonly Func<double> _cellWidthToHeight;
    private BrushFootprint? _footprint;
    private double _footprintCellWidthToHeight;

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
}
