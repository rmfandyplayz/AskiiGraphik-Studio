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

    /// <summary>How many cells a jump moves (<see cref="ToolKey.JumpLeft"/> and friends). A setting later (M8).</summary>
    public const int ShortJump = 3;

    /// <summary>How many cells a long jump moves (<see cref="ToolKey.LongJumpLeft"/> and friends). A setting later (M8).</summary>
    public const int LongJump = 10;

    /// <summary>The text tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolText", "tool.text", "text");

    /// <summary>Registers the text tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, session => new TextTool(session));

    private readonly EditingSession _session;
    private TextBlock? _block;
    private bool _backspaceErasesCaretCell;
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

    /// <summary>
    /// Places the caret in the clicked cell and starts a new block of text there, so Enter
    /// returns to this column.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!Document.CanvasSize.Contains(input.Cell))
            return;

        _block = TextBlock.StartingAt(input.Cell);
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

        if (MoveDistance(key) is { } distance)
        {
            MoveCaret(new CellPoint(caret.X + distance.X, caret.Y + distance.Y));
            return true;
        }

        switch (key)
        {
            case ToolKey.Home:
                MoveCaret(caret with { X = 0 });
                break;
            case ToolKey.End:
                MoveToRowEnd(caret);
                break;
            case ToolKey.Top:
                MoveCaret(caret with { Y = 0 });
                break;
            case ToolKey.Bottom:
                MoveCaret(caret with { Y = Document.CanvasSize.Height - 1 });
                break;
            case ToolKey.Enter:
                StartNextLine(caret);
                break;
            case ToolKey.Backspace:
                EraseBackwards(caret);
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

        var cells = new CellEditBuilder(Document, _session.ActiveLayer.Id);
        var block = BlockAround(caret);
        var typedAnything = false;
        var isStuckAtRightEdge = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (!Document.CharacterSet.Allows(rune))
                continue;

            cells.Set(caret, Cell.FromGlyph(rune));
            block = block.WithTyped(caret);
            var next = ClampToCanvas(caret with { X = caret.X + 1 });
            isStuckAtRightEdge = next == caret;
            caret = next;
            typedAnything = true;
        }

        if (!typedAnything)
            return true;

        _block = block;

        // Stuck at the right edge, the caret is on what was just typed, so Backspace erases that.
        _backspaceErasesCaretCell = isStuckAtRightEdge;
        Execute(cells.Build());
        SetCaret(caret);
        return true;
    }

    /// <summary>Removes the caret and ends the typing burst.</summary>
    public void Deactivate()
    {
        _typingBurst = null;
        _block = null;
        SetCaret(null);
    }

    // Backspace normally erases the cell before the caret and moves back into it, taking back what
    // was just typed. But when the caret was put on its cell (by a click or a move key), or it's at
    // the left edge, Backspace erases that cell instead. The cells to the right never shift: that
    // would wreck the art beside the text.
    private void EraseBackwards(CellPoint caret)
    {
        if (_backspaceErasesCaretCell || caret.X == 0)
        {
            Write(caret, Cell.Empty, nextCaret: caret);
            return;
        }

        var behind = caret with { X = caret.X - 1 };
        Write(behind, Cell.Empty, nextCaret: behind);
    }

    // After an erase, the caret's cell is empty, so the next Backspace erases the cell before it.
    private void Write(CellPoint point, Cell cell, CellPoint nextCaret)
    {
        if (!CanEdit())
            return;

        var cells = new CellEditBuilder(Document, _session.ActiveLayer.Id);
        cells.Set(point, cell);
        Execute(cells.Build());
        _backspaceErasesCaretCell = false;
        SetCaret(nextCaret);
    }

    // Enter goes to the next row, under the start of the text being typed. Pressed away from that
    // text, it starts a new block where the caret is.
    private void StartNextLine(CellPoint caret)
    {
        var block = BlockAround(caret);
        var next = ClampToCanvas(new CellPoint(block.LineStart, caret.Y + 1));
        _block = block.WithRow(next.Y);
        MoveCaret(next);
    }

    // End goes just past the row's last glyph, as if the row had just been typed, so Backspace
    // erases that glyph. In a full row the caret stops on the last glyph instead.
    private void MoveToRowEnd(CellPoint caret)
    {
        var column = ColumnAfterLastGlyph(caret.Y);
        MoveCaret(caret with { X = column }, backspaceErasesCaretCell: column >= Document.CanvasSize.Width);
    }

    // How far each arrow-style key moves the caret. Moves stop at the canvas edges.
    private static (int X, int Y)? MoveDistance(ToolKey key) => key switch
    {
        ToolKey.Left => (-1, 0),
        ToolKey.Right => (1, 0),
        ToolKey.Up => (0, -1),
        ToolKey.Down => (0, 1),
        ToolKey.JumpLeft => (-ShortJump, 0),
        ToolKey.JumpRight => (ShortJump, 0),
        ToolKey.JumpUp => (0, -ShortJump),
        ToolKey.JumpDown => (0, ShortJump),
        ToolKey.LongJumpLeft => (-LongJump, 0),
        ToolKey.LongJumpRight => (LongJump, 0),
        ToolKey.LongJumpUp => (0, -LongJump),
        ToolKey.LongJumpDown => (0, LongJump),
        _ => null
    };

    private TextBlock BlockAround(CellPoint caret) =>
        _block is { } block && block.Continues(caret) ? block : TextBlock.StartingAt(caret);

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

    // Any move but typing ends the typing burst. The caret usually lands on a cell the user chose,
    // so Backspace erases that cell rather than the one before it.
    private void MoveCaret(CellPoint point, bool backspaceErasesCaretCell = true)
    {
        _typingBurst = null;
        _backspaceErasesCaretCell = backspaceErasesCaretCell;
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

    // The text typed since the last click: the column Enter returns to, the rows it covers, and its
    // rightmost typed column. Moving the caret with the keys and typing elsewhere starts a new one,
    // so Enter doesn't jump back to text typed somewhere else (from 1.5 testing).
    private readonly record struct TextBlock(int LineStart, int Top, int Bottom, int Right)
    {
        // Nothing typed yet, so the block is just this cell.
        public static TextBlock StartingAt(CellPoint cell) => new(cell.X, cell.Y, cell.Y, cell.X - 1);

        // A caret on one of the block's rows, between its line start and just past its rightmost
        // glyph, is still in this text: fixing a typo, say.
        public bool Continues(CellPoint caret) =>
            caret.Y >= Top && caret.Y <= Bottom && caret.X >= LineStart && caret.X <= Right + 1;

        public TextBlock WithTyped(CellPoint cell) => WithRow(cell.Y) with { Right = Math.Max(Right, cell.X) };

        public TextBlock WithRow(int row) => this with { Top = Math.Min(Top, row), Bottom = Math.Max(Bottom, row) };
    }
}
