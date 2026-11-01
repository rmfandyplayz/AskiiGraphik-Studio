// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.App.Documents;

/// <summary>
/// The dialogs the document actions ask the user through. Each returns null when the user
/// cancels. Tests replace this with a fake that answers straight away.
/// </summary>
internal interface IDocumentDialogs
{
    Task<NewDocumentSettings?> ShowNewDocumentAsync();

    Task<CanvasResizeRequest?> ShowResizeCanvasAsync(CanvasSize currentSize);

    Task<FontReference?> ShowFontPickerAsync(FontReference currentFont);
}

/// <summary>What the New dialog asks for.</summary>
internal sealed record NewDocumentSettings(CanvasSize Size, CharacterSet CharacterSet);

/// <summary>What the Resize Canvas dialog asks for.</summary>
internal sealed record CanvasResizeRequest(CanvasSize Size, ResizeAnchor Anchor);
