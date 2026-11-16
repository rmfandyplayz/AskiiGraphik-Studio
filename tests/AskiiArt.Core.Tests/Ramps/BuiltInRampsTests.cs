// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class BuiltInRampsTests
{
    [Fact]
    public void All_HasTheFiveAgreedRamps_InOrder_WithStandardAsTheDefault()
    {
        Assert.Equal<BuiltInRamp>(
            [BuiltInRamps.Standard, BuiltInRamps.StandardAlphanumerical, BuiltInRamps.Fine, BuiltInRamps.Blocks,
                BuiltInRamps.Braille],
            BuiltInRamps.All);
        Assert.Same(BuiltInRamps.Standard, BuiltInRamps.Default);
        Assert.Equal(".:-=+*#%@", BuiltInRamps.Standard.Ramp.ToString());
    }

    [Fact]
    public void IdsAndNameKeys_AreDistinct()
    {
        Assert.Equal(BuiltInRamps.All.Count, BuiltInRamps.All.Select(ramp => ramp.Id).Distinct().Count());
        Assert.Equal(BuiltInRamps.All.Count, BuiltInRamps.All.Select(ramp => ramp.NameKey).Distinct().Count());
    }

    [Fact]
    public void EveryRamp_PaintsAnEmptyCellAtIntensity0()
    {
        Assert.All(BuiltInRamps.All, ramp => Assert.Equal(RampZeroLevel.Empty, ramp.Ramp.ZeroLevel));
    }

    [Fact]
    public void TheAsciiRamps_FitAsciiOnlyDocuments_AndTheOthersDont()
    {
        Assert.True(BuiltInRamps.Standard.Ramp.FitsIn(CharacterSet.Ascii));
        Assert.True(BuiltInRamps.StandardAlphanumerical.Ramp.FitsIn(CharacterSet.Ascii));
        Assert.True(BuiltInRamps.Fine.Ramp.FitsIn(CharacterSet.Ascii));
        Assert.False(BuiltInRamps.Blocks.Ramp.FitsIn(CharacterSet.Ascii));
        Assert.False(BuiltInRamps.Braille.Ramp.FitsIn(CharacterSet.Ascii));
    }

    [Fact]
    public void StandardAlphanumerical_IsAboutTwentyLettersAndDigits_FinerThanStandard()
    {
        var ramp = BuiltInRamps.StandardAlphanumerical.Ramp;

        Assert.Equal(20, ramp.TopLevel);
        Assert.All(ramp.Glyphs, glyph => Assert.True(char.IsAsciiLetterOrDigit((char)glyph.Value)));
    }

    [Fact]
    public void Fine_IsALongRamp()
    {
        Assert.Equal(69, BuiltInRamps.Fine.Ramp.TopLevel);
    }
}
