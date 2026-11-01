// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Formats;

namespace AskiiArt.App.Tests.Shell;

public sealed class WindowTitleTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(3, 1));
    private readonly WindowTitle _title;
    private readonly List<string> _changes = [];

    public WindowTitleTests()
    {
        _title = new WindowTitle(_canvas.ActiveDocument);
        _title.PropertyChanged += (_, _) => _changes.Add(_title.Text);
    }

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void NewDocument_IsUntitled_WithTheAppName()
    {
        Assert.Equal(Title(Strings.WindowTitleFormat, Strings.UntitledDocumentName), _title.Text);
    }

    [Fact]
    public void UnsavedChanges_AreMarked_UntilSaved()
    {
        MakeAChange(_canvas.Session);
        Assert.Equal(Title(Strings.WindowTitleUnsavedFormat, Strings.UntitledDocumentName), _title.Text);
        Assert.True(_title.HasUnsavedChanges);

        var file = new DocumentFile(Path.Combine("folder", "art.asciiart"), DocumentFileVariant.Plain);
        _canvas.ActiveDocument.SetFile(file);
        _canvas.Session.History.MarkSaved();

        Assert.Equal(Title(Strings.WindowTitleFormat, "art.asciiart"), _title.Text);
        Assert.False(_title.HasUnsavedChanges);
        Assert.Equal(_title.Text, _changes[^1]);
    }

    [Fact]
    public void AfterTheDocumentIsReplaced_FollowsTheNewOne()
    {
        var oldSession = _canvas.Session;
        var newSession = new EditingSession(Document.Create(new CanvasSize(3, 1), "Layer"));
        _canvas.ActiveDocument.Replace(newSession);
        _changes.Clear();

        MakeAChange(oldSession);
        Assert.Empty(_changes);

        MakeAChange(newSession);
        Assert.Equal(Title(Strings.WindowTitleUnsavedFormat, Strings.UntitledDocumentName), _title.Text);
    }

    private static string Title(string format, string documentName) =>
        string.Format(CultureInfo.CurrentCulture, format, documentName, Strings.AppDisplayName);

    private static void MakeAChange(EditingSession session)
    {
        var edit = new CellEditBuilder(session.Document, session.Document.ActiveFrame.Layers[0].Id);
        edit.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('x')));
        session.History.Execute(edit.Build()!);
    }
}
