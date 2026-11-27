// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Shapes;

/// <summary>Which shape a <see cref="BoxShapeTool"/> draws in the box a drag makes.</summary>
public enum BoxShape
{
    /// <summary>A rectangle filling the box.</summary>
    Rectangle,

    /// <summary>The ellipse that fits the box.</summary>
    Ellipse
}

/// <summary>
/// The Rectangle and Ellipse tools (ADR 0080), the Shapes group's: a drag draws the shape in the box from the cell it
/// starts in to the cell under the pointer, both included, its outline in the chosen <see cref="LineStyle"/> and
/// inside it the chosen <see cref="ShapeFill"/>, in the current colours. Shift makes the box look square on screen and
/// Alt grows it from the first cell outwards, as in Rectangle and Ellipse Select (ADR 0060). As with the Line tool
/// (ADR 0079), the shape shows in the document as it's dragged and ends as one undo step, a release in the first cell
/// draws nothing, Esc takes it back, it clips to the selection, and it isn't drawn on a locked or hidden layer, or
/// while it would draw a character the document can't hold. A box one cell thin draws as a line. The rectangle has
/// the box-drawing styles too, and with Include joins on, its outline joins the line characters already there, a
/// character fill covering the parts of them inside it (ADR 0081, ADR 0082).
/// </summary>
public sealed class BoxShapeTool : ITool
{
    /// <summary>The Rectangle tool's id.</summary>
    public const string RectangleId = "askiiart.shapes.rectangle";

    /// <summary>The Ellipse tool's id.</summary>
    public const string EllipseId = "askiiart.shapes.ellipse";

    private static readonly Rune _roundedTopCorner = new('.');
    private static readonly Rune _roundedBottomCorner = new('\'');
    private static readonly Rune _roundedBoxTopLeft = new('╭');
    private static readonly Rune _roundedBoxTopRight = new('╮');
    private static readonly Rune _roundedBoxBottomRight = new('╯');
    private static readonly Rune _roundedBoxBottomLeft = new('╰');

    /// <summary>The Rectangle tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor RectangleDescriptor { get; } =
        new(RectangleId, "ToolRectangle", "tool.shapes.rectangle", "shapes");

    /// <summary>The Ellipse tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor EllipseDescriptor { get; } =
        new(EllipseId, "ToolEllipse", "tool.shapes.ellipse", "shapes");

    /// <summary>Registers the Rectangle tool, as DI does.</summary>
    public static ToolDefinition RectangleDefinition { get; } = CreateDefinition(BoxShape.Rectangle);

    /// <summary>Registers the Ellipse tool, as DI does.</summary>
    public static ToolDefinition EllipseDefinition { get; } = CreateDefinition(BoxShape.Ellipse);

    private readonly BoxShape _shape;
    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly ToolSettings _settings;
    private readonly Func<double> _cellWidthToHeight;
    private readonly Func<IGlyphDensityProvider> _glyphs;
    private Drag? _drag;

    /// <summary>
    /// Creates a tool that draws <paramref name="shape"/> for <paramref name="session"/>, in <paramref name="colors"/>,
    /// with the options <paramref name="settings"/> hold.
    /// </summary>
    /// <param name="shape">The shape it draws.</param>
    /// <param name="session">The editing session the tool changes.</param>
    /// <param name="colors">The current colours, which drawn cells get.</param>
    /// <param name="settings">The tools' option values: the style, the fill, rounded corners and the character.</param>
    /// <param name="cellWidthToHeight">How wide the document's cells look next to their height.</param>
    /// <param name="glyphs">Where each glyph's ink lies in the document's font, for Precise.</param>
    public BoxShapeTool(BoxShape shape, EditingSession session, PaintColors colors, ToolSettings settings,
        Func<double> cellWidthToHeight, Func<IGlyphDensityProvider> glyphs)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(cellWidthToHeight);
        ArgumentNullException.ThrowIfNull(glyphs);

