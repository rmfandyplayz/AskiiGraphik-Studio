// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>
/// The two current colours tools paint with (ADR 0042): the FG colour for text, and the BG colour for
/// backgrounds. They're shared by every open document, so each is kept as chosen and converted to a
/// document's colour mode only when used. An unset colour means none: text in the canvas's own ink,
/// and no new background. One of the two is chosen (<see cref="Target"/>): it's the one the Color and Swatches
/// windows change, and the one the Brush paints with (ADR 0043).
/// </summary>
/// <remarks>Used on the UI thread only, like the tools.</remarks>
public sealed class PaintColors
{
    private CellColor _foreground;
    private CellColor _background;
    private PaintColorTarget _target;

    /// <summary>Raised once after either colour changes, including after a swap.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised after <see cref="Target"/> changes.</summary>
    public event EventHandler? TargetChanged;

    /// <summary>The FG colour, as chosen. Text is typed in it, and the FG brush paints it.</summary>
    public CellColor Foreground
    {
        get => _foreground;
        set => SetColors(value, _background);
    }

    /// <summary>The BG colour, as chosen. Typing puts it behind the text, and the BG brush paints it.</summary>
    public CellColor Background
    {
        get => _background;
        set => SetColors(_foreground, value);
    }

    /// <summary>
    /// Which colour is chosen: the one the Color and Swatches windows change, and the one the Brush paints with.
    /// It's one choice for both for now, at the owner's request. To part them, the Brush would get a choice of
    /// its own here, because tools are made afresh each time one is chosen (ADR 0043).
    /// </summary>
    public PaintColorTarget Target
    {
        get => _target;
        set
        {
            if (value == _target)
                return;

            _target = value;
            TargetChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>The chosen colour, as chosen.</summary>
    public CellColor TargetColor
    {
        get => _target == PaintColorTarget.Foreground ? _foreground : _background;
        set
        {
            if (_target == PaintColorTarget.Foreground)
                Foreground = value;
            else
                Background = value;
        }
    }

    /// <summary>Swaps the FG and BG colours.</summary>
    public void Swap() => SetColors(_background, _foreground);

    /// <summary>Sets both colours to none.</summary>
    public void Reset() => SetColors(CellColor.Unset, CellColor.Unset);

    /// <summary>The FG colour as a document in <paramref name="colorMode"/> would hold it.</summary>
    public CellColor ForegroundIn(ColorMode colorMode) => colorMode.Convert(_foreground);

    /// <summary>The BG colour as a document in <paramref name="colorMode"/> would hold it.</summary>
    public CellColor BackgroundIn(ColorMode colorMode) => colorMode.Convert(_background);

    /// <summary>The chosen colour as a document in <paramref name="colorMode"/> would hold it.</summary>
    public CellColor TargetColorIn(ColorMode colorMode) => colorMode.Convert(TargetColor);

    private void SetColors(CellColor foreground, CellColor background)
    {
        if (foreground == _foreground && background == _background)
            return;

        _foreground = foreground;
        _background = background;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
