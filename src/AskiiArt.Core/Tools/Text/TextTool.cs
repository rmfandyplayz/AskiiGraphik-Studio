// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools.Text;

/// <summary>
/// Types text into the active layer, one glyph per cell, overwriting what's there (ADR 0017).
/// Click a cell to place the caret; typed characters land at the caret and move it right.
/// </summary>
/// <remarks>
/// Each keystroke is its own command, but the keystrokes of one typing burst coalesce into one
/// undo step (ARCHITECTURE §9). A burst ends when the caret is moved by anything but typing, or
/// after a pause of <see cref="TypingPause"/>.
/// </remarks>
public sealed class TextTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.text";

    /// <summary>How long a pause in typing ends the burst, so the next keystroke starts a new undo step.</summary>
    public static readonly TimeSpan TypingPause = TimeSpan.FromSeconds(2);

    /// <summary>The text tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolText", "tool.text", "text");

    /// <summary>Registers the text tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, session => new TextTool(session));

    private readonly EditingSession _session;
    private int _lineStartColumn;
    private CoalescingGroup? _typingBurst;
    private DateTimeOffset _lastKeystrokeTime;

    /// <summary>Creates a text tool for <paramref name="session"/>, with no caret yet.</summary>
    public TextTool(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    /// <inheritdoc/>
    public event EventHandler? OverlayChanged;

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>False: Space types a space.</summary>
    public bool AllowsSpacePanning => false;

    /// <summary>The cell the next typed character goes into, or null before the first click and after Esc.</summary>
    public CellPoint? Caret { get; private set; }

    /// <inheritdoc/>
    public ToolOverlay Overlay => Caret is null ? ToolOverlay.None : new ToolOverlay(Caret);

    private Document Document => _session.Document;

    /// <summary>Places the caret in the clicked cell. Enter returns to this column.</summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!Document.CanvasSize.Contains(input.Cell))
            return;

        _lineStartColumn = input.Cell.X;
        MoveCaret(input.Cell);
    }

    /// <inheritdoc/>
    public void PointerMoved(ToolPointerInput input)
    {
    }

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input)
    {
    }

    /// <inheritdoc/>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (CurrentCaret() is not { } caret)
            return false;

        switch (key)
        {
            case ToolKey.Left:
                MoveCaret(caret with { X = caret.X - 1 });
                break;
            case ToolKey.Right:
                MoveCaret(caret with { X = caret.X + 1 });
                break;
            case ToolKey.Up:
                MoveCaret(caret with { Y = caret.Y - 1 });
                break;
            case ToolKey.Down:
                MoveCaret(caret with { Y = caret.Y + 1 });
                break;
            case ToolKey.Home:
                MoveCaret(caret with { X = 0 });
                break;
            case ToolKey.End:
                MoveCaret(caret with { X = ColumnAfterLastGlyph(caret.Y) });
                break;
            case ToolKey.Enter:
                MoveCaret(new CellPoint(_lineStartColumn, caret.Y + 1));
                break;
            case ToolKey.Backspace:
                EraseBehind(caret);
                break;
            case ToolKey.Delete:
                Write(caret, Cell.Empty, nextCaret: caret);
                break;
            case ToolKey.Escape:
                Deactivate();
                break;
            default:
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes each character of <paramref name="text"/> at the caret, moving it right. Characters
    /// the document's character set doesn't allow are skipped. At the right edge the caret stays
    /// in the last column, so further typing overwrites that cell.
    /// </summary>
    public bool TextEntered(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (CurrentCaret() is not { } caret)
            return false;

        if (!CanEdit())
            return true;

        var layer = _session.ActiveLayer;
        var cells = new CellEditBuilder(Document, layer.Id);
        foreach (var rune in text.EnumerateRunes())
        {
            if (!Document.CharacterSet.Allows(rune))
                continue;

            cells.Set(caret, Cell.FromGlyph(rune));
            caret = ClampToCanvas(caret with { X = caret.X + 1 });
        }

        Execute(cells.Build());
        SetCaret(caret);
        return true;
    }

    /// <summary>Removes the caret and ends the typing burst.</summary>
    public void Deactivate()
    {
        _typingBurst = null;
        SetCaret(null);
    }

    // Backspace erases the cell before the caret and moves back into it. The cells to the right
    // stay where they are: shifting them would wreck the art beside the text.
    private void EraseBehind(CellPoint caret)
    {
        if (caret.X == 0)
            return;

        var behind = caret with { X = caret.X - 1 };
        Write(behind, Cell.Empty, nextCaret: behind);
    }

    private void Write(CellPoint point, Cell cell, CellPoint nextCaret)
    {
        if (!CanEdit())
            return;

        var cells = new CellEditBuilder(Document, _session.ActiveLayer.Id);
        cells.Set(point, cell);
        Execute(cells.Build());
        SetCaret(nextCaret);
    }

    // A locked layer can't be typed into (§8.1). Nothing happens, and the caret stays put.
    private bool CanEdit() => !_session.ActiveLayer.Properties.IsLocked;

    private void Execute(CellEditCommand? command)
    {
        var now = _session.Clock.GetUtcNow();
        if (_typingBurst is null || now - _lastKeystrokeTime > TypingPause)
            _typingBurst = new CoalescingGroup();

        _lastKeystrokeTime = now;

        // Typing a glyph over the same glyph changes nothing, but still counts as typing.
        if (command is not null)
            _session.History.Execute(command, _typingBurst);
    }

    private void MoveCaret(CellPoint point)
    {
        _typingBurst = null;
        SetCaret(ClampToCanvas(point));
    }

    private void SetCaret(CellPoint? caret)
    {
        if (caret == Caret)
            return;

        Caret = caret;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    // The canvas can shrink under the caret (a resize, or undoing one).
    private CellPoint? CurrentCaret()
    {
        if (Caret is { } caret)
            Caret = ClampToCanvas(caret);

        return Caret;
    }

    private int ColumnAfterLastGlyph(int row)
    {
        var cells = _session.ActiveLayer.GetRow(row);
        var lastOccupied = cells.Length - 1;
        while (lastOccupied >= 0 && cells[lastOccupied].IsEmpty)
            lastOccupied--;

        return lastOccupied + 1;
    }

    private CellPoint ClampToCanvas(CellPoint point)
    {
        var size = Document.CanvasSize;
        return new CellPoint(Math.Clamp(point.X, 0, size.Width - 1), Math.Clamp(point.Y, 0, size.Height - 1));
    }
}
