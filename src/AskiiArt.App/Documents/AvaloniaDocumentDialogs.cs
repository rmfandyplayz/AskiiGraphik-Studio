// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Documents;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AskiiArt.App.Documents;

/// <summary><see cref="IDocumentDialogs"/> for the real app: modal windows owned by the main window.</summary>
internal sealed class AvaloniaDocumentDialogs(FontCatalog fonts) : IDocumentDialogs
{
    private static readonly FilePickerFileType _fontFiles = new(Strings.FontFileTypeName)
    {
        Patterns = ["*.ttf", "*.otf", "*.ttc"],
        AppleUniformTypeIdentifiers = ["public.truetype-ttf-font", "public.opentype-font", "public.truetype-collection-font"],
        MimeTypes = ["font/ttf", "font/otf", "font/collection"]
    };

    public async Task<NewDocumentSettings?> ShowNewDocumentAsync()
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new NewDocumentWindow { DataContext = new NewDocumentViewModel() };
        return await dialog.ShowDialog<NewDocumentSettings?>(owner);
    }

    public async Task<CanvasResizeRequest?> ShowResizeCanvasAsync(CanvasSize currentSize)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new ResizeCanvasWindow { DataContext = new ResizeCanvasViewModel(currentSize) };
        return await dialog.ShowDialog<CanvasResizeRequest?>(owner);
    }

    public async Task<FontReference?> ShowFontPickerAsync(FontReference currentFont)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new FontPickerWindow();
        dialog.DataContext = new FontPickerViewModel(fonts, currentFont, () => PickFontFileAsync(dialog));
        return await dialog.ShowDialog<FontReference?>(owner);
    }

    // The file picker belongs to the font picker, so it opens over it.
    private static async Task<string?> PickFontFileAsync(TopLevel owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.FontFilePickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [_fontFiles]
        });

        if (files.Count == 0)
            return null;

        return files[0].TryGetLocalPath();
    }
}