        _shape = shape;
        _session = session;
        _colors = colors;
        _settings = settings;
        _cellWidthToHeight = cellWidthToHeight;
        _glyphs = glyphs;
    }

    /// <summary>Never raised: the shape shows in the document itself.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => _shape == BoxShape.Rectangle ? RectangleDescriptor : EllipseDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging draws.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>Nothing: the shape shows in the document itself.</summary>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>
    /// Starts a shape in the pressed cell, unless the active layer is locked or hidden, or the shape would draw the
    /// Character Brush's character and the document can't hold it. The options and colours are kept for the whole
    /// drag. A shape may start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!_session.CanEditActiveLayer)
            return;

        var document = _session.Document;
        var style = LineStyleOptions.ReadStyle(_settings.For(ShapeOptions.StyleGroupOf(_shape)));
        var fill = ShapeOptions.ReadFill(_settings.For(ShapeOptions.FillGroup));
        var character = _settings.For(CharacterBrushOptions.Group).Get(CharacterBrushOptions.Character);
        var usesCharacter = style == LineStyle.SingleCharacter || fill == ShapeFill.Character;
        if (usesCharacter && !document.CharacterSet.Allows(character))
            return;

        if (usesCharacter)
            _settings.RecentGlyphs.Use(character);

        // A monochrome document's cells have no backgrounds to fill.
        if (fill == ShapeFill.Background && document.ColorMode == ColorMode.Monochrome)
            fill = ShapeFill.None;

        var isRounded = _shape == BoxShape.Rectangle
            && _settings.For(ShapeOptions.RectangleGroup).Get(ShapeOptions.RoundedCorners);
        var picker = new LineGlyphPicker(_cellWidthToHeight(), _glyphs(), document.CharacterSet);
        var joins = _shape == BoxShape.Rectangle && LineStyleOptions.ReadJoins(_settings.For(LineStyleOptions.JoinsGroup), style);
        var look = new Look(picker.Drawn(style), joins, fill, isRounded, character, picker,
            _colors.ForegroundIn(document.ColorMode), _colors.BackgroundIn(document.ColorMode));
        var undoStepName = _shape == BoxShape.Rectangle ? UndoStepNames.Rectangle : UndoStepNames.Ellipse;
        _drag = new Drag(input.Cell, look, new LiveShape(_session, undoStepName))
        {
            Pointer = input.Cell,
            Modifiers = input.Modifiers
        };
    }

    /// <summary>Fits the shape to the box from the first cell to the pointer's, shaped by Shift and Alt.</summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_drag is not { } drag)
            return;

        drag.Pointer = input.Cell;
        drag.Modifiers = input.Modifiers;
        Redraw(drag);
    }

    /// <summary>Ends the shape where it is, as one undo step.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        PointerMoved(input);
        _drag = null;
    }

    /// <summary>Follows Shift and Alt at once during a drag, though the pointer hasn't moved.</summary>
    public void ModifiersChanged(ToolModifiers modifiers)
    {
        if (_drag is not { } drag || drag.Modifiers == modifiers)
            return;

        drag.Modifiers = modifiers;
        Redraw(drag);
    }

    /// <summary>
    /// Esc during a drag takes the shape back as if it never happened. Otherwise Esc isn't the tool's, so the App
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

    /// <summary>Keeps the shape drawn so far.</summary>
    public void Deactivate() => _drag = null;

    private static ToolDefinition CreateDefinition(BoxShape shape) => new(
        shape == BoxShape.Rectangle ? RectangleDescriptor : EllipseDescriptor,
        context => new BoxShapeTool(shape, context.Session, context.Colors, context.Settings,
            context.CellWidthToHeight, context.Densities))
    {
        OptionGroups = OptionGroupsOf(shape),
        WarningFor = (settings, document) => ShapeOptions.WarningFor(shape, settings, document),
        CharacterChosen = settings => ShapeOptions.ChooseCharacter(shape, settings)
    };

    private static ToolOptionGroup[] OptionGroupsOf(BoxShape shape)
    {
        if (shape == BoxShape.Rectangle)
        {
            return [ShapeOptions.RectangleStyleGroup, LineStyleOptions.JoinsGroup, ShapeOptions.FillGroup,
                ShapeOptions.RectangleGroup, CharacterBrushOptions.Group];
        }

        return [LineStyleOptions.Group, ShapeOptions.FillGroup, CharacterBrushOptions.Group];
    }

    // Takes back the last box's step and draws the shape in the new box. Anything else that changed the history since,
    // such as an undo during the drag, ends the drag where it is.
    private void Redraw(Drag drag)
    {
        CellRect? box = null;
        if (drag.Pointer != drag.Start)
        {
            box = DragBox.Between(drag.Start, drag.Pointer, drag.Modifiers.HasFlag(ToolModifiers.Shift),
                drag.Modifiers.HasFlag(ToolModifiers.Alt), _cellWidthToHeight());
        }

        if (box == drag.Box)
            return;

        if (drag.Shape.IsInterrupted)
        {
            _drag = null;
            return;
        }

        drag.Shape.TakeBack();
        drag.Box = box;

        // Undoing during the drag can take away the layer it started on, or lock it.
        if (box is not { } drawn || !_session.CanEditActiveLayer)
            return;

        var cells = drag.Shape.Collect(drag.Look.Foreground, drag.Look.Background);
        Draw(cells, drawn, drag.Look);
        drag.Shape.Show(cells);
    }

    private void Draw(ShapeCells cells, CellRect box, Look look)
    {
        if (box.Width == 1 || box.Height == 1)
        {
            var line = LinePath.Straight(MiddleOf(box.Left, box.Top), MiddleOf(box.Right - 1, box.Bottom - 1));
            cells.PutLine(look.Picker.Pick(line, look.Style, look.Character), line.IsClosed, look.Joins);
            return;
        }

        var path = _shape == BoxShape.Rectangle ? LinePath.Rectangle(box) : LinePath.Ellipse(box);
        var outline = look.Picker.Pick(path, look.Style, look.Character);
        HashSet<CellPoint>? covered = null;
        if (look.Fill != ShapeFill.None)
        {
            var inside = ShapeInterior.Of(box, outline.Select(glyph => glyph.Cell));
            foreach (var point in inside)
            {
                if (look.Fill == ShapeFill.Character)
                    cells.PutGlyph(point, look.Character);
                else
                    cells.PutBackground(point);
            }

            // A character fill covers what was inside, the lines the outline joins included.
            if (look.Fill == ShapeFill.Character)
                covered = [.. inside];
        }

        if (!look.IsRounded)
        {
            cells.PutLine(outline, path.IsClosed, look.Joins, covered);
            return;
        }

        var rounded = new List<LineGlyph>(outline.Count);
        foreach (var glyph in outline)
        {
            if (!IsCornerOf(box, glyph.Cell))
                rounded.Add(glyph);
            else if (look.Style != LineStyle.SingleCharacter)
                rounded.Add(glyph with { Glyph = RoundedCorner(look.Style, box, glyph.Cell) ?? glyph.Glyph });
        }

        cells.PutLine(rounded, path.IsClosed, look.Joins, covered);
    }

    private static bool IsCornerOf(CellRect box, CellPoint point) =>
        (point.X == box.Left || point.X == box.Right - 1) && (point.Y == box.Top || point.Y == box.Bottom - 1);

    // A rounded corner: . at the top and ' at the bottom, or the rounded box-drawing corners in Box. Heavy and double
    // box-drawing lines have no rounded corners, so theirs stay square.
    private static Rune? RoundedCorner(LineStyle style, CellRect box, CellPoint corner)
    {
        var isTop = corner.Y == box.Top;
        var isLeft = corner.X == box.Left;
        return style switch
        {
            LineStyle.HeavyBox or LineStyle.DoubleBox => null,
            LineStyle.Box when isTop => isLeft ? _roundedBoxTopLeft : _roundedBoxTopRight,
            LineStyle.Box => isLeft ? _roundedBoxBottomLeft : _roundedBoxBottomRight,
            _ => isTop ? _roundedTopCorner : _roundedBottomCorner
        };
    }

    private static CanvasPoint MiddleOf(int x, int y) => new(x + 0.5, y + 0.5);

    // What a drag draws, kept from its start: the options, with the style it's drawn in, the colours and the picker for
    // the document's font.
    private sealed record Look(LineStyle Style, bool Joins, ShapeFill Fill, bool IsRounded, Rune Character,
        LineGlyphPicker Picker, CellColor Foreground, CellColor Background);

    // One drag's shape: where it started, and where it has got to.
    private sealed class Drag(CellPoint start, Look look, LiveShape shape)
    {
        public CellPoint Start { get; } = start;

        public Look Look { get; } = look;

        public LiveShape Shape { get; } = shape;

        public CellPoint Pointer { get; set; }

        public ToolModifiers Modifiers { get; set; }

        // The box the drawn shape fills, or null while nothing is drawn.
        public CellRect? Box { get; set; }
    }
}
