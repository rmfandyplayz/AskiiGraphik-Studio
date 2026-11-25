// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Libraries;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Options;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Characters;

/// <summary>
/// The Characters window's content (ADR 0053): a dropdown of character sets, with Recent first, then the user's sets,
/// then the built-in ones, and a grid of the chosen set's characters. Clicking a character chooses it for the
/// Character Brush and switches to that brush. The user's sets change as they're edited, as soon as what's typed is
/// valid; until then the window says why it isn't. Built-in sets hide the characters no font can draw. It lives as
/// long as the app, so reopening the window shows the same state.
/// </summary>
internal sealed class CharactersPanelViewModel : ObservableObject
{
    private readonly CanvasViewModel _canvas;
    private readonly GlyphSetLibrary _sets;
    private readonly RecentGlyphs _recent;
    private readonly ToolOptionValues _options;
    private readonly IAppShell _shell;
    private readonly IGlyphSetFileDialogs _dialogs;
    private readonly Func<Rune, bool> _canDraw;
    private readonly GlyphSetRow _recentRow;
    private Document _document;

    // While the editor is being filled from the chosen set, its fields aren't edits.
    private bool _isFillingEditor;

    // While an edit is being applied, the library's change doesn't refill the editor, which already shows it.
    private bool _isApplyingEdit;

    private GlyphSetRow? _selectedSet;
    private string _name = "";
    private string _characters = "";
    private string _newCharacters = "";

    /// <summary>Creates the window's content.</summary>
    /// <param name="canvas">The canvas, whose document, tools and Character Brush settings the window follows.</param>
    /// <param name="sets">The user's sets, which it shows and edits.</param>
    /// <param name="shell">Asks before deleting, and says what went wrong.</param>
    /// <param name="dialogs">The pickers for character set files.</param>
    /// <param name="canDraw">Whether any font can draw a character, now. Built-in sets show only those.</param>
    public CharactersPanelViewModel(CanvasViewModel canvas, GlyphSetLibrary sets, IAppShell shell,
        IGlyphSetFileDialogs dialogs, Func<Rune, bool> canDraw)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(sets);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(canDraw);

        _canvas = canvas;
        _sets = sets;
        _recent = canvas.ToolSettings.RecentGlyphs;
        _options = canvas.ToolSettings.For(CharacterBrushOptions.Group);
        _shell = shell;
        _dialogs = dialogs;
        _canDraw = canDraw;
        _document = canvas.Session.Document;
        AddCommand = new RelayCommand(AddNewSet, () => CanAdd);

        _recentRow = GlyphSetRow.ForRecent(_recent);
        Sets.Add(_recentRow);
        foreach (var set in sets.UserSets)
            Sets.Add(GlyphSetRow.For(set));

        foreach (var set in BuiltInGlyphSets.All)
            Sets.Add(GlyphSetRow.For(set));

        sets.Changed += OnSetsChanged;
        _recent.Changed += OnRecentChanged;
        _options.Changed += OnOptionsChanged;
        canvas.PropertyChanged += OnCanvasChanged;
        _document.Changed += OnDocumentChanged;

