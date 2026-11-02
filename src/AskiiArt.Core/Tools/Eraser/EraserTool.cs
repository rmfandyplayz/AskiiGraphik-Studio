// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools.Eraser;

/// <summary>
/// The hard eraser (ARCHITECTURE §8.3): dragging empties the cells under the pointer on the active
/// layer, one cell wide for now. The erasing shows as it happens, so each move is its own command,
/// and the moves of one stroke coalesce into one undo step, as a burst of typing does (ADR 0033).
/// </summary>
public sealed class EraserTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.eraser";

    /// <summary>The eraser's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolEraser", "tool.eraser", "paint", 'E');

    /// <summary>Registers the eraser, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, session => new EraserTool(session));

    private readonly EditingSession _session;
    private CoalescingGroup? _stroke;
    private CellPoint _lastCell;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes
    // back the newest step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    /// <summary>Creates an eraser for <paramref name="session"/>.</summary>
    public EraserTool(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    /// <summary>The eraser never draws anything over the document, so this never happens.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: the eraser takes no text.</summary>
    public bool IsTakingText => false;

    /// <inheritdoc/>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>Starts a stroke, unless the active layer is locked or hidden. It may start outside the canvas.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!_session.CanEditActiveLayer)
            return;

        _stroke = new CoalescingGroup();
        _undoCountAfterLastEdit = null;
        _lastCell = input.Cell;
        Erase(input.Cell, input.Cell);
    }

    /// <summary>Continues the stroke in a straight line, because a fast drag reports cells far apart.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_stroke is null || input.Cell == _lastCell)
            return;

        Erase(_lastCell, input.Cell);
        _lastCell = input.Cell;
    }

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input) => _stroke = null;

    /// <summary>Esc during a stroke takes it back as if it never happened.</summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _stroke is null)
            return false;

        if (_undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();

        _stroke = null;
        return true;
    }

    /// <inheritdoc/>
    public bool TextEntered(string text) => false;

    /// <summary>Keeps what the stroke has erased so far.</summary>
    public void Deactivate() => _stroke = null;

    private void Erase(CellPoint from, CellPoint to)
    {
        // Undoing during a stroke can take away the layer it started on.
        if (!_session.CanEditActiveLayer)
            return;

        var cells = new CellEditBuilder(_session.Document, _session.ActiveLayer.Id);
        foreach (var point in CellLine.Between(from, to))
        {
            if (_session.Document.CanvasSize.Contains(point))
                cells.Set(point, Cell.Empty);
        }

        if (cells.Build(UndoStepNames.Erase) is not { } command)
            return;

        _session.History.Execute(command, _stroke);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }
}
