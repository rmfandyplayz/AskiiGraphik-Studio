// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AskiiArt.App.Canvas.FrameRate;
using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's view of the active document: what it draws (the composite, font and atlases), how
/// (zoom, grid, the cell under the pointer), and the tool that edits it. Zoom and grid are view
/// settings, not document changes, so they don't go through commands or undo. When New replaces
/// the document, all of these switch to the new one.
/// </summary>
internal sealed class CanvasViewModel : ObservableObject, IDisposable
{
    /// <summary>The font size at 100% zoom, in logical pixels. It's the same for every document (ADR 0019).</summary>
    public const double DefaultFontSize = 16;

    /// <summary>The letter that swaps the FG and BG colours (ADR 0042).</summary>
    public const char SwapColorsLetter = 'X';

    /// <summary>The letter that sets both colours to none. A temporary key (ADR 0042).</summary>
    public const char ResetColorsLetter = 'D';

    // The size a font's cell shape is measured at. Big enough that rounding to whole pixels barely changes it.
    private const float CellShapePixelSize = 64;

    private readonly ActiveDocument _activeDocument;
    private readonly ToolRegistry _tools;
    private readonly FontCatalog _fonts;
    private readonly GlyphDensityCache _densities;

    // Each group's tool that was chosen last, by group id, so choosing the group brings it back (ADR 0049).
    private readonly Dictionary<string, string> _lastToolOfGroup = [];
    private string _selectedToolId = HandTool.Id;
    private double _cellWidthToHeight;

    public CanvasViewModel(ActiveDocument activeDocument, ToolRegistry tools, PaintColors colors,
        ToolSettings toolSettings, FontCatalog fonts, GlyphAtlasCache atlases, GlyphDensityCache densities)
    {
        ArgumentNullException.ThrowIfNull(activeDocument);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(toolSettings);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(atlases);
        ArgumentNullException.ThrowIfNull(densities);

        _activeDocument = activeDocument;
        _tools = tools;
        Colors = colors;
        ToolSettings = toolSettings;
        _fonts = fonts;
        Atlases = atlases;
        _densities = densities;

        Attach(activeDocument.Session);
        activeDocument.SessionChanged += OnSessionChanged;
    }

    public EditingSession Session { get; private set; }

    /// <summary>The document's selection, which the canvas outlines (ADR 0055).</summary>
    public SessionSelection Selection => Session.Selection;

    public ITool ActiveTool { get; private set; }

    /// <summary>The tools that can be chosen.</summary>
    public IReadOnlyList<ToolDefinition> Tools => _tools.All;

    /// <summary>The chosen tool's definition, which says what options it has.</summary>
    public ToolDefinition SelectedTool => _tools.Get(_selectedToolId);

    /// <summary>The groups the tools are chosen in, in the order the Tools toolbar shows them (ADR 0049).</summary>
    public IReadOnlyList<ToolGroup> ToolGroups => _tools.Groups;

    /// <summary>The chosen tool's group.</summary>
    public ToolGroup SelectedGroup => _tools.GroupOf(_selectedToolId);

    /// <summary>The FG and BG colours the tools paint with, shared by every document.</summary>
    public PaintColors Colors { get; }

    /// <summary>The tools' option values, such as the brush size, shared by every document (ADR 0047).</summary>
    public ToolSettings ToolSettings { get; }

    public CompositeCache Composite { get; private set; }

    /// <summary>The document's font, or the default font when it isn't available.</summary>
    public LoadedFont Font { get; private set; }

    public GlyphAtlasCache Atlases { get; }

    /// <summary>The Debug frame-rate readout in the status bar.</summary>
    public FrameRateReadout FrameRate { get; } = new();

    public double FontSize { get; } = DefaultFontSize;

