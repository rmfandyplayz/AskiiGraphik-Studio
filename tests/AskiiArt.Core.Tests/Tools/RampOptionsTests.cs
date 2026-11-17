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
        Assert.Equal(BuiltInRamps.Blocks, RampOptions.ReadRamp(softEraserRamp));
    }

    [Fact]
    public void ARamp_FitsADocument_WhoseCharacterSetHoldsItsCharacters()
    {
        var ascii = Document.Create(new CanvasSize(1, 1), "Layer", CharacterSet.Ascii);
        var settings = new ToolSettings();
        var values = settings.For(RampOptions.Group);

        Assert.True(RampOptions.FitsIn(values, ascii));
        Assert.Null(RampOptions.WarningFor(settings, ascii));

        values.Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);

        Assert.False(RampOptions.FitsIn(values, ascii));
        Assert.Equal(RampOptions.RampDoesNotFitWarningKey, RampOptions.WarningFor(settings, ascii));
    }
}
