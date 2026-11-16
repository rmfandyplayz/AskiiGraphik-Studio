// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Glyphs;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Ramps;

/// <summary>
/// The ramp actions. For now that's only the Debug menu's density sample; the Ramps window joins it in step 3.5.
/// </summary>
internal sealed class RampActions(ActiveDocument activeDocument, DocumentFiles files, FontCatalog fonts,
    GlyphDensityCache densities, bool includeDebugActions) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        if (!includeDebugActions)
            yield break;

        yield return new AppAction(RampActionIds.OpenDensitySample, nameof(Strings.ActionOpenDensitySample),
            new AsyncRelayCommand(OpenDensitySampleAsync));
    }

    // The sample keeps the font of the document it replaces, so choosing a font and then opening the sample shows
    // that font's measurements.
    private Task OpenDensitySampleAsync()
    {
        var font = activeDocument.Session.Document.Font;
        return files.ReplaceWithNewAsync(() =>
        {
            var meter = densities.GetMeter(fonts.Resolve(font));
            return Task.FromResult<Document?>(DensitySampleDocument.Create(Strings.DefaultLayerName, font, meter));
        });
    }
}
