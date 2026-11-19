// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Ramps;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.App.Tests.Ramps;

/// <summary>
/// The Ramps window's content (ADR 0052): the list of ramps, editing the user's ones live, and the buttons.
/// </summary>
public sealed class RampsPanelViewModelTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeRampFileDialogs _dialogs = new();
    private readonly TestCanvas _canvas = new();
    private readonly RampsPanelViewModel _viewModel;
    private readonly string _folder =
        Path.Combine(Path.GetTempPath(), "AskiiArtRampTests", Guid.NewGuid().ToString("N"));

    public RampsPanelViewModelTests()
    {
        _viewModel = _canvas.CreateRampsPanelViewModel(_shell, _dialogs);
        Directory.CreateDirectory(_folder);
    }

    private RampLibrary Library => _canvas.ToolSettings.Ramps;

    private UserRamp SelectedUserRamp => Library.FindUserRamp(_viewModel.SelectedRow!.Id)!;

    public void Dispose()
    {
        _canvas.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void ItStarts_WithTheBuiltInRamps_TheFirstChosen_AndOnlyShown()
    {
        Assert.Equal(BuiltInRamps.All.Select(ramp => ramp.Id), _viewModel.Rows.Select(row => row.Id));
        Assert.Same(_viewModel.Rows[0], _viewModel.SelectedRow);
        Assert.True(_viewModel.IsBuiltInSelected);
        Assert.False(_viewModel.IsEditable);
        Assert.Equal((true, false, false, true),
            (_viewModel.CanDuplicate, _viewModel.CanDelete, _viewModel.CanSortByDarkness, _viewModel.CanExport));
        Assert.Equal(Strings.RampStandard, _viewModel.Name);
        Assert.Equal(BuiltInRamps.Standard.Ramp.ToString(), _viewModel.Characters);
    }

    [Fact]
    public void ItListsTheUserRampsAfterTheBuiltInOnes_OldestFirst()
    {
        Library.Add("First", Ramp.FromText("ab"));
        Library.Add("Second", Ramp.FromText("cd"));

        Assert.Equal(["First", "Second"], _viewModel.Rows.Skip(BuiltInRamps.All.Count).Select(row => row.Name));
    }

    [Fact]
    public void EditingABuiltInRamp_ChangesNothing()
    {
        _viewModel.Name = "Mine";
        _viewModel.Characters = "xyz";
        _viewModel.IsLightestSpace = true;

        Assert.Empty(Library.UserRamps);
        Assert.Equal(Strings.RampStandard, _viewModel.Rows[0].Name);
        Assert.Equal(BuiltInRamps.Standard.Ramp, _viewModel.Rows[0].Ramp);
    }

    [Fact]
    public void New_AddsARampWithTheStandardCharacters_AndChoosesIt()
    {
        _viewModel.AddRamp();
        _viewModel.AddRamp();

        Assert.Equal([Strings.RampNewName, Strings.RampNewName + " 2"], Library.UserRamps.Select(ramp => ramp.Name));
        Assert.Equal(Library.UserRamps[1].Id, _viewModel.SelectedRow!.Id);
        Assert.Equal(BuiltInRamps.Standard.Ramp, SelectedUserRamp.Ramp);
        Assert.True(_viewModel.IsEditable);
        Assert.Equal((true, true, true, true),
            (_viewModel.CanDuplicate, _viewModel.CanDelete, _viewModel.CanSortByDarkness, _viewModel.CanExport));
    }

    [Fact]
    public void Duplicate_MakesAnEditableCopy_AndChoosesIt()
    {
        _viewModel.SelectedRow = _viewModel.Rows.Single(row => row.Id == BuiltInRamps.Blocks.Id);

        _viewModel.DuplicateSelected();

        var copy = Assert.Single(Library.UserRamps);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.RampCopyNameFormat, Strings.RampBlocks),
            copy.Name);
        Assert.Equal(BuiltInRamps.Blocks.Ramp, copy.Ramp);
        Assert.Equal(copy.Id, _viewModel.SelectedRow!.Id);
    }

    [Fact]
    public void TypingAName_RenamesTheRamp_KeepingItsRow()
    {
        _viewModel.AddRamp();
        var row = _viewModel.SelectedRow!;

        _viewModel.Name = "Mine";

        Assert.Equal("Mine", SelectedUserRamp.Name);
        Assert.Same(row, _viewModel.SelectedRow);
        Assert.Equal("Mine", row.Name);
        Assert.False(_viewModel.HasNameProblem);
    }

    [Theory]
    [InlineData("   ", nameof(Strings.RampNameBlank))]
    [InlineData("standard", nameof(Strings.RampNameTaken))]
    [InlineData("Other", nameof(Strings.RampNameTaken))]
    public void ANameThatCantBeUsed_SaysWhy_AndKeepsTheOldName(string name, string problemKey)
    {
        Library.Add("Other", Ramp.FromText("ab"));
        _viewModel.AddRamp();

        _viewModel.Name = name;

        Assert.Equal(Strings.RampNewName, SelectedUserRamp.Name);
        Assert.Equal(UiText.Get(problemKey), _viewModel.NameProblem);

        // Fixing it applies it and clears the problem.
        _viewModel.Name = "Mine";
        Assert.Equal("Mine", SelectedUserRamp.Name);
        Assert.Null(_viewModel.NameProblem);
    }

    [Fact]
    public void TypingCharacters_ChangesTheRamp_AndItsPreview()
    {
        _viewModel.AddRamp();

        _viewModel.Characters = "ab";

        Assert.Equal(Ramp.FromText("ab"), SelectedUserRamp.Ramp);
        Assert.Equal("ab", _viewModel.SelectedRow!.Characters);
        var rows = _viewModel.Preview.Split('\n');
        Assert.Equal(RampsPanelViewModel.PreviewRows, rows.Length);
        Assert.All(rows, row => Assert.Equal(RampsPanelViewModel.PreviewWidth, row.Length));
        Assert.StartsWith(" ", rows[0], StringComparison.Ordinal);
        Assert.EndsWith("b", rows[0], StringComparison.Ordinal);
        Assert.Equal(" ab", string.Concat(rows[0].Distinct().Order()));
    }

    public static TheoryData<string, string> CharactersThatCantBeARamp => new()
    {
        { "", Strings.RampCharactersEmpty },
        { "a b", Strings.RampCharactersSpace },
        { "abca", string.Format(CultureInfo.CurrentCulture, Strings.RampCharactersRepeatedFormat, "a") },
        { "a\tb", string.Format(CultureInfo.CurrentCulture, Strings.RampCharactersUnusableFormat, "0009") },
    };

    [Theory]
    [MemberData(nameof(CharactersThatCantBeARamp))]
    public void CharactersThatCantBeARamp_SayWhy_AndDontChangeIt(string characters, string problem)
    {
        _viewModel.AddRamp();

        _viewModel.Characters = characters;

        Assert.Equal(BuiltInRamps.Standard.Ramp, SelectedUserRamp.Ramp);
        Assert.Equal(problem, _viewModel.CharactersProblem);
        Assert.Equal(characters, _viewModel.Characters);
    }

    [Fact]
    public void ChoosingTheLightestLevel_ChangesTheRamp()
    {
        _viewModel.AddRamp();

        _viewModel.IsLightestSpace = true;

        Assert.Equal(RampZeroLevel.Space, SelectedUserRamp.Ramp.ZeroLevel);
        Assert.False(_viewModel.IsLightestEmpty);

        _viewModel.IsLightestEmpty = true;
        Assert.Equal(RampZeroLevel.Empty, SelectedUserRamp.Ramp.ZeroLevel);
    }

    [Fact]
    public void ChoosingAnotherRamp_ShowsItInTheEditor_AndForgetsAProblem()
    {
        _viewModel.AddRamp();
        _viewModel.Characters = "aa";
        Assert.True(_viewModel.HasCharactersProblem);

        _viewModel.SelectedRow = _viewModel.Rows.Single(row => row.Id == BuiltInRamps.Fine.Id);

        Assert.Equal(BuiltInRamps.Fine.Ramp.ToString(), _viewModel.Characters);
        Assert.False(_viewModel.HasCharactersProblem);
        Assert.True(_viewModel.IsBuiltInSelected);
    }

    [Fact]
    public async Task Delete_AsksFirst_AndKeepsTheRampIfTheUserSaysNo()
    {
        _viewModel.AddRamp();
        _shell.ConfirmationAnswer = false;

        await _viewModel.DeleteSelectedAsync();

        var question = Assert.Single(_shell.ConfirmationMessages);
        Assert.Contains(Strings.RampNewName, question, StringComparison.Ordinal);
        Assert.Single(Library.UserRamps);
    }

    [Fact]
    public async Task Delete_WhenConfirmed_RemovesTheRamp_AndChoosesTheOneThatTookItsPlace()
    {
        Library.Add("First", Ramp.FromText("ab"));
        Library.Add("Second", Ramp.FromText("cd"));
        _viewModel.SelectedRow = _viewModel.Rows.Single(row => row.Name == "First");
        _shell.ConfirmationAnswer = true;

        await _viewModel.DeleteSelectedAsync();

        Assert.Equal(["Second"], Library.UserRamps.Select(ramp => ramp.Name));
        Assert.Equal("Second", _viewModel.SelectedRow!.Name);
        Assert.Equal("cd", _viewModel.Characters);

        // Deleting the last one chooses the one before it.
        await _viewModel.DeleteSelectedAsync();
        Assert.Empty(Library.UserRamps);
        Assert.Equal(BuiltInRamps.All[^1].Id, _viewModel.SelectedRow!.Id);
    }

    [Fact]
    public async Task DeletingTheRampATool_Uses_PutsItBackOnStandard()
    {
        _viewModel.AddRamp();
        var rampChoice = _canvas.ToolSettings.For(RampOptions.Group);
        rampChoice.Set(RampOptions.Ramp, _viewModel.SelectedRow!.Id);
        _shell.ConfirmationAnswer = true;

        await _viewModel.DeleteSelectedAsync();

        Assert.Equal(BuiltInRamps.Standard.Id, rampChoice.Get(RampOptions.Ramp));
    }

    [Fact]
    public void SortByDarkness_PutsTheCharactersInOrderInTheDocumentsFont()
    {
        _viewModel.AddRamp();
        _viewModel.Characters = "@.";

        _viewModel.SortSelectedByDarkness();

        Assert.Equal(".@", SelectedUserRamp.Ramp.ToString());
        Assert.Equal(".@", _viewModel.Characters);
    }

    [Fact]
    public void Generate_AddsARampOfTheCountAsked_AndChoosesIt()
    {
        _viewModel.GenerateCount = 5;

        _viewModel.GenerateCommand.Execute(null);

        var generated = Assert.Single(Library.UserRamps);
        Assert.Equal(Strings.RampGeneratedName, generated.Name);
        Assert.Equal(5, generated.Ramp.TopLevel);
        Assert.Equal(generated.Id, _viewModel.SelectedRow!.Id);
        Assert.False(_viewModel.HasGenerateProblem);
    }

    [Fact]
    public void Generate_WithNothingToPickFrom_SaysSo_AndAddsNothing()
    {
        _viewModel.GenerateCandidates = "   ";

        _viewModel.GenerateCommand.Execute(null);

        Assert.Empty(Library.UserRamps);
        Assert.Equal(Strings.GenerateRampNothingToPick, _viewModel.GenerateProblem);
    }

    [Fact]
    public async Task Export_WritesTheChosenRamp_ToTheFileTheUserPicks()
    {
        _viewModel.AddRamp();
        _viewModel.Name = "Mine";
        _viewModel.Characters = "ab";
        var path = Path.Combine(_folder, "mine" + RampFile.Extension);
        _dialogs.FileToExport = path;

        await _viewModel.ExportSelectedAsync();

        Assert.Equal(["Mine"], _dialogs.ExportSuggestedNames);
        var text = await File.ReadAllTextAsync(path, Encoding.UTF8, TestContext.Current.CancellationToken);
        Assert.True(RampFile.TryRead(text, out var read, out _));
        Assert.Equal(new NamedRamp("Mine", Ramp.FromText("ab")), read);
    }

    [Fact]
    public async Task Export_Cancelled_WritesNothing()
    {
        await _viewModel.ExportSelectedAsync();

        Assert.Single(_dialogs.ExportSuggestedNames);
        Assert.Empty(Directory.GetFiles(_folder));
        Assert.Empty(_shell.Messages);
    }

    [Fact]
    public async Task Import_AddsEachFilesRamp_NumberingATakenName_AndChoosesTheLast()
    {
        _dialogs.FilesToImport.Add(WriteRampFile("one", "Standard", Ramp.FromText("ab")));
        _dialogs.FilesToImport.Add(WriteRampFile("two", "Mine", Ramp.FromText("cd", RampZeroLevel.Space)));

        await _viewModel.ImportAsync();

        Assert.Equal(["Standard 2", "Mine"], Library.UserRamps.Select(ramp => ramp.Name));
        Assert.Equal(Ramp.FromText("cd", RampZeroLevel.Space), Library.UserRamps[1].Ramp);
        Assert.Equal("Mine", _viewModel.SelectedRow!.Name);
        Assert.Empty(_shell.Messages);
    }

    [Fact]
    public async Task Import_SaysWhichFilesCouldntBeImported_AndWhy_AndImportsTheRest()
    {
        var notARamp = Path.Combine(_folder, "notes.txt");
        await File.WriteAllTextAsync(notARamp, "{}", TestContext.Current.CancellationToken);
        _dialogs.FilesToImport.Add(notARamp);
        _dialogs.FilesToImport.Add(Path.Combine(_folder, "missing" + RampFile.Extension));
        _dialogs.FilesToImport.Add(WriteRampFile("good", "Good", Ramp.FromText("ab")));

        await _viewModel.ImportAsync();

        Assert.Equal(["Good"], Library.UserRamps.Select(ramp => ramp.Name));
        var (title, message) = Assert.Single(_shell.Messages);
        Assert.Equal(Strings.ImportRampsFailedTitle, title);
        Assert.Contains("notes.txt", message, StringComparison.Ordinal);
        Assert.Contains(Strings.RampFileNotARampFile, message, StringComparison.Ordinal);
        Assert.Contains("missing" + RampFile.Extension, message, StringComparison.Ordinal);
        Assert.Contains(Strings.RampFileCouldNotRead, message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Import_Cancelled_DoesNothing()
    {
        await _viewModel.ImportAsync();

        Assert.Empty(Library.UserRamps);
        Assert.Empty(_shell.Messages);
        Assert.Same(_viewModel.Rows[0], _viewModel.SelectedRow);
    }

    [Fact]
    public void TheButtons_AreOnlyEnabledWhenTheyCanApply()
    {
        var actions = new RampActions(_viewModel, _canvas.ActiveDocument,
                _canvas.CreateDocumentFiles(new FakeDocumentDialogs(), _shell), _canvas.Fonts, _canvas.Densities,
                includeDebugActions: false)
            .CreateActions().ToDictionary(action => action.Id);

        Assert.False(actions[RampActionIds.Delete].Command.CanExecute(null));
        Assert.False(actions[RampActionIds.SortByDarkness].Command.CanExecute(null));
        Assert.True(actions[RampActionIds.Duplicate].Command.CanExecute(null));
        Assert.True(actions[RampActionIds.Export].Command.CanExecute(null));

        actions[RampActionIds.New].Command.Execute(null);

        Assert.True(actions[RampActionIds.Delete].Command.CanExecute(null));
        Assert.True(actions[RampActionIds.SortByDarkness].Command.CanExecute(null));
    }

    private string WriteRampFile(string fileName, string name, Ramp ramp)
    {
        var path = Path.Combine(_folder, fileName + RampFile.Extension);
        File.WriteAllText(path, RampFile.Write(name, ramp), Encoding.UTF8);
        return path;
    }
}
