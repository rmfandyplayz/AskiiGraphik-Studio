// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// The ramps the app knows (ADR 0052): the built-in ones, which can't change, then the user's, which they add,
/// rename, change and delete, newest last. Names are unique, ignoring case and the spaces around them, so the Ramp
/// dropdown never shows two alike. Used on the UI thread only.
/// </summary>
public sealed class RampLibrary
{
    /// <summary>What every user ramp's id starts with, so it can't be mistaken for a built-in one.</summary>
    public const string UserRampIdPrefix = "user.";

    private readonly UserItemList<UserRamp> _userRamps;

    /// <summary>Creates a library with no user ramps, whose built-in ramps have no names to keep apart from.</summary>
    public RampLibrary()
        : this([], [])
    {
    }

    /// <summary>
    /// Creates a library holding <paramref name="userRamps"/>, such as the ones saved last time. One whose id is
    /// taken gets a new one, and one whose name is taken gets a number after it.
    /// </summary>
    /// <param name="builtInNames">The built-in ramps' names as the user sees them, which user ramps can't take.</param>
    /// <param name="userRamps">The user's ramps, oldest first.</param>
    /// <exception cref="ArgumentException">A user ramp's name is blank.</exception>
    public RampLibrary(IEnumerable<string> builtInNames, IEnumerable<UserRamp> userRamps)
    {
        ArgumentNullException.ThrowIfNull(userRamps);

        _userRamps = new UserItemList<UserRamp>(UserRampIdPrefix, builtInNames);
        foreach (var ramp in userRamps)
            _userRamps.Add(new UserRamp(_userRamps.IdFor(ramp.Id), _userRamps.UniqueName(ramp.Name), ramp.Ramp));
    }

    /// <summary>Raised after a user ramp is added, renamed, changed or deleted.</summary>
    public event EventHandler? Changed;

    /// <summary>The user's ramps, oldest first.</summary>
    public IReadOnlyList<UserRamp> UserRamps => _userRamps.Items;

    /// <summary>Whether <paramref name="id"/> is a built-in ramp's.</summary>
    public static bool IsBuiltIn(string id) => BuiltInRamps.All.Any(ramp => ramp.Id == id);

    /// <summary>The ramp with <paramref name="id"/>, built-in or the user's, or null when there's none.</summary>
    public Ramp? Find(string id) =>
        BuiltInRamps.All.FirstOrDefault(ramp => ramp.Id == id)?.Ramp ?? FindUserRamp(id)?.Ramp;

    /// <summary>The user ramp with <paramref name="id"/>, or null when there's none.</summary>
    public UserRamp? FindUserRamp(string id) => _userRamps.Find(id);

    /// <summary>
    /// Whether <paramref name="name"/> can be a ramp's. Pass the ramp's own <paramref name="id"/> when renaming it,
    /// so its own name doesn't count as taken.
    /// </summary>
    public LibraryNameProblem CheckName(string name, string? id = null) => _userRamps.CheckName(name, id);

    /// <summary>
    /// <paramref name="name"/> without the spaces around it, if no ramp has it, or else with the first number from 2
    /// up after it that makes it unique, such as "Fine 2".
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string UniqueName(string name) => _userRamps.UniqueName(name);

    /// <summary>
    /// Adds <paramref name="ramp"/> as the newest user ramp, named <paramref name="name"/>, or the
    /// <see cref="UniqueName"/> of it if that's taken.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public UserRamp Add(string name, Ramp ramp)
    {
        ArgumentNullException.ThrowIfNull(ramp);

        var added = new UserRamp(_userRamps.NewId(), UniqueName(name), ramp);
        _userRamps.Add(added);
        Changed?.Invoke(this, EventArgs.Empty);
        return added;
    }

    /// <summary>Renames the user ramp with <paramref name="id"/> to <paramref name="name"/>, trimmed.</summary>
    /// <exception cref="ArgumentException">
    /// There's no user ramp with <paramref name="id"/>, or <paramref name="name"/> can't be its name
    /// (<see cref="CheckName"/>).
    /// </exception>
    public void Rename(string id, string name)
    {
        var index = _userRamps.IndexOf(id);
        _userRamps.ThrowIfCantBeNamed(id, name);

        var trimmed = name.Trim();
        if (_userRamps[index].Name == trimmed)
            return;

        _userRamps[index] = _userRamps[index] with { Name = trimmed };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Changes the user ramp with <paramref name="id"/> to <paramref name="ramp"/>.</summary>
    /// <exception cref="ArgumentException">There's no user ramp with <paramref name="id"/>.</exception>
    public void Change(string id, Ramp ramp)
    {
        ArgumentNullException.ThrowIfNull(ramp);

        var index = _userRamps.IndexOf(id);
        if (_userRamps[index].Ramp.Equals(ramp))
            return;

        _userRamps[index] = _userRamps[index] with { Ramp = ramp };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes the user ramp with <paramref name="id"/>.</summary>
    /// <exception cref="ArgumentException">There's no user ramp with <paramref name="id"/>.</exception>
    public void Delete(string id)
    {
        _userRamps.RemoveAt(_userRamps.IndexOf(id));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
