// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.Globalization;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Layers;

/// <summary>
/// The Layers panel's content, and what its buttons and the Layer menu do: one row per layer, top
/// layer first, with the active layer's row as the current one. It lives as long as the app, so
/// reopening or floating the panel shows the same state, and it follows the active document.
/// </summary>
internal sealed class LayersPanelViewModel : ObservableObject
{
    private readonly ActiveDocument _activeDocument;
    private EditingSession _followedSession;
    private bool _isSyncing;

    public LayersPanelViewModel(ActiveDocument activeDocument)
    {
        ArgumentNullException.ThrowIfNull(activeDocument);

        _activeDocument = activeDocument;
        _followedSession = activeDocument.Session;
        Follow(_followedSession);
        activeDocument.SessionChanged += OnSessionChanged;

        SyncRows();
    }

    /// <summary>Raised when what can be done to the active layer may have changed.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised when a row's name should open for editing. Only a showing panel can do that.</summary>
    public event EventHandler<LayerRow>? RenameRequested;

    public ObservableCollection<LayerRow> Rows { get; } = [];

    /// <summary>
    /// The active layer's row. Setting it makes that row's layer active. A list briefly reports no
    /// selection (-1) while its rows change, which isn't a request for anything.
    /// </summary>
    public int ActiveRow
    {
        get => RowOf(ActiveIndex);
        set
        {
            if (!_isSyncing && value >= 0 && value < Rows.Count)
                Session.SetActiveLayer(Rows[value].LayerId);
        }
    }

    public bool CanDeleteActiveLayer => Layers.Count > 1 && !Session.ActiveLayer.Properties.IsLocked;

    public bool CanMoveActiveLayerUp => ActiveIndex < Layers.Count - 1;

    public bool CanMoveActiveLayerDown => ActiveIndex > 0;

    /// <summary>
    /// Both layers have to be visible, because a hidden layer's cells would suddenly show or vanish,
    /// and unlocked, because a merge changes both.
    /// </summary>
    public bool CanMergeActiveLayerDown =>
        ActiveIndex > 0 &&
        Session.ActiveLayer.Properties is { IsVisible: true, IsLocked: false } &&
        Layers[ActiveIndex - 1].Properties is { IsVisible: true, IsLocked: false };

    private EditingSession Session => _activeDocument.Session;

    private IReadOnlyList<Layer> Layers => Session.Document.ActiveFrame.Layers;

    private int ActiveIndex => Session.Document.ActiveFrame.IndexOf(Session.ActiveLayer.Id);

    /// <summary>Adds an empty layer above the active one, and makes it active.</summary>
    public void AddLayer()
    {
        var command = new AddLayerCommand(ActiveIndex + 1, new LayerProperties(NextLayerName()));
        Session.History.Execute(command);
        Session.SetActiveLayer(command.LayerId);
    }

    /// <summary>Copies the active layer to just above itself, and makes the copy active.</summary>
    public void DuplicateActiveLayer()
    {
        var name = string.Format(CultureInfo.CurrentCulture, Strings.LayerCopyNameFormat, Session.ActiveLayer.Properties.Name);
        var command = new DuplicateLayerCommand(Session.ActiveLayer.Id, name);
        Session.History.Execute(command);
        Session.SetActiveLayer(command.LayerId);
    }

    /// <summary>Deletes the active layer. The one below it takes over, or the one above if it was the bottom.</summary>
    public void DeleteActiveLayer()
    {
        if (!CanDeleteActiveLayer)
            return;

        var deleted = Session.ActiveLayer.Id;
        var index = ActiveIndex;
        Session.SetActiveLayer(Layers[index > 0 ? index - 1 : 1].Id);
        Session.History.Execute(new RemoveLayerCommand(deleted));
    }

    public void MoveActiveLayerUp()
    {
        if (CanMoveActiveLayerUp)
            Session.History.Execute(new MoveLayerCommand(Session.ActiveLayer.Id, ActiveIndex + 1));
    }

