// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Shapes;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// The Line tool (ADR 0079), the Shapes group's first: a drag draws a straight line from the middle of the cell it
/// starts in to the middle of the cell under the pointer, in the chosen <see cref="LineStyle"/> and the current
/// colours, as typing gives them. The line shows in the document as it's dragged, each new end taking back the last
/// one's undo step, so it's one step when the drag ends; a release in the cell it started in draws nothing. Shift
/// keeps it to an axis, Ctrl (Cmd on macOS) to the diagonals that look like 45° and Ctrl+Shift to one column per row,
/// as the Move tool's drag does (ADR 0069). Esc takes the line back. It clips to the selection, and doesn't draw on a
/// locked or hidden layer, or in Single character while the document can't hold the character. With Include joins on,
/// it joins the line characters already there (ADR 0081).
/// </summary>
public sealed class LineTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.shapes.line";

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolLine", "tool.shapes.line", "shapes");

    /// <summary>Registers the tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new LineTool(
        context.Session, context.Colors, context.Settings.For(LineStyleOptions.Group),
        context.Settings.For(LineStyleOptions.JoinsGroup), context.Settings.For(CharacterBrushOptions.Group),
        context.Settings.RecentGlyphs, context.CellWidthToHeight, context.Densities))
    {
        OptionGroups = [LineStyleOptions.Group, LineStyleOptions.JoinsGroup, CharacterBrushOptions.Group],
        WarningFor = LineStyleOptions.WarningFor,
        CharacterChosen = LineStyleOptions.ChooseSingleCharacter
    };

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly ToolOptionValues _style;
    private readonly ToolOptionValues _joins;
    private readonly ToolOptionValues _character;
    private readonly RecentGlyphs _recent;
    private readonly Func<double> _cellWidthToHeight;
    private readonly Func<IGlyphDensityProvider> _glyphs;
    private Drag? _drag;

    /// <summary>
    /// Creates a Line tool for <paramref name="session"/>, drawing in <paramref name="colors"/> with the style
    /// <paramref name="style"/> holds and, in Single character, the character <paramref name="character"/> holds.
    /// </summary>
    /// <param name="session">The editing session the tool changes.</param>
    /// <param name="colors">The current colours, which drawn cells get.</param>
    /// <param name="style">The values of <see cref="LineStyleOptions.Group"/>.</param>
    /// <param name="joins">The values of <see cref="LineStyleOptions.JoinsGroup"/>.</param>
    /// <param name="character">The values of <see cref="CharacterBrushOptions.Group"/>, shared with the Character Brush.</param>
    /// <param name="recent">The recent characters, which a Single character line's character joins.</param>
    /// <param name="cellWidthToHeight">How wide the document's cells look next to their height.</param>
    /// <param name="glyphs">Where each glyph's ink lies in the document's font, for Precise.</param>
    public LineTool(EditingSession session, PaintColors colors, ToolOptionValues style, ToolOptionValues joins,
        ToolOptionValues character, RecentGlyphs recent, Func<double> cellWidthToHeight,
        Func<IGlyphDensityProvider> glyphs)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(joins);
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(recent);
        ArgumentNullException.ThrowIfNull(cellWidthToHeight);
        ArgumentNullException.ThrowIfNull(glyphs);
        if (style.Group != LineStyleOptions.Group)
            throw new ArgumentException("These aren't the Shapes group's style options.", nameof(style));

        if (joins.Group != LineStyleOptions.JoinsGroup)
            throw new ArgumentException("These aren't the Shapes group's join options.", nameof(joins));

        if (character.Group != CharacterBrushOptions.Group)
            throw new ArgumentException("These aren't the Character Brush's own options.", nameof(character));

        _session = session;
        _colors = colors;
        _style = style;
        _joins = joins;
        _character = character;
        _recent = recent;
        _cellWidthToHeight = cellWidthToHeight;
        _glyphs = glyphs;
    }

    /// <summary>Never raised: the line shows in the document itself.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging draws.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>Nothing: the line shows in the document itself.</summary>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>
    /// Starts a line in the pressed cell, unless the active layer is locked or hidden, or the style is Single character
    /// and the document can't hold the character. The options and colours are kept for the whole drag. A line may
    /// start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        var document = _session.Document;
        var style = LineStyleOptions.ReadStyle(_style);
        var glyph = _character.Get(CharacterBrushOptions.Character);
        if (!_session.CanEditActiveLayer)
            return;

        if (style == LineStyle.SingleCharacter && !document.CharacterSet.Allows(glyph))
            return;

        if (style == LineStyle.SingleCharacter)
            _recent.Use(glyph);

        var picker = new LineGlyphPicker(_cellWidthToHeight(), _glyphs(), document.CharacterSet);
        var joins = LineStyleOptions.ReadJoins(_joins, style);
        var foreground = _colors.ForegroundIn(document.ColorMode);
        var background = _colors.BackgroundIn(document.ColorMode);
        _drag = new Drag(input.Cell, style, joins, glyph, picker, foreground, background,
            new LiveShape(_session, UndoStepNames.Line))
        {
            Pointer = input.Cell,
            Modifiers = input.Modifiers
        };
    }

    /// <summary>Moves the line's end to the pointer's cell, kept to the direction the modifiers choose.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_drag is not { } drag)
            return;

        drag.Pointer = input.Cell;
        drag.Modifiers = input.Modifiers;
        Redraw(drag);
    }

    /// <summary>Ends the line where it is, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        PointerMoved(input);
        _drag = null;
    }

    /// <summary>Follows Shift and Ctrl at once during a drag, though the pointer hasn't moved.</summary>
    public void ModifiersChanged(ToolModifiers modifiers)
    {
        if (_drag is not { } drag || drag.Modifiers == modifiers)
            return;

        drag.Modifiers = modifiers;
        Redraw(drag);
    }

    /// <summary>
    /// Esc during a drag takes the line back as if it never happened. Otherwise Esc isn't the tool's, so the App
    /// switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _drag is not { } drag)
            return false;

        drag.Shape.TakeBack();
        _drag = null;
        return true;
    }

    /// <summary>The tool takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Keeps the line drawn so far.</summary>
    public void Deactivate() => _drag = null;

    // Takes back the last end's step and draws the line to the new end. Anything else that changed the history since,
    // such as an undo during the drag, ends the drag where it is.
    private void Redraw(Drag drag)
    {
        var distance = new CellPoint(drag.Pointer.X - drag.Start.X, drag.Pointer.Y - drag.Start.Y);
        var kept = MoveConstraint.Apply(distance, drag.Modifiers, _cellWidthToHeight());
        var end = new CellPoint(drag.Start.X + kept.X, drag.Start.Y + kept.Y);
        if (end == drag.End)
            return;

        if (drag.Shape.IsInterrupted)
        {
            _drag = null;
            return;
        }

        drag.Shape.TakeBack();
        drag.End = end;

        // Undoing during the drag can take away the layer it started on, or lock it.
        if (end == drag.Start || !_session.CanEditActiveLayer)
            return;

        var path = LinePath.Straight(MiddleOf(drag.Start), MiddleOf(end));
        var cells = drag.Shape.Collect(drag.Foreground, drag.Background);
        cells.PutLine(drag.Picker.Pick(path, drag.Style, drag.Character), path.IsClosed, drag.Joins);
        drag.Shape.Show(cells);
    }

    private static CanvasPoint MiddleOf(CellPoint cell) => new(cell.X + 0.5, cell.Y + 0.5);

    // One drag's line: what it keeps from its start, and where it has got to.
    private sealed class Drag(CellPoint start, LineStyle style, bool joins, Rune character, LineGlyphPicker picker,
        CellColor foreground, CellColor background, LiveShape shape)
    {
        public CellPoint Start { get; } = start;

        public LineStyle Style { get; } = style;

        public bool Joins { get; } = joins;

        public Rune Character { get; } = character;

        public LineGlyphPicker Picker { get; } = picker;

        public CellColor Foreground { get; } = foreground;

        public CellColor Background { get; } = background;

        public LiveShape Shape { get; } = shape;

        public CellPoint Pointer { get; set; }

        public ToolModifiers Modifiers { get; set; }

        // Where the drawn line ends; at the start, nothing is drawn.
        public CellPoint End { get; set; } = start;
    }
}
