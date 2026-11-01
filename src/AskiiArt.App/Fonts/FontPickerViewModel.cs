// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.Globalization;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Fonts;

/// <summary>A row in the font list.</summary>
internal sealed record FontPickerItem(FontReference Font, string DisplayName);

/// <summary>
/// The font picker: the bundled, loaded and installed fonts, a search box, and Load from File.
/// Only the family is picked; the size at 100% zoom is the same for every document (ADR 0019).
/// </summary>
internal sealed partial class FontPickerViewModel : ObservableObject
{
    private readonly FontCatalog _fonts;
    private readonly Func<Task<string?>> _pickFontFile;
    private readonly ILogger _logger;
    private List<FontPickerItem> _allFonts = [];

    /// <param name="fonts">Where the fonts come from.</param>
    /// <param name="currentFont">The document's font, selected to begin with.</param>
    /// <param name="pickFontFile">Asks the user for a font file; returns its path, or null if they cancel.</param>
    /// <param name="logger">Records why a font file couldn't be loaded.</param>
    public FontPickerViewModel(FontCatalog fonts, FontReference currentFont, Func<Task<string?>> pickFontFile,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(currentFont);
        ArgumentNullException.ThrowIfNull(pickFontFile);
        ArgumentNullException.ThrowIfNull(logger);

        _fonts = fonts;
        _pickFontFile = pickFontFile;
        _logger = logger;
        LoadFromFileCommand = new AsyncRelayCommand(LoadFromFileAsync);

        RefreshFonts();
        SelectedFont = Fonts.FirstOrDefault(item => item.Font == currentFont);
    }

    /// <summary>The fonts whose name contains <see cref="SearchText"/>.</summary>
    public ObservableCollection<FontPickerItem> Fonts { get; } = [];

    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                ApplySearch();
        }
    } = string.Empty;

    public FontPickerItem? SelectedFont
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsNotMonospace));
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(CanAccept));
        }
    }

    /// <summary>True when the selected font's characters aren't all the same width, so the user is warned (§11).</summary>
    public bool IsNotMonospace => SelectedFont is { } item && !_fonts.IsMonospace(item.Font);

    /// <summary>What happened when a font file was loaded, or null.</summary>
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public IAsyncRelayCommand LoadFromFileCommand { get; }

    /// <summary>The font to switch to, or null when none is selected.</summary>
    public FontReference? Result => SelectedFont?.Font;

    public bool CanAccept => Result is not null;

    private async Task LoadFromFileAsync()
    {
        if (await _pickFontFile() is not { } path)
            return;

        try
        {
            var (font, wasAdded) = _fonts.AddFile(path);
            if (wasAdded)
                Message = null;
            else
                Message = string.Format(CultureInfo.CurrentCulture, Strings.FontAlreadyAvailableFormat, font);

            RefreshFonts();
            SearchText = string.Empty;
            SelectedFont = Fonts.FirstOrDefault(item => item.Font == font);
        }
        catch (Exception exception) when (exception is FontLoadException or IOException or UnauthorizedAccessException)
        {
            LogCouldNotLoadFontFile(exception, path);
            Message = Strings.FontLoadFailed;
        }
    }

    private void RefreshFonts()
    {
        _allFonts = [.. _fonts.GetChoices().Select(choice => new FontPickerItem(choice.Font, DisplayNameOf(choice)))];
        ApplySearch();
    }

    private void ApplySearch()
    {
        var selected = SelectedFont;
        Fonts.Clear();
        foreach (var item in _allFonts)
        {
            if (item.DisplayName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
                Fonts.Add(item);
        }

        // A font the search hides can't stay selected, or OK would pick something not on screen.
        if (selected is not null && Fonts.Contains(selected))
            SelectedFont = selected;
        else
            SelectedFont = null;
    }

    private static string DisplayNameOf(FontChoice choice)
    {
        if (choice.Source == FontSource.System)
            return choice.Font.FamilyName;

        var format = Strings.FontSourceFileFormat;
        if (choice.Source == FontSource.Bundled)
            format = Strings.FontSourceBundledFormat;

        return string.Format(CultureInfo.CurrentCulture, format, choice.Font.FamilyName);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The font file {FontPath} could not be loaded")]
    private partial void LogCouldNotLoadFontFile(Exception exception, string fontPath);
}
