// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Painting;

/// <summary>
/// What the Brush Options toolbar and the Color and Swatches windows show and change (ADR 0042): the two
/// current colours, shown as the active document would hold them, and which of the two the Color and Swatches
/// windows change. It lives as long as the app, like the other windows' content, and follows the active
/// document's colour mode.
/// </summary>
internal sealed class PaintColorsViewModel : ObservableObject
{
    private readonly ActiveDocument _activeDocument;
    private Document _followedDocument;

    // What the picker shows while the colour it changes is none, so it stays where it was last dragged.
    private RgbColor _pickerColorForNone = AnsiPalette.Vga[AnsiColor.White];

    public PaintColorsViewModel(PaintColors colors, ActiveDocument activeDocument)
    {
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(activeDocument);

        Colors = colors;
        _activeDocument = activeDocument;
        _followedDocument = activeDocument.Session.Document;
        _followedDocument.Changed += OnDocumentChanged;
        activeDocument.SessionChanged += OnSessionChanged;
        colors.Changed += (_, _) => NotifyColorsChanged();

        Swatches =
        [
            .. Enum.GetValues<AnsiColor>().Select(color => new SwatchItem(ToBrush(AnsiPalette.Vga[color]),
                AnsiColorNames.Get(color), new RelayCommand(() => SetTargetColor(CellColor.FromAnsi(color))))),
        ];
        ChooseNoneCommand = new RelayCommand(() => SetTargetColor(CellColor.Unset));
    }

    public PaintColors Colors { get; }

    /// <summary>Which colour the Color and Swatches windows change. The Brush Options toolbar chooses it.</summary>
    public PaintColorTarget Target
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsForegroundTarget));
            OnPropertyChanged(nameof(IsBackgroundTarget));
            OnPropertyChanged(nameof(TargetText));
            OnPropertyChanged(nameof(PickerColor));
        }
    }

    // One radio button binds to each. A button being unticked is the other being ticked, so false is ignored.
    public bool IsForegroundTarget
    {
        get => Target == PaintColorTarget.Foreground;
        set => ChooseTargetIf(value, PaintColorTarget.Foreground);
    }

    public bool IsBackgroundTarget
    {
        get => Target == PaintColorTarget.Background;
        set => ChooseTargetIf(value, PaintColorTarget.Background);
    }

    public ColorMode ColorMode => _activeDocument.Session.Document.ColorMode;

    public bool IsMonochrome => ColorMode == ColorMode.Monochrome;

    public bool CanChooseSwatches => !IsMonochrome;

    /// <summary>
    /// The picker works in RGB documents only: in an ANSI one, it would show colours that can't be used.
    /// </summary>
    public bool CanUsePicker => ColorMode == ColorMode.Rgb;

    /// <summary>The FG colour as the active document would hold it, or null for none.</summary>
    public IBrush? ForegroundBrush => ToBrush(Colors.ForegroundIn(ColorMode));

    /// <summary>The BG colour as the active document would hold it, or null for none.</summary>
    public IBrush? BackgroundBrush => ToBrush(Colors.BackgroundIn(ColorMode));

    public string ForegroundText => Describe(Strings.PaintColorForegroundFormat, Colors.ForegroundIn(ColorMode));

    public string BackgroundText => Describe(Strings.PaintColorBackgroundFormat, Colors.BackgroundIn(ColorMode));

    /// <summary>The FG button's tooltip: the colour, and what the button and the X and D keys do.</summary>
    public string ForegroundTip => Tip(ForegroundText);

    /// <summary>The BG button's tooltip: the colour, and what the button and the X and D keys do.</summary>
    public string BackgroundTip => Tip(BackgroundText);

    /// <summary>Which colour the Color and Swatches windows change, for when Brush Options isn't showing.</summary>
    public string TargetText => Target == PaintColorTarget.Foreground
        ? Strings.PaintColorTargetForeground
        : Strings.PaintColorTargetBackground;

    /// <summary>The colour the picker shows: the one it changes, as RGB.</summary>
    public Color PickerColor
    {
        get
        {
            var rgb = ToRgb(TargetColorInDocument) ?? _pickerColorForNone;
            return Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue);
        }
    }

    /// <summary>The 16 ANSI colours, in the order of their escape codes.</summary>
    public IReadOnlyList<SwatchItem> Swatches { get; }

    /// <summary>Sets the colour the windows change to none.</summary>
    public IRelayCommand ChooseNoneCommand { get; }

    /// <summary>
    /// The user chose <paramref name="color"/> in the picker. Its alpha is ignored: cells are opaque.
    /// </summary>
    public void PickColor(Color color)
    {
        var rgb = new RgbColor(color.R, color.G, color.B);
        _pickerColorForNone = rgb;
        SetTargetColor(CellColor.FromRgb(rgb));
    }

    private CellColor TargetColorInDocument => Target == PaintColorTarget.Foreground
        ? Colors.ForegroundIn(ColorMode)
        : Colors.BackgroundIn(ColorMode);

    private static RgbColor? ToRgb(CellColor color)
    {
        if (color.TryGetRgb(out var rgb))
            return rgb;

        if (color.TryGetAnsi(out var ansi))
            return AnsiPalette.Vga[ansi];

        return null;
    }

    private static ImmutableSolidColorBrush? ToBrush(CellColor color) => ToRgb(color) is { } rgb ? ToBrush(rgb) : null;

    private static ImmutableSolidColorBrush ToBrush(RgbColor rgb) =>
        new(Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue));

    private static string Describe(string format, CellColor color)
    {
        string name;
        if (color.TryGetAnsi(out var ansi))
            name = AnsiColorNames.Get(ansi);
        else if (color.TryGetRgb(out var rgb))
            name = rgb.ToString().ToUpperInvariant();
        else
            name = Strings.PaintColorNone;

        return string.Format(CultureInfo.CurrentCulture, format, name);
    }

    private void SetTargetColor(CellColor color)
    {
        if (Target == PaintColorTarget.Foreground)
            Colors.Foreground = color;
        else
            Colors.Background = color;
    }

    private void ChooseTargetIf(bool isChosen, PaintColorTarget target)
    {
        if (isChosen)
            Target = target;
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _followedDocument.Changed -= OnDocumentChanged;
        _followedDocument = _activeDocument.Session.Document;
        _followedDocument.Changed += OnDocumentChanged;
        NotifyColorModeChanged();
    }

    // A colour mode change (or undoing one) changes how the colours are shown.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.SettingsChanged)
            NotifyColorModeChanged();
    }

    private void NotifyColorModeChanged()
    {
        OnPropertyChanged(nameof(ColorMode));
        OnPropertyChanged(nameof(IsMonochrome));
        OnPropertyChanged(nameof(CanChooseSwatches));
        OnPropertyChanged(nameof(CanUsePicker));
        NotifyColorsChanged();
    }

    private void NotifyColorsChanged()
    {
        OnPropertyChanged(nameof(ForegroundBrush));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(ForegroundText));
        OnPropertyChanged(nameof(BackgroundText));
        OnPropertyChanged(nameof(ForegroundTip));
        OnPropertyChanged(nameof(BackgroundTip));
        OnPropertyChanged(nameof(PickerColor));
    }

    private static string Tip(string colorText) =>
        string.Format(CultureInfo.CurrentCulture, Strings.BrushOptionsSwatchTipFormat, colorText);
}
