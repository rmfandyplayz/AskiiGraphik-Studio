// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Libraries;
using AskiiArt.Core.Ramps;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Ramps;

/// <summary>
/// The Ramps window's content (ADR 0052): every ramp, built-in ones first, and an editor for the chosen one. The
/// user's ramps change as they're edited, as soon as what's typed is valid; until then the window says why it isn't.
/// Built-in ramps can't be changed, only duplicated. It lives as long as the app, so reopening the window shows the
/// same state.
/// </summary>
internal sealed class RampsPanelViewModel : ObservableObject
{
    /// <summary>How many characters the preview's gradient is wide. A placeholder (docs/TEMPORARY-UI.md).</summary>
    public const int PreviewWidth = 48;

    /// <summary>How many rows the preview repeats its gradient on. A placeholder.</summary>
    public const int PreviewRows = 3;

    /// <summary>The most characters Generate picks.</summary>
    public const int MostGeneratedCharacters = 100;

    /// <summary>How many characters Generate picks until the user changes it.</summary>
    public const int DefaultGeneratedCharacters = 10;

    private readonly RampLibrary _ramps;
    private readonly IAppShell _shell;
    private readonly IRampFileDialogs _dialogs;
    private readonly Func<IGlyphDensityProvider> _densities;

    // While the editor is being filled from the chosen ramp, its fields aren't edits.
    private bool _isFillingEditor;

    // While an edit is being applied, the library's change doesn't refill the editor, which already shows it.
    private bool _isApplyingEdit;

    private RampRow? _selectedRow;
    private string _name = "";
    private string _characters = "";
    private RampZeroLevel _zeroLevel;

    /// <summary>Creates the window's content.</summary>
    /// <param name="ramps">The ramps it shows and edits.</param>
    /// <param name="shell">Asks before deleting, and says what went wrong.</param>
    /// <param name="dialogs">The pickers for ramp files.</param>
    /// <param name="densities">How dark glyphs look in the active document's font, now.</param>
    public RampsPanelViewModel(RampLibrary ramps, IAppShell shell, IRampFileDialogs dialogs,
        Func<IGlyphDensityProvider> densities)
    {
        ArgumentNullException.ThrowIfNull(ramps);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(densities);

        _ramps = ramps;
        _shell = shell;
        _dialogs = dialogs;
        _densities = densities;
        GenerateCommand = new RelayCommand(Generate);

        foreach (var ramp in BuiltInRamps.All)
            Rows.Add(RampRow.For(ramp));

        foreach (var ramp in ramps.UserRamps)
            Rows.Add(RampRow.For(ramp));

        ramps.Changed += OnRampsChanged;
        SelectedRow = Rows[0];
    }

    /// <summary>Raised when what can be done to the chosen ramp may have changed.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Every ramp: the built-in ones, then the user's, oldest first.</summary>
    public ObservableCollection<RampRow> Rows { get; } = [];

    /// <summary>
    /// The chosen ramp, which the editor shows. A list briefly reports no selection while its rows change, which
    /// keeps the chosen ramp.
    /// </summary>
    public RampRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (value is null || value == _selectedRow)
            {
                OnPropertyChanged();
                return;
            }