        SelectedSet = _recentRow.Glyphs.Count > 0 ? _recentRow : Find(BuiltInGlyphSets.BasicLatin.Id);
    }

    /// <summary>Raised when what can be done to the chosen set may have changed.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Every set: Recent, then the user's, oldest first, then the built-in ones.</summary>
    public ObservableCollection<GlyphSetRow> Sets { get; } = [];

    /// <summary>
    /// The chosen set, which the grid shows. A dropdown briefly reports no selection while its items change, which
    /// keeps the chosen set.
    /// </summary>
    public GlyphSetRow? SelectedSet
    {
        get => _selectedSet;
        set
        {
            if (value is null || value == _selectedSet)
            {
                OnPropertyChanged();
                return;
            }

            _selectedSet = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(IsRecentSelected));
            OnPropertyChanged(nameof(IsBuiltInSelected));
            FillEditor();
            ShowGlyphs();
        }
    }

    /// <summary>The chosen set's characters that it shows, each a button.</summary>
    public IReadOnlyList<GlyphItem> Glyphs { get; private set; } = [];

    /// <summary>Whether the chosen set is the user's, so the editor can change it.</summary>
    public bool IsEditable => _selectedSet is { Kind: GlyphSetKind.User };

    public bool IsRecentSelected => _selectedSet is { Kind: GlyphSetKind.Recent };

    public bool IsBuiltInSelected => _selectedSet is { Kind: GlyphSetKind.BuiltIn };

    /// <summary>Whether Recent is chosen and has no characters yet.</summary>
    public bool IsRecentEmpty => IsRecentSelected && Glyphs.Count == 0;

    /// <summary>Whether a built-in set is chosen of which no font can draw any character.</summary>
    public bool IsNothingDrawable => IsBuiltInSelected && Glyphs.Count == 0;

    /// <summary>Whether some of the shown characters can't go in the document.</summary>
    public bool HasUnavailable { get; private set; }

    /// <summary>The chosen set's name. Typing a name that can be used renames the set.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (!SetProperty(ref _name, value) || _isFillingEditor)
                return;

            ApplyName();
        }
    }

    /// <summary>Why <see cref="Name"/> isn't the set's name, or null when it is.</summary>
    public string? NameProblem
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNameProblem));
        }
    }

    public bool HasNameProblem => NameProblem is not null;

    /// <summary>
    /// The chosen set's characters. Typing characters that make a set changes the set. Spaces and line breaks only
    /// separate them, and a repeated one counts once.
    /// </summary>
    public string Characters
    {
        get => _characters;
        set
        {
            if (!SetProperty(ref _characters, value) || _isFillingEditor)
                return;

            ApplyCharacters();
        }
    }

    /// <summary>Why <see cref="Characters"/> aren't the set's, or null when they are.</summary>
    public string? CharactersProblem
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasCharactersProblem));
        }
    }

    public bool HasCharactersProblem => CharactersProblem is not null;

    /// <summary>The characters typed to make a new set with <see cref="AddCommand"/>.</summary>
    public string NewCharacters
    {
        get => _newCharacters;
        set
        {
            if (!SetProperty(ref _newCharacters, value))
                return;

            var check = GlyphSet.Check(value);
            NewCharactersProblem = check.Problem == GlyphSetTextProblem.Unusable ? UnusableProblem(check) : null;
            AddCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Why <see cref="NewCharacters"/> can't make a set, or null when they can or there are none yet.
    /// </summary>
    public string? NewCharactersProblem
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNewCharactersProblem));
        }
    }

    public bool HasNewCharactersProblem => NewCharactersProblem is not null;

    /// <summary>Whether <see cref="NewCharacters"/> make a set.</summary>
    public bool CanAdd => GlyphSet.Check(_newCharacters).IsValid;

    /// <summary>Makes a new set of <see cref="NewCharacters"/>, empties the box, and chooses the set.</summary>
    public IRelayCommand AddCommand { get; }

    public bool CanDuplicate => Glyphs.Count > 0;

    public bool CanDelete => IsEditable;

    public bool CanExport => Glyphs.Count > 0;

    /// <summary>
    /// Chooses <paramref name="item"/>'s character for the Character Brush, unless the document can't hold it. A tool
    /// that draws with that character, such as the Line tool, stays chosen and does what it does with a chosen
    /// character (ADR 0079); otherwise the Character Brush is chosen.
    /// </summary>
    public void Choose(GlyphItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsAvailable)
            return;

        _options.Set(CharacterBrushOptions.Character, item.Glyph);
        if (_canvas.SelectedTool.CharacterChosen is { } chosen)
            chosen(_canvas.ToolSettings);
        else
            _canvas.SelectTool(CharacterBrushTool.Id);
    }

    /// <summary>Adds a copy of the characters the chosen set shows, and chooses it.</summary>
    public void DuplicateSelected()
    {
        if (_selectedSet is not { } row || !CanDuplicate)
            return;

        var name = string.Format(CultureInfo.CurrentCulture, Strings.CharacterSetCopyNameFormat, row.Name);
        var added = _sets.Add(name, ShownSet());
        SelectedSet = Find(added.Id);
    }

    /// <summary>Deletes the chosen set once the user confirms, and chooses the one after it.</summary>
    public async Task DeleteSelectedAsync()
    {
        if (_selectedSet is not { Kind: GlyphSetKind.User } row)
            return;

        var message = string.Format(CultureInfo.CurrentCulture, Strings.DeleteCharacterSetMessageFormat, row.Name);
        if (!await _shell.ConfirmAsync(Strings.DeleteCharacterSetTitle, message, Strings.DeleteCharacterSetConfirm))
            return;

        var index = Sets.IndexOf(row);
        _sets.Delete(row.Id);
        SelectedSet = Sets[Math.Min(index, Sets.Count - 1)];
    }

    /// <summary>
    /// Adds the sets in the files the user picks, each named as its file says, with a number after a name that's
    /// taken, and chooses the last. Says which files couldn't be imported, and why.
    /// </summary>
    public async Task ImportAsync()
    {
        var paths = await _dialogs.PickFilesToImportAsync();
        var failures = new List<string>();
        UserGlyphSet? last = null;
        foreach (var path in paths)
        {
            string text;
            try
            {
                text = await File.ReadAllTextAsync(path, Encoding.UTF8);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                failures.Add(Failure(path, Strings.LibraryFileCouldNotRead));
                continue;
            }

            if (GlyphSetFile.TryRead(text, out var set, out var problem))
                last = _sets.Add(set.Name, set.Glyphs);
            else
                failures.Add(Failure(path, ReasonFor(problem)));
        }

        if (last is not null)
            SelectedSet = Find(last.Id);

        if (failures.Count > 0)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.ImportFailedMessageFormat,
                string.Join("\n", failures));
            await _shell.ShowMessageAsync(Strings.ImportCharacterSetsFailedTitle, message);
        }
    }

    /// <summary>Saves the characters the chosen set shows to a character set file the user picks.</summary>
    public async Task ExportSelectedAsync()
    {
        if (_selectedSet is not { } row || !CanExport)
            return;

        var set = ShownSet();
        if (await _dialogs.PickFileToExportAsync(row.Name) is not { } path)
            return;

        try
        {
            await AtomicFile.WriteAsync(path, Encoding.UTF8.GetBytes(GlyphSetFile.Write(row.Name, set)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.ExportCharacterSetFailedMessageFormat,
                Path.GetFileName(path));
            await _shell.ShowMessageAsync(Strings.ExportCharacterSetFailedTitle, message);
        }
    }

    private GlyphSetRow Find(string id) => Sets.First(row => row.Id == id);

    private GlyphSet ShownSet() => new(Glyphs.Select(item => item.Glyph));

    private void AddNewSet()
    {
        if (!CanAdd)
            return;

        var added = _sets.Add(Strings.CharacterSetNewName, GlyphSet.FromText(_newCharacters));
        NewCharacters = "";
        SelectedSet = Find(added.Id);
    }

    // A built-in set shows only what a font can draw, since it's the app's choice of characters, not the user's.
    private void ShowGlyphs()
    {
        IEnumerable<Rune> glyphs = _selectedSet?.Glyphs ?? [];
        if (IsBuiltInSelected)
            glyphs = glyphs.Where(_canDraw);

        Glyphs = [.. glyphs.Select(glyph => new GlyphItem(glyph, Choose))];
        OnPropertyChanged(nameof(Glyphs));
        OnPropertyChanged(nameof(IsRecentEmpty));
        OnPropertyChanged(nameof(IsNothingDrawable));
        RefreshGlyphs();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshGlyphs()
    {
        var chosen = _options.Get(CharacterBrushOptions.Character);
        var characterSet = _document.CharacterSet;
        var hasUnavailable = false;
        foreach (var item in Glyphs)
        {
            item.IsAvailable = characterSet.Allows(item.Glyph);
            item.IsChosen = item.Glyph == chosen;
            hasUnavailable |= !item.IsAvailable;
        }

        if (hasUnavailable == HasUnavailable)
            return;

        HasUnavailable = hasUnavailable;
        OnPropertyChanged(nameof(HasUnavailable));
    }

    private void FillEditor()
    {
        _isFillingEditor = true;
        try
        {
            Name = _selectedSet?.Name ?? "";
            Characters = _selectedSet?.Characters ?? "";
            NameProblem = null;
            CharactersProblem = null;
        }
        finally
        {
            _isFillingEditor = false;
        }
    }

    private void ApplyName()
    {
        if (_selectedSet is not { Kind: GlyphSetKind.User } row)
            return;

        NameProblem = _sets.CheckName(_name, row.Id) switch
        {
            LibraryNameProblem.Blank => Strings.CharacterSetNameBlank,
            LibraryNameProblem.Taken => Strings.CharacterSetNameTaken,
            _ => null
        };

        if (NameProblem is null)
            Apply(() => _sets.Rename(row.Id, _name));
    }

    private void ApplyCharacters()
    {
        if (_selectedSet is not { Kind: GlyphSetKind.User } row)
            return;

        var check = GlyphSet.Check(_characters);
        CharactersProblem = check.Problem switch
        {
            GlyphSetTextProblem.None => null,
            GlyphSetTextProblem.Empty => Strings.CharacterSetCharactersEmpty,
            _ => UnusableProblem(check)
        };

        if (CharactersProblem is null)
            Apply(() => _sets.Change(row.Id, GlyphSet.FromText(_characters)));
    }

    private void Apply(Action edit)
    {
        _isApplyingEdit = true;
        try
        {
            edit();
        }
        finally
        {
            _isApplyingEdit = false;
        }
    }

    private static string UnusableProblem(GlyphSetTextCheck check) =>
        string.Format(CultureInfo.CurrentCulture, Strings.CharacterSetCharactersUnusableFormat,
            check.Glyph.Value.ToString("X4", CultureInfo.InvariantCulture));

    private static string ReasonFor(LibraryFileProblem problem) => problem switch
    {
        LibraryFileProblem.NewerVersion => Strings.LibraryFileNewerVersion,
        LibraryFileProblem.Damaged => Strings.LibraryFileDamaged,
        _ => Strings.CharacterSetFileWrongKind
    };

    private static string Failure(string path, string reason) =>
        string.Format(CultureInfo.CurrentCulture, Strings.ImportFailureFormat, Path.GetFileName(path), reason);

    // Rows follow the library: renamed or changed sets update in place, so the dropdown keeps its place while the user
    // types, and added or deleted ones are added after Recent and the other user sets, or taken away.
    private void OnSetsChanged(object? sender, EventArgs e)
    {
        foreach (var row in Sets.Where(row => row.Kind == GlyphSetKind.User).ToList())
        {
            if (_sets.FindUserSet(row.Id) is null)
                Sets.Remove(row);
        }

        for (var index = 0; index < _sets.UserSets.Count; index++)
        {
            var set = _sets.UserSets[index];
            if (Sets.FirstOrDefault(row => row.Id == set.Id) is { } existing)
                existing.Update(set);
            else
                Sets.Insert(1 + index, GlyphSetRow.For(set));
        }

        if (_selectedSet is not { Kind: GlyphSetKind.User } selected || !Sets.Contains(selected))
            return;

        if (!_isApplyingEdit)
            FillEditor();

        ShowGlyphs();
    }

    // Painting reorders Recent, which shows at once if it's the chosen set.
    private void OnRecentChanged(object? sender, EventArgs e)
    {
        _recentRow.Update(_recent);
        if (IsRecentSelected)
            ShowGlyphs();
    }

    // Such as a character picked from the canvas with Alt.
    private void OnOptionsChanged(object? sender, EventArgs e) => RefreshGlyphs();

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.Session))
        {
            _document.Changed -= OnDocumentChanged;
            _document = _canvas.Session.Document;
            _document.Changed += OnDocumentChanged;
            RefreshGlyphs();
        }
        else if (e.PropertyName == nameof(CanvasViewModel.Font) && IsBuiltInSelected)
        {
            ShowGlyphs();
        }
    }

    // Such as the character set, which decides which characters can be chosen.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.SettingsChanged)
            RefreshGlyphs();
    }
}
