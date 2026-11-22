// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tests.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tests.Tools;

// How the Brush and the Soft Eraser pick a glyph by where the tip covers an edge cell (ADR 0068). A hard circle 5
// cells wide covers the top of the cells either side of its top middle cell about 30% and their bottom almost fully,
// and the same the other way up at its bottom.
public sealed class ShapedRampStrokeTests
{
    // Levels 1 to 4, at intensities 1/4 to 1: 'v' inks the bottom of its cell, '=' all of it evenly, '^' mostly the
    // top, and '#' all of it.
    private const string RampId = "user.shapes";

    private static readonly IGlyphDensityProvider _glyphs = new FakeGlyphDensities(
        new Dictionary<char, double> { ['='] = 0.5, ['#'] = 1 },
        new Dictionary<char, GlyphShape>
        {
            ['v'] = TestShapes.Bands(top: 0, bottom: 2 / 3.0),
            ['^'] = TestShapes.Bands(top: 1, bottom: 1 / 3.0)
        });

    private readonly EditingSession _session = new(TestCells.CreateDocument(7, 7));
    private readonly RampLibrary _ramps = new([], [new UserRamp(RampId, "Shapes", Ramp.FromText("v=^#"))]);
    private readonly ToolOptionValues _tip = new(BrushTool.TipOptions);
    private readonly ToolOptionValues _ramp;

    public ShapedRampStrokeTests()
    {
        _ramp = new ToolSettings(_ramps).For(RampOptions.Group);
        _ramp.Set(RampOptions.Ramp, RampId);
        TestTools.SetTip(_tip, 5, "circle");
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    // Its tone is nearer '=' or '^', but the tip covers the bottom of the cell.
    [Fact]
    public void TheBrush_GivesACellAtTheTopOfTheTip_AGlyphInkedAtTheBottom()
    {
        var brush = CreateBrush(softness: 0);

        Click(brush, 3, 3);

        var rows = TestCells.Read(Layer);
        Assert.Equal('v', rows[1][2]);
        Assert.Equal('v', rows[1][4]);
        Assert.Equal('#', rows[3][3]);
    }

    [Fact]
    public void TheBrush_GivesACellAtTheBottomOfTheTip_AGlyphInkedAtTheTop()
    {
        var brush = CreateBrush(softness: 0);

        Click(brush, 3, 3);

        var rows = TestCells.Read(Layer);
        Assert.Equal('^', rows[5][2]);
        Assert.Equal('^', rows[5][4]);
    }

    // What's left of the cell is the top, away from the hole.
    [Fact]
    public void TheSoftEraser_LeavesACellAtTheTopOfTheHole_InkedAtTheTop()
    {
        TestCells.Paint(Layer, "#######", "#######", "#######", "#######", "#######", "#######", "#######");
        var eraser = CreateSoftEraser();

        Click(eraser, 3, 3);

        var rows = TestCells.Read(Layer);
        Assert.Equal('^', rows[1][2]);
        Assert.Equal('v', rows[5][2]);
        Assert.Equal('·', rows[3][3]);
    }

    // Each part moves once, by the greatest weight that has reached it, so pressing harder where the tip is takes the
    // cells as far as pressing that hard from the start.
    [Fact]
    public void PressingHarder_TakesShapedCellsAsFarAsPressingThatHardFromTheStart()
    {
        var brush = CreateBrush(softness: 50);
        Click(brush, 3, 3);
        var pressedHard = TestCells.Read(Layer);
        _session.History.Undo();

        brush.PointerPressed(PointerAt(3, 3, pressure: 0.3f));
        brush.PointerMoved(PointerAt(3, 3, pressure: 0.6f));
        brush.PointerMoved(PointerAt(3, 3, pressure: 1));
        brush.PointerReleased(PointerAt(3, 3));

        Assert.Equal(pressedHard, TestCells.Read(Layer));
    }

    private BrushTool CreateBrush(int softness)
    {
        var options = new ToolOptionValues(RampBrushOptions.Group);
        options.Set(RampBrushOptions.Softness, softness);
        return new BrushTool(_session, new PaintColors(), TestTools.Tip(_tip), TestTools.Ramp(_ramp, _ramps), options,
            () => _glyphs);
    }

    private SoftEraserTool CreateSoftEraser()
    {
        var options = new ToolOptionValues(SoftEraserOptions.Group);
        options.Set(SoftEraserOptions.Softness, 0);
        return new SoftEraserTool(_session, TestTools.Tip(_tip), TestTools.Ramp(_ramp, _ramps), options,
            () => _glyphs);
    }

    private static void Click(ITool tool, int x, int y)
    {
        tool.PointerPressed(PointerAt(x, y));
        tool.PointerReleased(PointerAt(x, y));
    }

    private static ToolPointerInput PointerAt(int x, int y, float pressure = 1) =>
        new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None, pressure);
}
