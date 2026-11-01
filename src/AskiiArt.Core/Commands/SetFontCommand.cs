// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Changes the font the document is drawn with (ARCHITECTURE §5.10, ADR 0019). The glyphs stay as
/// they are; the cell size follows the new font's metrics.
/// </summary>
public sealed class SetFontCommand : IDocumentCommand
{
    private const long EstimatedSizeInBytes = 64;

    private FontReference? _oldFont;

    /// <summary>Creates a command that switches the document to <paramref name="newFont"/>.</summary>
    public SetFontCommand(FontReference newFont)
    {
        ArgumentNullException.ThrowIfNull(newFont);

        NewFont = newFont;
    }

    /// <summary>The font after the change.</summary>
    public FontReference NewFont { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => EstimatedSizeInBytes;

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldFont = document.Font;
        document.SetFont(NewFont);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_oldFont is null)
            throw new InvalidOperationException("The command hasn't been applied.");

        document.SetFont(_oldFont);
    }
}
