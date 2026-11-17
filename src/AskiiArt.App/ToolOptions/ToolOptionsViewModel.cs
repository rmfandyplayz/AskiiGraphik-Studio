// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Canvas;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Options;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.ToolOptions;

/// <summary>
/// What the Tool Options toolbar shows: the chosen tool's options, built from its option groups as data (ADR 0047),
/// and a warning when the tool can't be used with them in this document. Changing an option changes the value that
/// every tool of its group shares. One lives as long as the app does.
/// </summary>
internal sealed class ToolOptionsViewModel : ObservableObject, IDisposable
{
    private readonly CanvasViewModel _canvas;
    private IReadOnlyList<ToolOptionValues> _values = [];
    private Document? _document;

    public ToolOptionsViewModel(CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _canvas = canvas;
        canvas.PropertyChanged += OnCanvasChanged;
        FollowDocument(canvas.Session.Document);
        Update();
    }

    /// <summary>
    /// The chosen tool's options, group by group, each in the order its group lists them. Empty when it has none.
    /// </summary>
    public IReadOnlyList<ToolOptionItem> Items { get; private set; } = [];

    /// <summary>Whether the chosen tool has any options.</summary>
    public bool HasOptions => Items.Count > 0;

    /// <summary>Why the chosen tool can't be used with these options in this document, or null when it can.</summary>
    public string? Warning { get; private set; }

    /// <summary>Whether there's a <see cref="Warning"/>.</summary>
    public bool HasWarning => Warning is not null;

    public void Dispose()
    {
        _canvas.PropertyChanged -= OnCanvasChanged;
        FollowValues([]);
        FollowDocument(null);
    }

    // Choosing another tool with the same option groups keeps the items, and so whatever the toolbar is in the middle
    // of.
    private void Update()
    {
        IReadOnlyList<ToolOptionValues> values =
            [.. _canvas.SelectedTool.OptionGroups.Select(group => _canvas.ToolSettings.For(group))];
        if (!values.SequenceEqual(_values))
        {
            FollowValues(values);
            Items = [.. values.SelectMany(CreateItems)];
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(HasOptions));
        }

        UpdateWarning();
    }

    private static IEnumerable<ToolOptionItem> CreateItems(ToolOptionValues values) =>
        values.Group.Options.Select(option => ToolOptionItem.Create(option, values));

    private void UpdateWarning()
    {
        var warningKey = _canvas.SelectedTool.WarningFor?.Invoke(_canvas.ToolSettings, _canvas.Session.Document);
        var warning = warningKey is null ? null : UiText.Get(warningKey);
        if (warning == Warning)
            return;

        Warning = warning;
        OnPropertyChanged(nameof(Warning));
        OnPropertyChanged(nameof(HasWarning));
    }

    private void FollowValues(IReadOnlyList<ToolOptionValues> values)
    {
        foreach (var followed in _values)
            followed.Changed -= OnValuesChanged;

        _values = values;
        foreach (var followed in _values)
            followed.Changed += OnValuesChanged;
    }

    private void FollowDocument(Document? document)
    {
        if (_document is not null)
            _document.Changed -= OnDocumentChanged;

        _document = document;
        if (_document is not null)
            _document.Changed += OnDocumentChanged;
    }

    private void OnCanvasChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CanvasViewModel.SelectedTool))
        {
            Update();
        }
        else if (e.PropertyName == nameof(CanvasViewModel.Session))
        {
            FollowDocument(_canvas.Session.Document);
            UpdateWarning();
        }
    }

    // A value can change from elsewhere too, such as the [ and ] keys on the canvas.
    private void OnValuesChanged(object? sender, EventArgs e)
    {
        foreach (var item in Items)
            item.Refresh();

        UpdateWarning();
    }

    // Such as the character set, which decides whether a ramp fits.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.SettingsChanged)
            UpdateWarning();
    }
}