            _selectedRow = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(IsBuiltInSelected));
            FillEditor();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Whether the chosen ramp is the user's, so the editor can change it.</summary>
    public bool IsEditable => _selectedRow is { IsBuiltIn: false };

    /// <summary>Whether the chosen ramp is a built-in one, which the editor only shows.</summary>
    public bool IsBuiltInSelected => _selectedRow is { IsBuiltIn: true };

    /// <summary>The chosen ramp's name. Typing a name that can be used renames the ramp.</summary>
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

    /// <summary>Why <see cref="Name"/> isn't the ramp's name, or null when it is.</summary>
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
    /// The chosen ramp's characters, lightest first. Typing characters that make a ramp changes the ramp.
    /// </summary>
    public string Characters
    {
        get => _characters;
        set
        {
            if (!SetProperty(ref _characters, value) || _isFillingEditor)
                return;

            ApplyRamp();
        }
    }

    /// <summary>Why <see cref="Characters"/> aren't the ramp's, or null when they are.</summary>
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

    /// <summary>Whether the lightest level is an empty cell, which shows the layers below.</summary>
    public bool IsLightestEmpty
    {
        get => _zeroLevel == RampZeroLevel.Empty;
        set
        {
            if (value)
                SetZeroLevel(RampZeroLevel.Empty);
        }
    }

    /// <summary>Whether the lightest level is a space, which covers the layers below.</summary>
    public bool IsLightestSpace
    {
        get => _zeroLevel == RampZeroLevel.Space;
        set
        {
            if (value)
                SetZeroLevel(RampZeroLevel.Space);
        }
    }

    /// <summary>
    /// The chosen ramp painting from no ink on the left to full on the right, on a few rows. Its lightest level shows
    /// as a space, whether it's an empty cell or a space.
    /// </summary>
    public string Preview { get; private set; } = "";

    public bool CanDuplicate => _selectedRow is not null;

    public bool CanDelete => IsEditable;

    public bool CanSortByDarkness => IsEditable;

    public bool CanExport => _selectedRow is not null;

    /// <summary>The characters Generate picks from. Printable ASCII until the user changes it.</summary>
    public string GenerateCandidates
    {
        get;
        set => SetProperty(ref field, value);
    } = RampGenerator.PrintableAscii;

    /// <summary>How many characters Generate picks, from 1 to <see cref="MostGeneratedCharacters"/>.</summary>
    public decimal? GenerateCount
    {
        get;
        set => SetProperty(ref field, value);
    } = DefaultGeneratedCharacters;

    /// <summary><see cref="MostGeneratedCharacters"/>, for the number box.</summary>
    public static decimal MostGeneratedCount => MostGeneratedCharacters;

    /// <summary>Why the last Generate made nothing, or null.</summary>
    public string? GenerateProblem
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasGenerateProblem));
        }
    }

    public bool HasGenerateProblem => GenerateProblem is not null;

    public IRelayCommand GenerateCommand { get; }

    /// <summary>Adds a new ramp with the Standard ramp's characters, and chooses it.</summary>
    public void AddRamp()
    {
        var added = _ramps.Add(Strings.RampNewName, BuiltInRamps.Standard.Ramp);
        Select(added.Id);
    }

    /// <summary>Adds a copy of the chosen ramp, and chooses it.</summary>
    public void DuplicateSelected()
    {
        if (_selectedRow is not { } row)
            return;

        var name = string.Format(CultureInfo.CurrentCulture, Strings.RampCopyNameFormat, row.Name);
        var added = _ramps.Add(name, row.Ramp);
        Select(added.Id);
    }

    /// <summary>
    /// Deletes the chosen ramp once the user confirms, and chooses the one after it. A tool that used it goes back to
    /// the Standard ramp.
    /// </summary>
    public async Task DeleteSelectedAsync()
    {
        if (_selectedRow is not { IsBuiltIn: false } row)
            return;

        var message = string.Format(CultureInfo.CurrentCulture, Strings.DeleteRampMessageFormat, row.Name);
        if (!await _shell.ConfirmAsync(Strings.DeleteRampTitle, message, Strings.DeleteRampConfirm))
            return;

        var index = Rows.IndexOf(row);
        _ramps.Delete(row.Id);
        SelectedRow = Rows[Math.Min(index, Rows.Count - 1)];
    }

    /// <summary>Puts the chosen ramp's characters in order of darkness in the active document's font.</summary>
    public void SortSelectedByDarkness()
    {
        if (_selectedRow is not { IsBuiltIn: false } row)
            return;

        _ramps.Change(row.Id, row.Ramp.SortedByDensity(_densities()));
    }

    /// <summary>
    /// Adds the ramps in the files the user picks, each named as its file says, with a number after a name that's
    /// taken, and chooses the last. Says which files couldn't be imported, and why.
    /// </summary>
    public async Task ImportAsync()
    {
        var paths = await _dialogs.PickFilesToImportAsync();
        var failures = new List<string>();
        UserRamp? last = null;
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

            if (RampFile.TryRead(text, out var ramp, out var problem))
                last = _ramps.Add(ramp.Name, ramp.Ramp);
            else
                failures.Add(Failure(path, ReasonFor(problem)));
        }

        if (last is not null)
            Select(last.Id);

        if (failures.Count > 0)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.ImportFailedMessageFormat,
                string.Join("\n", failures));
            await _shell.ShowMessageAsync(Strings.ImportRampsFailedTitle, message);
        }
    }

    /// <summary>Saves the chosen ramp to a ramp file the user picks.</summary>
    public async Task ExportSelectedAsync()
    {
        if (_selectedRow is not { } row)
            return;

        if (await _dialogs.PickFileToExportAsync(row.Name) is not { } path)
            return;

        try
        {
            await AtomicFile.WriteAsync(path, Encoding.UTF8.GetBytes(RampFile.Write(row.Name, row.Ramp)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.ExportRampFailedMessageFormat,
                Path.GetFileName(path));
            await _shell.ShowMessageAsync(Strings.ExportRampFailedTitle, message);
        }
    }

    // Picks from the candidates by measuring them in the active document's font, and adds the result as a new ramp.
    private void Generate()
    {
        var count = (int)Math.Clamp(GenerateCount ?? DefaultGeneratedCharacters, 1, MostGeneratedCharacters);
        if (RampGenerator.Generate(GenerateCandidates, count, _densities()) is not { } ramp)
        {
            GenerateProblem = Strings.GenerateRampNothingToPick;
            return;
        }

        GenerateProblem = null;
        var added = _ramps.Add(Strings.RampGeneratedName, ramp);
        Select(added.Id);
    }

    private void Select(string id) => SelectedRow = Rows.First(row => row.Id == id);

    private void FillEditor()
    {
        _isFillingEditor = true;
        try
        {
            Name = _selectedRow?.Name ?? "";
            Characters = _selectedRow?.Characters ?? "";
            _zeroLevel = _selectedRow?.Ramp.ZeroLevel ?? RampZeroLevel.Empty;
            OnPropertyChanged(nameof(IsLightestEmpty));
            OnPropertyChanged(nameof(IsLightestSpace));
            NameProblem = null;
            CharactersProblem = null;
            UpdatePreview();
        }
        finally
        {
            _isFillingEditor = false;
        }
    }

    private void ApplyName()
    {
        if (_selectedRow is not { IsBuiltIn: false } row)
            return;

        NameProblem = _ramps.CheckName(_name, row.Id) switch
        {
            LibraryNameProblem.Blank => Strings.RampNameBlank,
            LibraryNameProblem.Taken => Strings.RampNameTaken,
            _ => null
        };

        if (NameProblem is null)
            Apply(() => _ramps.Rename(row.Id, _name));
    }

    private void SetZeroLevel(RampZeroLevel zeroLevel)
    {
        if (_zeroLevel == zeroLevel)
            return;

        _zeroLevel = zeroLevel;
        OnPropertyChanged(nameof(IsLightestEmpty));
        OnPropertyChanged(nameof(IsLightestSpace));
        if (!_isFillingEditor)
            ApplyRamp();
    }

    private void ApplyRamp()
    {
        if (_selectedRow is not { IsBuiltIn: false } row)
            return;

        var check = Ramp.Check(_characters);
        CharactersProblem = DescribeProblem(check);
        if (CharactersProblem is null)
        {
            Apply(() => _ramps.Change(row.Id, Ramp.FromText(_characters, _zeroLevel)));
            UpdatePreview();
        }
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

    private void UpdatePreview()
    {
        Preview = _selectedRow is { } row ? CreatePreview(row.Ramp) : "";
        OnPropertyChanged(nameof(Preview));
    }

    private static string CreatePreview(Ramp ramp)
    {
        var line = new StringBuilder(PreviewWidth);
        for (var column = 0; column < PreviewWidth; column++)
            line.Append(ramp.GlyphAt(column / (double)(PreviewWidth - 1))?.ToString() ?? " ");

        return string.Join("\n", Enumerable.Repeat(line.ToString(), PreviewRows));
    }

    private static string? DescribeProblem(RampTextCheck check) => check.Problem switch
    {
        RampTextProblem.None => null,
        RampTextProblem.Empty => Strings.RampCharactersEmpty,
        RampTextProblem.Space => Strings.RampCharactersSpace,
        RampTextProblem.Repeated =>
            string.Format(CultureInfo.CurrentCulture, Strings.RampCharactersRepeatedFormat, check.Glyph.ToString()),
        _ => string.Format(CultureInfo.CurrentCulture, Strings.RampCharactersUnusableFormat,
            check.Glyph.Value.ToString("X4", CultureInfo.InvariantCulture))
    };

    private static string ReasonFor(LibraryFileProblem problem) => problem switch
    {
        LibraryFileProblem.NewerVersion => Strings.LibraryFileNewerVersion,
        LibraryFileProblem.Damaged => Strings.LibraryFileDamaged,
        _ => Strings.RampFileNotARampFile
    };

    private static string Failure(string path, string reason) =>
        string.Format(CultureInfo.CurrentCulture, Strings.ImportFailureFormat, Path.GetFileName(path), reason);

    // Rows follow the library: renamed or changed ramps update in place, so the list keeps its place while the user
    // types, and added or deleted ones are added or taken away.
    private void OnRampsChanged(object? sender, EventArgs e)
    {
        var userRows = Rows.Where(row => !row.IsBuiltIn).ToList();
        foreach (var row in userRows.Where(row => _ramps.FindUserRamp(row.Id) is null))
            Rows.Remove(row);

        foreach (var ramp in _ramps.UserRamps)
        {
            if (Rows.FirstOrDefault(row => row.Id == ramp.Id) is { } row)
                row.Update(ramp);
            else
                Rows.Add(RampRow.For(ramp));
        }

        if (_selectedRow is not null && !Rows.Contains(_selectedRow))
            return;

        if (!_isApplyingEdit)
            FillEditor();
    }
}
