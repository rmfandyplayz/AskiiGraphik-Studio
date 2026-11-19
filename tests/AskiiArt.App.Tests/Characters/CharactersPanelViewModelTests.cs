// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Characters;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;

namespace AskiiArt.App.Tests.Characters;

/// <summary>
/// The Characters window's content (ADR 0053): the sets, choosing a character for the Character Brush, Recent, and
/// making, editing and sharing the user's sets.
/// </summary>
public sealed class CharactersPanelViewModelTests : IDisposable
{
    private const char BlockCharacter = '█';

    private readonly FakeAppShell _shell = new();
    private readonly FakeGlyphSetFileDialogs _dialogs = new();
    private readonly TestCanvas _canvas = new();

    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "AskiiArtCharacterTests", Guid.NewGuid().ToString("N"));

    private CharactersPanelViewModel _viewModel;

    public CharactersPanelViewModelTests()
    {
        _viewModel = _canvas.CreateCharactersPanelViewModel(_shell, _dialogs);
        Directory.CreateDirectory(_folder);
    }

    private GlyphSetLibrary Library => _canvas.GlyphSets;

    private RecentGlyphs Recent => _canvas.ToolSettings.RecentGlyphs;

    private Rune Character =>
        _canvas.ToolSettings.For(CharacterBrushOptions.Group).Get(CharacterBrushOptions.Character);

    private IEnumerable<string> GlyphTexts => _viewModel.Glyphs.Select(item => item.Text);

    public void Dispose()
    {
        _canvas.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void ItStarts_WithRecentThenTheBuiltInSets_ShowingBasicLatin_WhileRecentIsEmpty()
    {
        Assert.Equal([GlyphSetRow.RecentId, .. BuiltInGlyphSets.All.Select(set => set.Id)],
            _viewModel.Sets.Select(row => row.Id));
        Assert.Equal(Strings.CharacterSetRecent, _viewModel.Sets[0].Name);
        Assert.Equal(BuiltInGlyphSets.BasicLatin.Id, _viewModel.SelectedSet!.Id);
        Assert.Equal(94, _viewModel.Glyphs.Count);
        Assert.True(_viewModel.IsBuiltInSelected);
        Assert.False(_viewModel.IsEditable);
        Assert.Equal((true, false, true), (_viewModel.CanDuplicate, _viewModel.CanDelete, _viewModel.CanExport));
    }

    [Fact]
    public void ItStarts_ShowingRecent_WhenItHasCharacters()
    {
        Recent.Use(new Rune('x'));
        Recent.Use(new Rune('y'));

        _viewModel = _canvas.CreateCharactersPanelViewModel(_shell, _dialogs);

        Assert.Equal(GlyphSetRow.RecentId, _viewModel.SelectedSet!.Id);
        Assert.Equal(["y", "x"], GlyphTexts);
        Assert.True(_viewModel.IsRecentSelected);
        Assert.False(_viewModel.IsRecentEmpty);
    }

    [Fact]
    public void TheUsersSets_ComeBetweenRecentAndTheBuiltInSets_OldestFirst()
    {
        Library.Add("First", GlyphSet.FromText("ab"));
        Library.Add("Second", GlyphSet.FromText("cd"));

        Assert.Equal(["Recent", "First", "Second", "Basic Latin"], _viewModel.Sets.Take(4).Select(row => row.Name));
        Assert.Equal(GlyphSetKind.User, _viewModel.Sets[1].Kind);
    }

    [Fact]
    public void ClickingACharacter_ChoosesItForTheCharacterBrush_AndSwitchesToThatBrush()
    {
        var item = _viewModel.Glyphs.Single(glyph => glyph.Text == "A");

        item.ChooseCommand.Execute(null);

        Assert.Equal(new Rune('A'), Character);
        Assert.Equal(CharacterBrushTool.Id, _canvas.ViewModel.SelectedTool.Descriptor.Id);
        Assert.Equal(["A"], _viewModel.Glyphs.Where(glyph => glyph.IsChosen).Select(glyph => glyph.Text));
    }

    [Fact]
    public void TheChosenCharacter_IsMarked_HoweverItWasChosen()
    {
        Assert.Equal(["#"], _viewModel.Glyphs.Where(glyph => glyph.IsChosen).Select(glyph => glyph.Text));

        _canvas.ToolSettings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, new Rune('@'));

        Assert.Equal(["@"], _viewModel.Glyphs.Where(glyph => glyph.IsChosen).Select(glyph => glyph.Text));
    }

    [Fact]
    public void InAnAsciiDocument_CharactersItCantHold_AreGreyedOut_AndCantBeChosen()
    {
        Library.Add("Mixed", GlyphSet.FromText("a" + BlockCharacter));
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Name == "Mixed");
        Assert.False(_viewModel.HasUnavailable);

        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));

        Assert.Equal([true, false], _viewModel.Glyphs.Select(glyph => glyph.IsAvailable));
        Assert.True(_viewModel.HasUnavailable);
        _viewModel.Glyphs[1].ChooseCommand.Execute(null);
        Assert.Equal(new Rune('#'), Character);
        Assert.NotEqual(CharacterBrushTool.Id, _canvas.ViewModel.SelectedTool.Descriptor.Id);
    }

    [Fact]
    public void ANewDocument_IsFollowed()
    {
        Library.Add("Mixed", GlyphSet.FromText("a" + BlockCharacter));
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Name == "Mixed");

        _canvas.ActiveDocument.Replace(new Core.Editing.EditingSession(
            Document.Create(new CanvasSize(4, 2), "Layer", CharacterSet.Ascii)));

        Assert.True(_viewModel.HasUnavailable);
    }

    [Fact]
    public void BuiltInSets_HideTheCharactersNoFontCanDraw_ButTheUsersSetsDont()
    {
        _viewModel = _canvas.CreateCharactersPanelViewModel(_shell, _dialogs, canDraw: glyph => glyph.Value <= 'A');
        Library.Add("Mine", GlyphSet.FromText("XYZ"));

        Assert.Equal('A' - '!' + 1, _viewModel.Glyphs.Count);
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Name == "Mine");
        Assert.Equal(["X", "Y", "Z"], GlyphTexts);
    }

    [Fact]
    public void ABuiltInSetNoFontCanDraw_SaysSo_AndCantBeCopied()
    {
        _viewModel = _canvas.CreateCharactersPanelViewModel(_shell, _dialogs, canDraw: _ => false);

        Assert.Empty(_viewModel.Glyphs);
        Assert.True(_viewModel.IsNothingDrawable);
        Assert.False(_viewModel.CanDuplicate);
        Assert.False(_viewModel.CanExport);
    }

    [Fact]
    public void Add_MakesANewSetOfTheTypedCharacters_ChoosesIt_AndEmptiesTheBox()
    {
        _viewModel.NewCharacters = "a b\nc a";
        Assert.True(_viewModel.AddCommand.CanExecute(null));

        _viewModel.AddCommand.Execute(null);

        var added = Assert.Single(Library.UserSets);
        Assert.Equal((Strings.CharacterSetNewName, "abc"), (added.Name, added.Glyphs.ToString()));
        Assert.Equal(added.Id, _viewModel.SelectedSet!.Id);
        Assert.Same(_viewModel.Sets[1], _viewModel.SelectedSet);
        Assert.Equal("", _viewModel.NewCharacters);
        Assert.True(_viewModel.IsEditable);
        Assert.Equal((Strings.CharacterSetNewName, "abc"), (_viewModel.Name, _viewModel.Characters));

        _viewModel.NewCharacters = "x";
        _viewModel.AddCommand.Execute(null);
        Assert.Equal(Strings.CharacterSetNewName + " 2", Library.UserSets[1].Name);
    }

    [Fact]
    public void Add_CantRun_WithNoCharacters_OrOneThatCantBePainted_AndSaysWhy()
    {
        _viewModel.NewCharacters = "  ";
        Assert.False(_viewModel.AddCommand.CanExecute(null));
        Assert.False(_viewModel.HasNewCharactersProblem);

        _viewModel.NewCharacters = "a\u0007";
        Assert.False(_viewModel.AddCommand.CanExecute(null));
        Assert.Contains("U+0007", _viewModel.NewCharactersProblem, StringComparison.Ordinal);

        _viewModel.NewCharacters = "a";
        Assert.True(_viewModel.AddCommand.CanExecute(null));
        Assert.False(_viewModel.HasNewCharactersProblem);
    }

    [Fact]
    public void EditingAUsersSet_ChangesItLive_AndSaysWhyItCant()
    {
        var set = Library.Add("Mine", GlyphSet.FromText("ab"));
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Id == set.Id);

        _viewModel.Name = "Pointers";
        _viewModel.Characters = "x y";
        Assert.Equal(new UserGlyphSet(set.Id, "Pointers", GlyphSet.FromText("xy")), Library.FindUserSet(set.Id));
        Assert.Equal(["x", "y"], GlyphTexts);
        Assert.Equal("x y", _viewModel.Characters);

        _viewModel.Name = "Recent";
        Assert.Equal(Strings.CharacterSetNameTaken, _viewModel.NameProblem);
        _viewModel.Name = " ";
        Assert.Equal(Strings.CharacterSetNameBlank, _viewModel.NameProblem);
        _viewModel.Characters = " ";
        Assert.Equal(Strings.CharacterSetCharactersEmpty, _viewModel.CharactersProblem);
        _viewModel.Characters = "x\u0007";
        Assert.Contains("U+0007", _viewModel.CharactersProblem, StringComparison.Ordinal);

        Assert.Equal(new UserGlyphSet(set.Id, "Pointers", GlyphSet.FromText("xy")), Library.FindUserSet(set.Id));
    }

    [Fact]
    public void BuiltInSetsAndRecent_CantBeEdited()
    {
        _viewModel.Name = "Mine";
        _viewModel.Characters = "ab";
        _viewModel.SelectedSet = _viewModel.Sets[0];
        _viewModel.Characters = "cd";

        Assert.Empty(Library.UserSets);
        Assert.False(_viewModel.CanDelete);
    }

    [Fact]
    public void Duplicate_CopiesWhatTheSetShows_AndChoosesTheCopy()
    {
        _viewModel = _canvas.CreateCharactersPanelViewModel(_shell, _dialogs, canDraw: glyph => glyph.Value <= 'A');

        _viewModel.DuplicateSelected();

        var copy = Assert.Single(Library.UserSets);
        Assert.Equal("Basic Latin copy", copy.Name);
        Assert.Equal('A' - '!' + 1, copy.Glyphs.Glyphs.Count);
        Assert.Equal(copy.Id, _viewModel.SelectedSet!.Id);
    }

    [Fact]
    public void Duplicate_KeepsRecentsCharacters_AsASet()
    {
        Recent.Use(new Rune('x'));
        _viewModel.SelectedSet = _viewModel.Sets[0];

        _viewModel.DuplicateSelected();

        Assert.Equal(("Recent copy", "x"), (Library.UserSets[0].Name, Library.UserSets[0].Glyphs.ToString()));
    }

    [Fact]
    public async Task Delete_AsksFirst_AndChoosesTheSetThatTookItsPlace()
    {
        Library.Add("First", GlyphSet.FromText("ab"));
        Library.Add("Second", GlyphSet.FromText("cd"));
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Name == "First");

        _shell.ConfirmationAnswer = false;
        await _viewModel.DeleteSelectedAsync();
        Assert.Contains("First", Assert.Single(_shell.ConfirmationMessages), StringComparison.Ordinal);
        Assert.Equal(2, Library.UserSets.Count);

        _shell.ConfirmationAnswer = true;
        await _viewModel.DeleteSelectedAsync();
        Assert.Equal(["Second"], Library.UserSets.Select(set => set.Name));
        Assert.Equal("Second", _viewModel.SelectedSet!.Name);
        Assert.Equal("cd", _viewModel.Characters);
    }

    [Fact]
    public void PaintingWithTheCharacterBrush_PutsItsCharacterFirstInRecent_WhileRecentIsShown()
    {
        _viewModel.SelectedSet = _viewModel.Sets[0];
        Assert.True(_viewModel.IsRecentEmpty);

        _canvas.ViewModel.SelectTool(CharacterBrushTool.Id);
        var input = new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None);
        _canvas.ViewModel.ActiveTool.PointerPressed(input);
        _canvas.ViewModel.ActiveTool.PointerReleased(input);

        Assert.Equal(["#"], GlyphTexts);
        Assert.False(_viewModel.IsRecentEmpty);
    }

    [Fact]
    public async Task Export_WritesWhatTheSetShows_ToTheFileTheUserPicks()
    {
        Library.Add("Mine", GlyphSet.FromText("ab"));
        _viewModel.SelectedSet = _viewModel.Sets.Single(row => row.Name == "Mine");
        _dialogs.FileToExport = Path.Combine(_folder, "mine" + GlyphSetFile.Extension);

        await _viewModel.ExportSelectedAsync();

        Assert.Equal(["Mine"], _dialogs.ExportSuggestedNames);
        var text = await File.ReadAllTextAsync(_dialogs.FileToExport, TestContext.Current.CancellationToken);
        Assert.True(GlyphSetFile.TryRead(text, out var named, out _));
        Assert.Equal(new NamedGlyphSet("Mine", GlyphSet.FromText("ab")), named);
    }

    [Fact]
    public async Task Import_AddsEachFilesSet_ChoosesTheLast_AndSaysWhichCouldntBeImported()
    {
        var notASet = Path.Combine(_folder, "notes.txt");
        await File.WriteAllTextAsync(notASet, "{}", TestContext.Current.CancellationToken);
        _dialogs.FilesToImport.Add(await WriteSetFileAsync("one", "Recent", "ab"));
        _dialogs.FilesToImport.Add(notASet);
        _dialogs.FilesToImport.Add(Path.Combine(_folder, "missing" + GlyphSetFile.Extension));
        _dialogs.FilesToImport.Add(await WriteSetFileAsync("two", "Mine", "cd"));

        await _viewModel.ImportAsync();

        Assert.Equal(["Recent 2", "Mine"], Library.UserSets.Select(set => set.Name));
        Assert.Equal("Mine", _viewModel.SelectedSet!.Name);
        var (title, message) = Assert.Single(_shell.Messages);
        Assert.Equal(Strings.ImportCharacterSetsFailedTitle, title);
        Assert.Contains(Strings.CharacterSetFileWrongKind, message, StringComparison.Ordinal);
        Assert.Contains(Strings.LibraryFileCouldNotRead, message, StringComparison.Ordinal);
    }

    private async Task<string> WriteSetFileAsync(string fileName, string name, string characters)
    {
        var path = Path.Combine(_folder, fileName + GlyphSetFile.Extension);
        await File.WriteAllTextAsync(path, GlyphSetFile.Write(name, GlyphSet.FromText(characters)),
            TestContext.Current.CancellationToken);
        return path;
    }
}