    /// <summary>The zoom, where 1 is 100%. Kept between <see cref="ZoomLevels.Minimum"/> and <see cref="ZoomLevels.Maximum"/>.</summary>
    public double ZoomFactor
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Clamp(value, ZoomLevels.Minimum, ZoomLevels.Maximum)))
                OnPropertyChanged(nameof(ZoomText));
        }
    } = ZoomLevels.ActualSize;

    public bool IsGridVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Whether the mouse presses like a pen at half pressure, from the Debug menu (ADR 0054).</summary>
    public bool IsMouseActingAsPen
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>The cell under the pointer, reported by the canvas control.</summary>
    public CellPoint? HoveredCell
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HoveredCellText));
        }
    }

    /// <summary>
    /// Where the pointer is over the canvas view, in cells, or null when it's elsewhere, reported by the canvas control.
    /// Paste centres on it (ADR 0067). Nothing watches it, so it raises no change.
    /// </summary>
    public CanvasPoint? PointerCanvasPosition { get; set; }

    /// <summary>The spot of the canvas in the middle of the view, in cells, reported by the canvas control.</summary>
    public CanvasPoint ViewCentre { get; set; }

    public bool CanZoomIn => ZoomLevels.StepIn(ZoomFactor) > ZoomFactor;

    public bool CanZoomOut => ZoomLevels.StepOut(ZoomFactor) < ZoomFactor;

    public string ZoomText => string.Format(CultureInfo.CurrentCulture, Strings.StatusZoomFormat, ZoomFactor * 100);

    public string HoveredCellText
    {
        get
        {
            if (HoveredCell is not { } cell)
                return string.Empty;

            return string.Format(CultureInfo.CurrentCulture, Strings.StatusCellFormat, cell.X, cell.Y);
        }
    }

    public string CanvasSizeText => string.Format(CultureInfo.CurrentCulture, Strings.StatusCanvasSizeFormat,
        Composite.Size.Width, Composite.Size.Height);

    // A document keeps asking for its own font even when another is drawn in its place (§5.10).
    public string FontText
    {
        get
        {
            var documentFont = Session.Document.Font;
            if (new FontReference(Font.FamilyName) == documentFont)
                return documentFont.FamilyName;

            return string.Format(CultureInfo.CurrentCulture, Strings.StatusFontNotAvailableFormat,
                documentFont.FamilyName);
        }
    }

    public string CharacterSetText => Session.Document.CharacterSet switch
    {
        CharacterSet.Ascii => Strings.StatusCharacterSetAscii,
        _ => Strings.StatusCharacterSetUnicode
    };

    public void ZoomIn() => ZoomFactor = ZoomLevels.StepIn(ZoomFactor);

    public void ZoomOut() => ZoomFactor = ZoomLevels.StepOut(ZoomFactor);

    public void ZoomToActualSize() => ZoomFactor = ZoomLevels.ActualSize;

    public void ToggleGrid() => IsGridVisible = !IsGridVisible;

    public void ToggleMouseActingAsPen() => IsMouseActingAsPen = !IsMouseActingAsPen;

    /// <summary>Makes another tool active. It stays chosen when New or Open replaces the document.</summary>
    public void SelectTool(string toolId)
    {
        if (toolId == _selectedToolId)
            return;

        var definition = _tools.Get(toolId);
        var group = _tools.GroupOf(toolId);
        var isOtherGroup = group.Id != SelectedGroup.Id;
        ActiveTool.Deactivate();
        _selectedToolId = toolId;
        _lastToolOfGroup[group.Id] = toolId;
        ActiveTool = definition.Create(CreateToolContext(Session));
        OnPropertyChanged(nameof(SelectedTool));
        if (isOtherGroup)
            OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(ActiveTool));
    }

    /// <summary>Each group's tool that was chosen last, by group id. A group whose tool hasn't been chosen isn't there.
    /// </summary>
    public IReadOnlyDictionary<string, string> LastToolOfGroup => _lastToolOfGroup;

    /// <summary>
    /// Sets the tool each group brings back when it's chosen, such as the ones saved last time, without choosing any.
    /// A group or tool that doesn't exist, or a tool that isn't in its group, is ignored.
    /// </summary>
    public void RestoreLastTools(IReadOnlyDictionary<string, string> lastToolOfGroup)
    {
        ArgumentNullException.ThrowIfNull(lastToolOfGroup);

        foreach (var (groupId, toolId) in lastToolOfGroup)
        {
            var group = _tools.Groups.FirstOrDefault(group => group.Id == groupId);
            if (group is not null && group.ToolIds.Contains(toolId))
                _lastToolOfGroup[groupId] = toolId;
        }
    }

    /// <summary>Chooses the group's tool that was chosen last, or its first tool if none has been (ADR 0049).</summary>
    public void SelectToolGroup(string groupId)
    {
        var group = _tools.GetGroup(groupId);
        SelectTool(_lastToolOfGroup.GetValueOrDefault(group.Id, group.ToolIds[0]));
    }

    /// <summary>
    /// Uses a single-letter shortcut (ADR 0033): a tool group's letter chooses that group, a tool's own letter chooses
    /// that tool (ADR 0063), <see cref="SwapColorsLetter"/> swaps the colours and <see cref="ResetColorsLetter"/> sets
    /// both to none.
    /// Returns false when the letter isn't a shortcut, or when the active tool is taking text, which is what the
    /// letter is for then.
    /// </summary>
    public bool TryUseShortcutLetter(char letter)
    {
        if (ActiveTool.IsTakingText)
            return false;

        var wanted = char.ToUpperInvariant(letter);
        if (ToolGroups.FirstOrDefault(group => group.ShortcutLetter == wanted) is { } group)
        {
            SelectToolGroup(group.Id);
            return true;
        }

        foreach (var toolGroup in ToolGroups)
        {
            if (toolGroup.ToolLetters.TryGetValue(wanted, out var toolId))
            {
                SelectTool(toolId);
                return true;
            }
        }

        switch (wanted)
        {
            case SwapColorsLetter:
                Colors.Swap();
                return true;
            case ResetColorsLetter:
                Colors.Reset();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Empties the active layer's selected cells, for the Delete and Backspace keys on the canvas (ADR 0065). Returns
    /// false, leaving the key unused, when nothing is selected or the active layer is locked or hidden.
    /// </summary>
    public bool TryDeleteSelectedCells()
    {
        if (!SelectionContents.CanClear(Session))
            return false;

        SelectionContents.Clear(Session);
        return true;
    }

    /// <summary>
    /// Uses a number key typed on the canvas (ADR 0049): 1 chooses the first tool of the chosen tool's group, 2 the
    /// second and so on. Returns false when the group has no tool with that number, or when the active tool is
    /// taking text, which is what the key is for then.
    /// </summary>
    public bool TryUseNumberKey(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (ActiveTool.IsTakingText || text is not [>= '1' and <= '9' and var digit])
            return false;

        var toolIds = SelectedGroup.ToolIds;
        var index = digit - '1';
        if (index >= toolIds.Count)
            return false;

        SelectTool(toolIds[index]);
        return true;
    }

    /// <summary>
    /// Switches to the Hand tool, as Esc does from any tool, so a tool is always chosen (ADR 0041). Returns
    /// false when it's already the active one.
    /// </summary>
    public bool TrySwitchToHandTool()
    {
        if (_selectedToolId == HandTool.Id)
            return false;

        SelectTool(HandTool.Id);
        return true;
    }

    public void Dispose()
    {
        _activeDocument.SessionChanged -= OnSessionChanged;
        Detach();
        FrameRate.Dispose();
    }

    [MemberNotNull(nameof(Session), nameof(Composite), nameof(ActiveTool), nameof(Font))]
    private void Attach(EditingSession session)
    {
        Session = session;
        Composite = new CompositeCache(session.Document);
        Composite.Changed += OnCompositeChanged;
        session.Document.Changed += OnDocumentChanged;
        Font = _fonts.Resolve(session.Document.Font);
        _cellWidthToHeight = CellWidthToHeightOf(Font);
        ActiveTool = _tools.Get(_selectedToolId).Create(CreateToolContext(session));
    }

    // The tool asks for the cell shape and the glyph densities each time it needs them, because the font can change
    // while it's in use.
    private ToolContext CreateToolContext(EditingSession session) =>
        new(session, Colors, ToolSettings, () => _cellWidthToHeight, () => _densities.GetMeter(Font));

    private static double CellWidthToHeightOf(LoadedFont font)
    {
        var cell = CellMetrics.Measure(font, CellShapePixelSize);
        return (double)cell.Width / cell.Height;
    }

    private void Detach()
    {
        ActiveTool.Deactivate();
        Session.Document.Changed -= OnDocumentChanged;
        Composite.Changed -= OnCompositeChanged;
        Composite.Dispose();
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        Detach();
        Attach(_activeDocument.Session);

        OnPropertyChanged(nameof(Session));
        OnPropertyChanged(nameof(Selection));
        OnPropertyChanged(nameof(Composite));
        OnPropertyChanged(nameof(Font));
        OnPropertyChanged(nameof(ActiveTool));
        OnPropertyChanged(nameof(CanvasSizeText));
        OnPropertyChanged(nameof(CharacterSetText));
        OnPropertyChanged(nameof(FontText));
    }

    private void OnCompositeChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(CanvasSizeText));

    // A font change (or undoing one) redraws the canvas with the new font's cell size.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (!e.SettingsChanged)
            return;

        OnPropertyChanged(nameof(CharacterSetText));

        var font = _fonts.Resolve(Session.Document.Font);
        if (font == Font)
            return;

        Font = font;
        _cellWidthToHeight = CellWidthToHeightOf(font);
        OnPropertyChanged(nameof(Font));
        OnPropertyChanged(nameof(FontText));
    }
}
