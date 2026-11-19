// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tests.Tools;

public sealed class RampOptionsTests
{
    [Fact]
    public void TheRampChoices_ShowEachRampsCharacters()
    {
        var standard = RampOptions.Ramp.Choices[0];

        Assert.Equal(BuiltInRamps.All.Select(ramp => ramp.Id), RampOptions.Ramp.Choices.Select(choice => choice.Value));
        Assert.Equal(".:-=+*#%@", standard.Detail);
    }

    [Fact]
    public void TheBrushAndTheSoftEraser_ShareOneRamp()
    {
        var settings = new ToolSettings();
        Assert.Contains(RampOptions.Group, BrushTool.Definition.OptionGroups);
        Assert.Contains(RampOptions.Group, SoftEraserTool.Definition.OptionGroups);

        settings.For(BrushTool.Definition.OptionGroups[1]).Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);

        var softEraserRamp = settings.For(SoftEraserTool.Definition.OptionGroups[1]);
        Assert.Equal(BuiltInRamps.Blocks.Ramp, new RampSource(softEraserRamp, settings.Ramps).Ramp);
    }

    [Fact]
    public void ARamp_FitsADocument_WhoseCharacterSetHoldsItsCharacters()
    {
        var ascii = Document.Create(new CanvasSize(1, 1), "Layer", CharacterSet.Ascii);
        var settings = new ToolSettings();
        var values = settings.For(RampOptions.Group);
        var source = RampOptions.SourceIn(settings);

        Assert.True(source.FitsIn(ascii));
        Assert.Null(RampOptions.WarningFor(settings, ascii));

        values.Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);

        Assert.False(source.FitsIn(ascii));
        Assert.Equal(RampOptions.RampDoesNotFitWarningKey, RampOptions.WarningFor(settings, ascii));
    }

    [Fact]
    public void TheUsersRamps_AreChoices_AfterTheBuiltInOnes()
    {
        var settings = new ToolSettings();
        var values = settings.For(RampOptions.Group);
        var mine = settings.Ramps.Add("Mine", Ramp.FromText("ab"));

        var choices = values.ChoicesOf(RampOptions.Ramp);

        Assert.Equal([.. BuiltInRamps.All.Select(ramp => ramp.Id), mine.Id], choices.Select(choice => choice.Value));
        Assert.Equal("Mine", choices[^1].Name);
        Assert.Equal("ab", choices[^1].Detail);
    }

    [Fact]
    public void ChoosingAUsersRamp_GivesItsCharactersAsTheyAreNow()
    {
        var settings = new ToolSettings();
        var mine = settings.Ramps.Add("Mine", Ramp.FromText("ab"));
        var source = RampOptions.SourceIn(settings);
        source.Values.Set(RampOptions.Ramp, mine.Id);

        settings.Ramps.Change(mine.Id, Ramp.FromText("xyz"));

        Assert.Equal(Ramp.FromText("xyz"), source.Ramp);
    }

    [Fact]
    public void DeletingTheChosenRamp_ChoosesStandard()
    {
        var settings = new ToolSettings();
        var mine = settings.Ramps.Add("Mine", Ramp.FromText("ab"));
        var values = settings.For(RampOptions.Group);
        values.Set(RampOptions.Ramp, mine.Id);

        settings.Ramps.Delete(mine.Id);

        Assert.Equal(BuiltInRamps.Standard.Id, values.Get(RampOptions.Ramp));
    }

    [Fact]
    public void ASavedChoiceOfAUsersRamp_IsRestored_WhenTheRampIsStillThere()
    {
        var ramps = new RampLibrary([], [new UserRamp("user.mine", "Mine", Ramp.FromText("ab"))]);
        var settings = new ToolSettings(ramps);

        settings.Restore("{\"askiiart.ramp\": {\"ramp\": \"user.mine\"}}");
        Assert.Equal("user.mine", settings.For(RampOptions.Group).Get(RampOptions.Ramp));

        var withoutIt = new ToolSettings();
        withoutIt.Restore("{\"askiiart.ramp\": {\"ramp\": \"user.mine\"}}");
        Assert.Equal(BuiltInRamps.Standard.Id, withoutIt.For(RampOptions.Group).Get(RampOptions.Ramp));
    }
}
