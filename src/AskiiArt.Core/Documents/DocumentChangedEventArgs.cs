// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// What changed in a <see cref="Document"/> since the last <see cref="Document.Changed"/> event
/// (ARCHITECTURE §5.9). Views use it to refresh only what's affected.
/// </summary>
public sealed class DocumentChangedEventArgs : EventArgs
{
    internal DocumentChangedEventArgs(
        IReadOnlyList<LayerCellsChange> cellChanges,
        bool layerListChanged,
        IReadOnlyList<Guid> layersWithChangedProperties,
        CanvasResizeChange? canvasResize)
    {
        CellChanges = cellChanges;
        LayerListChanged = layerListChanged;
        LayersWithChangedProperties = layersWithChangedProperties;
        CanvasResize = canvasResize;
    }

    /// <summary>
    /// The cells that changed, per layer, in the order the layers were first changed. Empty when
    /// <see cref="CanvasResize"/> is set, because then the whole canvas changed.
    /// </summary>
    public IReadOnlyList<LayerCellsChange> CellChanges { get; }

    /// <summary>True when layers were added, removed or reordered.</summary>
    public bool LayerListChanged { get; }

    /// <summary>The layers whose <see cref="Layer.Properties"/> changed, such as visibility or name.</summary>
    public IReadOnlyList<Guid> LayersWithChangedProperties { get; }

    /// <summary>Set when the canvas was resized: every cell of every layer may have changed.</summary>
    public CanvasResizeChange? CanvasResize { get; }
}

/// <summary>The cells that changed on one layer.</summary>
/// <param name="LayerId">The layer whose cells changed.</param>
/// <param name="Regions">
/// Rects that together cover every changed cell. They may overlap, and may cover some unchanged
/// cells too.
/// </param>
public sealed record LayerCellsChange(Guid LayerId, IReadOnlyList<CellRect> Regions);

/// <summary>A canvas resize.</summary>
/// <param name="OldSize">The size before the change.</param>
/// <param name="NewSize">The size after the change.</param>
/// <param name="ContentOffset">How far the existing cells moved; see <see cref="CanvasResizePlan.ContentOffset"/>.</param>
public sealed record CanvasResizeChange(CanvasSize OldSize, CanvasSize NewSize, CellPoint ContentOffset);
