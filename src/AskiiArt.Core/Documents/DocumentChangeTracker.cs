// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Collects a document's changes and raises <see cref="Document.Changed"/>: at once for a single
/// change, or once at the end of a group started by <see cref="Document.BeginChanges"/> (ADR 0010).
/// </summary>
internal sealed class DocumentChangeTracker
{
    private readonly Document _document;
    private readonly List<Guid> _layersWithChangedCells = [];
    private readonly Dictionary<Guid, DirtyRegion> _dirtyRegionsByLayer = [];
    private readonly List<Guid> _layersWithChangedProperties = [];
    private int _groupDepth;
    private bool _layerListChanged;
    private bool _settingsChanged;
    private CanvasResizeChange? _canvasResize;

    public DocumentChangeTracker(Document document)
    {
        _document = document;
    }

    private bool HasPendingChanges =>
        _layersWithChangedCells.Count > 0 || _layerListChanged || _layersWithChangedProperties.Count > 0
        || _settingsChanged || _canvasResize is not null;

    public void Begin() => _groupDepth++;

    public void End()
    {
        if (_groupDepth == 0)
            throw new InvalidOperationException("No group of changes is in progress.");

        _groupDepth--;
        RaiseIfComplete();
    }

    public void RecordCells(Guid layerId, CellRect region)
    {
        // After a resize the whole canvas counts as changed, so single cells add nothing.
        if (_canvasResize is null)
        {
            if (!_dirtyRegionsByLayer.TryGetValue(layerId, out var dirtyRegion))
            {
                dirtyRegion = new DirtyRegion();
                _dirtyRegionsByLayer.Add(layerId, dirtyRegion);
                _layersWithChangedCells.Add(layerId);
            }

            dirtyRegion.Add(region);
        }

        RaiseIfComplete();
    }

    public void RecordLayerListChanged()
    {
        _layerListChanged = true;
        RaiseIfComplete();
    }

    public void RecordPropertiesChanged(Guid layerId)
    {
        if (!_layersWithChangedProperties.Contains(layerId))
            _layersWithChangedProperties.Add(layerId);

        RaiseIfComplete();
    }

    public void RecordSettingsChanged()
    {
        _settingsChanged = true;
        RaiseIfComplete();
    }

    public void RecordCanvasResized(CanvasSize oldSize, CanvasSize newSize, CellPoint contentOffset)
    {
        // Several resizes in one group report as one: from the first old size to the last new size,
        // with the offsets added up.
        var firstOldSize = _canvasResize?.OldSize ?? oldSize;
        var totalOffset = _canvasResize?.ContentOffset.Offset(contentOffset) ?? contentOffset;
        _canvasResize = new CanvasResizeChange(firstOldSize, newSize, totalOffset);

        ClearCellChanges();
        RaiseIfComplete();
    }

    public void DiscardPendingChanges()
    {
        ClearCellChanges();
        _layerListChanged = false;
        _layersWithChangedProperties.Clear();
        _settingsChanged = false;
        _canvasResize = null;
    }

    private void RaiseIfComplete()
    {
        if (_groupDepth > 0 || !HasPendingChanges)
            return;

        var cellChanges = _layersWithChangedCells
            .Select(layerId => new LayerCellsChange(layerId, _dirtyRegionsByLayer[layerId].ToList()))
            .ToList();
        var change = new DocumentChangedEventArgs(cellChanges, _layerListChanged, _layersWithChangedProperties.ToList(),
            _settingsChanged, _canvasResize);

        // Reset before raising, so a handler that reads or changes the document starts clean.
        DiscardPendingChanges();
        _document.RaiseChanged(change);
    }

    private void ClearCellChanges()
    {
        _layersWithChangedCells.Clear();
        _dirtyRegionsByLayer.Clear();
    }
}
