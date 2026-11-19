// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.GlyphSets;

/// <summary>A character set the user made, duplicated or imported, kept in a <see cref="GlyphSetLibrary"/>.</summary>
/// <param name="Id">A stable id. Never shown to the user.</param>
/// <param name="Name">The name the user sees, unique among the library's sets.</param>
/// <param name="Glyphs">Its characters.</param>
public sealed record UserGlyphSet(string Id, string Name, GlyphSet Glyphs) : IUserItem;

/// <summary>A character set read from a file, with the name the file gives it.</summary>
public sealed record NamedGlyphSet(string Name, GlyphSet Glyphs);

/// <summary>
/// The character sets the app knows (ADR 0053): the built-in ones, which can't change, then the user's, which they
/// add, rename, change and delete, newest last. Names are unique, ignoring case and the spaces around them, as ramps'
/// are (ADR 0052). Used on the UI thread only.
/// </summary>
public sealed class GlyphSetLibrary
{
    /// <summary>What every user set's id starts with, so it can't be mistaken for a built-in one.</summary>
    public const string UserSetIdPrefix = "user.";

    private readonly UserItemList<UserGlyphSet> _userSets;

    /// <summary>Creates a library with no user sets, whose built-in sets have no names to keep apart from.</summary>
    public GlyphSetLibrary()
        : this([], [])
    {
    }

    /// <summary>
    /// Creates a library holding <paramref name="userSets"/>, such as the ones saved last time. One whose id is taken
    /// gets a new one, and one whose name is taken gets a number after it.
    /// </summary>
    /// <param name="reservedNames">
    /// The names user sets can't take: the built-in sets' as the user sees them, and the Characters window's own,
    /// such as Recent.
    /// </param>
    /// <param name="userSets">The user's sets, oldest first.</param>
    /// <exception cref="ArgumentException">A user set's name is blank.</exception>
    public GlyphSetLibrary(IEnumerable<string> reservedNames, IEnumerable<UserGlyphSet> userSets)
    {
        ArgumentNullException.ThrowIfNull(userSets);

        _userSets = new UserItemList<UserGlyphSet>(UserSetIdPrefix, reservedNames);
        foreach (var set in userSets)
            _userSets.Add(new UserGlyphSet(_userSets.IdFor(set.Id), _userSets.UniqueName(set.Name), set.Glyphs));
    }

    /// <summary>Raised after a user set is added, renamed, changed or deleted.</summary>
    public event EventHandler? Changed;

    /// <summary>The user's sets, oldest first.</summary>
    public IReadOnlyList<UserGlyphSet> UserSets => _userSets.Items;

    /// <summary>The set with <paramref name="id"/>, built-in or the user's, or null when there's none.</summary>
    public GlyphSet? Find(string id) => BuiltInGlyphSets.Find(id)?.Glyphs ?? FindUserSet(id)?.Glyphs;

    /// <summary>The user set with <paramref name="id"/>, or null when there's none.</summary>
    public UserGlyphSet? FindUserSet(string id) => _userSets.Find(id);

    /// <summary>
    /// Whether <paramref name="name"/> can be a set's. Pass the set's own <paramref name="id"/> when renaming it, so
    /// its own name doesn't count as taken.
    /// </summary>
    public LibraryNameProblem CheckName(string name, string? id = null) => _userSets.CheckName(name, id);

    /// <summary>
    /// Adds <paramref name="glyphs"/> as the newest user set, named <paramref name="name"/>, or with a number after
    /// it if that's taken.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public UserGlyphSet Add(string name, GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);

        var added = new UserGlyphSet(_userSets.NewId(), _userSets.UniqueName(name), glyphs);
        _userSets.Add(added);
        Changed?.Invoke(this, EventArgs.Empty);
        return added;
    }

    /// <summary>Renames the user set with <paramref name="id"/> to <paramref name="name"/>, trimmed.</summary>
    /// <exception cref="ArgumentException">
    /// There's no user set with <paramref name="id"/>, or <paramref name="name"/> can't be its name
    /// (<see cref="CheckName"/>).
    /// </exception>
    public void Rename(string id, string name)
    {
        var index = _userSets.IndexOf(id);
        _userSets.ThrowIfCantBeNamed(id, name);

        var trimmed = name.Trim();
        if (_userSets[index].Name == trimmed)
            return;

        _userSets[index] = _userSets[index] with { Name = trimmed };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Changes the characters of the user set with <paramref name="id"/> to <paramref name="glyphs"/>.
    /// </summary>
    /// <exception cref="ArgumentException">There's no user set with <paramref name="id"/>.</exception>
    public void Change(string id, GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);

        var index = _userSets.IndexOf(id);
        if (_userSets[index].Glyphs.Equals(glyphs))
            return;

        _userSets[index] = _userSets[index] with { Glyphs = glyphs };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes the user set with <paramref name="id"/>.</summary>
    /// <exception cref="ArgumentException">There's no user set with <paramref name="id"/>.</exception>
    public void Delete(string id)
    {
        _userSets.RemoveAt(_userSets.IndexOf(id));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
