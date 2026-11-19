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
/// The ramp actions: what the Ramps window's buttons do to its chosen ramp (ADR 0052), and the Debug menu's density
/// sample.
/// </summary>
internal sealed class RampActions(RampsPanelViewModel ramps, ActiveDocument activeDocument, DocumentFiles files,
    FontCatalog fonts, GlyphDensityCache densities, bool includeDebugActions) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var duplicate = new RelayCommand(ramps.DuplicateSelected, () => ramps.CanDuplicate);
        var delete = new AsyncRelayCommand(ramps.DeleteSelectedAsync, () => ramps.CanDelete);
        var sort = new RelayCommand(ramps.SortSelectedByDarkness, () => ramps.CanSortByDarkness);
        var export = new AsyncRelayCommand(ramps.ExportSelectedAsync, () => ramps.CanExport);

        ramps.StateChanged += (_, _) =>
        {
            duplicate.NotifyCanExecuteChanged();
            delete.NotifyCanExecuteChanged();
            sort.NotifyCanExecuteChanged();
            export.NotifyCanExecuteChanged();
        };

        yield return new AppAction(RampActionIds.New, nameof(Strings.ActionRampNew), new RelayCommand(ramps.AddRamp));
        yield return new AppAction(RampActionIds.Duplicate, nameof(Strings.ActionRampDuplicate), duplicate);
        yield return new AppAction(RampActionIds.Delete, nameof(Strings.ActionRampDelete), delete);
        yield return new AppAction(RampActionIds.SortByDarkness, nameof(Strings.ActionRampSortByDarkness), sort);
        yield return new AppAction(RampActionIds.Import, nameof(Strings.ActionRampImport),
            new AsyncRelayCommand(ramps.ImportAsync));
        yield return new AppAction(RampActionIds.Export, nameof(Strings.ActionRampExport), export);

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
