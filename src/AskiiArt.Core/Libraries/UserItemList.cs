// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Libraries;

/// <summary>Something the user keeps in a library, such as a ramp, with a stable id and a name they see.</summary>
internal interface IUserItem
{
    string Id { get; }

    string Name { get; }
}

/// <summary>
/// The user's items of a library, oldest first, and the rules their ids and names follow (ADRs 0052, 0053): ids start
/// with the library's prefix and never change, and names are unique, ignoring case and the spaces around them, among
/// the user's items and the built-in ones. The libraries say when they change; this only holds the list.
/// </summary>
internal sealed class UserItemList<TItem>
    where TItem : class, IUserItem
{
    private readonly List<TItem> _items = [];
    private readonly HashSet<string> _builtInNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _idPrefix;

    /// <param name="idPrefix">
    /// What every user item's id starts with, so it can't be mistaken for a built-in one.
    /// </param>
    /// <param name="builtInNames">The built-in items' names as the user sees them, which user items can't take.</param>
    public UserItemList(string idPrefix, IEnumerable<string> builtInNames)
    {
        ArgumentNullException.ThrowIfNull(builtInNames);

        _idPrefix = idPrefix;
        foreach (var name in builtInNames)
            _builtInNames.Add(name.Trim());
    }

    public IReadOnlyList<TItem> Items => _items;

    public int Count => _items.Count;

    public TItem this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public TItem? Find(string id) => _items.Find(item => item.Id == id);

    /// <summary>
    /// <paramref name="name"/> trimmed, or null when it's blank: the name a saved or imported item gets, numbered by
    /// <see cref="UniqueName"/>.
    /// </summary>
    public static string? Trimmed(string? name) => name?.Trim() is { Length: > 0 } trimmed ? trimmed : null;

    /// <summary>
    /// Whether <paramref name="name"/> can be an item's. Pass the item's own <paramref name="id"/> when renaming it,
    /// so its own name doesn't count as taken.
    /// </summary>
    public LibraryNameProblem CheckName(string name, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return LibraryNameProblem.Blank;

        if (_builtInNames.Contains(trimmed))
            return LibraryNameProblem.Taken;

        var taken = _items.Any(item =>
            item.Id != id && string.Equals(item.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        return taken ? LibraryNameProblem.Taken : LibraryNameProblem.None;
    }

    /// <summary>
    /// <paramref name="name"/> without the spaces around it, if nothing has it, or else with the first number from 2
    /// up after it that makes it unique, such as "Fine 2".
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string UniqueName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Trimmed(name) is not { } trimmed)
            throw new ArgumentException("A name can't be blank.", nameof(name));

        if (CheckName(trimmed) == LibraryNameProblem.None)
            return trimmed;

        for (var number = 2; ; number++)
        {
            var numbered = $"{trimmed} {number}";
            if (CheckName(numbered) == LibraryNameProblem.None)
                return numbered;
        }
    }

    /// <summary>A new id, unlike any other.</summary>
    public string NewId() => _idPrefix + Guid.NewGuid().ToString("N");

    /// <summary><paramref name="savedId"/> if it's a user id nothing has yet, or else a new one.</summary>
    public string IdFor(string savedId)
    {
        var isFree = savedId.Length > _idPrefix.Length && savedId.StartsWith(_idPrefix, StringComparison.Ordinal)
            && Find(savedId) is null;
        return isFree ? savedId : NewId();
    }

    public void Add(TItem item) => _items.Add(item);

    public void RemoveAt(int index) => _items.RemoveAt(index);

    /// <summary>Where the item with <paramref name="id"/> is.</summary>
    /// <exception cref="ArgumentException">There's no item with <paramref name="id"/>.</exception>
    public int IndexOf(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var index = _items.FindIndex(item => item.Id == id);
        if (index < 0)
            throw new ArgumentException($"There's no user item '{id}'.", nameof(id));

        return index;
    }

    /// <summary>Throws unless <paramref name="name"/> can be the name of the item with <paramref name="id"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank or taken.</exception>
    public void ThrowIfCantBeNamed(string id, string name)
    {
        if (CheckName(name, id) is not LibraryNameProblem.None and var problem)
            throw new ArgumentException($"'{name}' can't be this name: {problem}.", nameof(name));
    }
}
