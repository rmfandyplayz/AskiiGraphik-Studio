// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The composited cells of a document's active frame (ARCHITECTURE §5.6), kept up to date from the
/// document's change events. Only the regions that changed are composited again (§12), so drawing
/// a frame just reads cells from here. Belongs to the UI thread, like the document.
/// </summary>
internal sealed class CompositeCache : IDisposable
{
    private readonly CompositingPolicy _policy;
    private Cell[] _cells = [];
    private bool _isDisposed;

    public CompositeCache(Document document, CompositingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(policy);

        Document = document;
        _policy = policy;
        CompositeEverything();
        document.Changed += OnDocumentChanged;
    }

    /// <summary>Raised after the cached cells have been brought up to date with a document change.</summary>
    public event EventHandler? Changed;

    public Document Document { get; }

    public CanvasSize Size { get; private set; }

    public Cell GetCell(CellPoint point)
    {
        if (Size.Contains(point))
            return _cells[point.Y * Size.Width + point.X];

        throw new ArgumentOutOfRangeException(nameof(point), point, "The point is outside the canvas.");
    }

    /// <summary>One row of composited cells.</summary>
    public ReadOnlySpan<Cell> GetRow(int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Size.Height);

        return _cells.AsSpan(y * Size.Width, Size.Width);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Document.Changed -= OnDocumentChanged;
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        UpdateFrom(e);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateFrom(DocumentChangedEventArgs e)
    {
        // Visibility, order and layer count affect every cell; so does a resize.
        if (e.CanvasResize is not null || e.LayerListChanged || e.LayersWithChangedProperties.Count > 0)
        {
            CompositeEverything();
            return;
        }

        foreach (var change in e.CellChanges)
        {
            foreach (var region in change.Regions)
                Composite(region);
        }
    }

    private void CompositeEverything()
    {
        Size = Document.CanvasSize;
        if (_cells.Length != Size.CellCount)
            _cells = new Cell[Size.CellCount];

        Composite(Size.Bounds);
    }

    private void Composite(CellRect region)
    {
        var frame = Document.ActiveFrame;

        // Row by row, because the compositor writes a region's cells contiguously and each row is
        // contiguous here.
        for (var y = region.Top; y < region.Bottom; y++)
        {
            var row = new CellRect(region.X, y, region.Width, 1);
            Compositor.CompositeRegion(frame, row, _policy, _cells.AsSpan(y * Size.Width + region.X, region.Width));
        }
    }
}