    public void MoveActiveLayerDown()
    {
        if (CanMoveActiveLayerDown)
            Session.History.Execute(new MoveLayerCommand(Session.ActiveLayer.Id, ActiveIndex - 1));
    }

    /// <summary>Merges the active layer into the one below, which becomes active.</summary>
    public void MergeActiveLayerDown()
    {
        if (!CanMergeActiveLayerDown)
            return;

        var merged = Session.ActiveLayer.Id;
        var command = MergeLayerDown.Create(Session.Document, merged, CompositingPolicy.Default);
        Session.SetActiveLayer(Layers[ActiveIndex - 1].Id);
        Session.History.Execute(command);
    }

    /// <summary>Asks the panel to open the active layer's name for editing.</summary>
    public void RenameActiveLayer() => RenameRequested?.Invoke(this, Rows[ActiveRow]);

    /// <summary>Closes a row's name box, keeping the new name if asked to. An empty name changes nothing.</summary>
    public void FinishRename(LayerRow row, bool keepNewName)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.IsRenaming)
            return;

        row.IsRenaming = false;
        var name = row.NameBeingEdited.Trim();
        if (keepNewName && name.Length > 0 && name != row.Name)
            ChangeProperties(row, properties => properties with { Name = name }, UndoStepNames.RenameLayer);
    }

    public void SetLayerShown(LayerRow row, bool isShown)
    {
        ChangeProperties(row, properties => properties with { IsVisible = isShown }, UndoStepNames.ShowOrHideLayer);
    }

    public void SetLayerLocked(LayerRow row, bool isLocked)
    {
        ChangeProperties(row, properties => properties with { IsLocked = isLocked }, UndoStepNames.LockOrUnlockLayer);
    }

    private void ChangeProperties(LayerRow row, Func<LayerProperties, LayerProperties> change, string stepNameKey)
    {
        var properties = Session.Document.GetLayer(row.LayerId).Properties;
        var changed = change(properties);
        if (changed != properties)
            Session.History.Execute(new SetLayerPropertiesCommand(row.LayerId, changed, stepNameKey));
    }

    private string NextLayerName()
    {
        for (var number = Layers.Count + 1; ; number++)
        {
            var name = string.Format(CultureInfo.CurrentCulture, Strings.LayerNameFormat, number);
            if (Layers.All(layer => layer.Properties.Name != name))
                return name;
        }
    }

    private int RowOf(int layerIndex) => Layers.Count - 1 - layerIndex;

    private void Follow(EditingSession session)
    {
        session.Document.Changed += OnDocumentChanged;
        session.ActiveLayerChanged += OnActiveLayerChanged;
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _followedSession.Document.Changed -= OnDocumentChanged;
        _followedSession.ActiveLayerChanged -= OnActiveLayerChanged;
        _followedSession = Session;
        Follow(_followedSession);
        SyncRows();
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.LayerListChanged || e.LayersWithChangedProperties.Count > 0)
            SyncRows();
    }

    private void OnActiveLayerChanged(object? sender, EventArgs e) => ReportState();

    // Rows are reused rather than replaced, so a list keeps its scroll position and a row being renamed
    // survives an unrelated change.
    private void SyncRows()
    {
        _isSyncing = true;
        try
        {
            for (var row = 0; row < Layers.Count; row++)
            {
                var layer = Layers[RowOf(row)];
                var existing = IndexOfRow(layer.Id, row);

                if (existing < 0)
                    Rows.Insert(row, new LayerRow(layer.Id, this));
                else if (existing != row)
                    Rows.Move(existing, row);

                Rows[row].Update(layer.Properties);
            }

            while (Rows.Count > Layers.Count)
                Rows.RemoveAt(Rows.Count - 1);
        }
        finally
        {
            _isSyncing = false;
        }

        ReportState();
    }

    private int IndexOfRow(Guid layerId, int startingAt)
    {
        for (var row = startingAt; row < Rows.Count; row++)
        {
            if (Rows[row].LayerId == layerId)
                return row;
        }

        return -1;
    }

    private void ReportState()
    {
        OnPropertyChanged(nameof(ActiveRow));
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
