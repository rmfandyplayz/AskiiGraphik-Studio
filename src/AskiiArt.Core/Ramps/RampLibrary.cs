// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>Why a name can't be a ramp's (<see cref="RampLibrary.CheckName"/>).</summary>
public enum RampNameProblem
{
    /// <summary>Nothing: the name can be used.</summary>
    None,

    /// <summary>The name is empty or only spaces.</summary>
    Blank,

    /// <summary>Another ramp has the name, or one that differs only in case.</summary>
    Taken
}

/// <summary>
/// The ramps the app knows (ADR 0052): the built-in ones, which can't change, then the user's, which they add,
/// rename, change and delete, newest last. Names are unique, ignoring case and the spaces around them, so the Ramp
/// dropdown never shows two alike. Used on the UI thread only.
/// </summary>
public sealed class RampLibrary
{
    /// <summary>What every user ramp's id starts with, so it can't be mistaken for a built-in one.</summary>
    public const string UserRampIdPrefix = "user.";

    private readonly List<UserRamp> _userRamps = [];
    private readonly HashSet<string> _builtInNames = new(StringComparer.OrdinalIgnoreCase);

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
        ArgumentNullException.ThrowIfNull(builtInNames);
        ArgumentNullException.ThrowIfNull(userRamps);

        foreach (var name in builtInNames)
            _builtInNames.Add(name.Trim());

        foreach (var ramp in userRamps)
        {
            var id = IsFreeUserRampId(ramp.Id) ? ramp.Id : NewId();
            _userRamps.Add(new UserRamp(id, UniqueName(ThrowIfBlank(ramp.Name)), ramp.Ramp));
        }
    }

    /// <summary>Raised after a user ramp is added, renamed, changed or deleted.</summary>
    public event EventHandler? Changed;

    /// <summary>The user's ramps, oldest first.</summary>
    public IReadOnlyList<UserRamp> UserRamps => _userRamps;

    /// <summary>Whether <paramref name="id"/> is a built-in ramp's.</summary>
    public static bool IsBuiltIn(string id) => BuiltInRamps.All.Any(ramp => ramp.Id == id);

    /// <summary>The ramp with <paramref name="id"/>, built-in or the user's, or null when there's none.</summary>
    public Ramp? Find(string id) =>
        BuiltInRamps.All.FirstOrDefault(ramp => ramp.Id == id)?.Ramp ?? FindUserRamp(id)?.Ramp;

    /// <summary>The user ramp with <paramref name="id"/>, or null when there's none.</summary>
    public UserRamp? FindUserRamp(string id) => _userRamps.Find(ramp => ramp.Id == id);

    /// <summary>
    /// Whether <paramref name="name"/> can be a ramp's. Pass the ramp's own <paramref name="id"/> when renaming it,
    /// so its own name doesn't count as taken.
    /// </summary>
    public RampNameProblem CheckName(string name, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return RampNameProblem.Blank;

        if (_builtInNames.Contains(trimmed))
            return RampNameProblem.Taken;

        var taken = _userRamps.Any(ramp =>
            ramp.Id != id && string.Equals(ramp.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        return taken ? RampNameProblem.Taken : RampNameProblem.None;
    }

    /// <summary>
    /// <paramref name="name"/> without the spaces around it, if no ramp has it, or else with the first number from 2
    /// up after it that makes it unique, such as "Fine 2".
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public string UniqueName(string name)
    {
        var trimmed = ThrowIfBlank(name);
        if (CheckName(trimmed) == RampNameProblem.None)
            return trimmed;

        for (var number = 2; ; number++)
        {
            var numbered = $"{trimmed} {number}";
            if (CheckName(numbered) == RampNameProblem.None)
                return numbered;
        }
    }

    /// <summary>
    /// Adds <paramref name="ramp"/> as the newest user ramp, named <paramref name="name"/>, or the
    /// <see cref="UniqueName"/> of it if that's taken.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public UserRamp Add(string name, Ramp ramp)
    {
        ArgumentNullException.ThrowIfNull(ramp);

        var added = new UserRamp(NewId(), UniqueName(name), ramp);
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
        var index = IndexOf(id);
        if (CheckName(name, id) is not RampNameProblem.None and var problem)
            throw new ArgumentException($"'{name}' can't be a ramp's name: {problem}.", nameof(name));

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

        var index = IndexOf(id);
        if (_userRamps[index].Ramp.Equals(ramp))
            return;

        _userRamps[index] = _userRamps[index] with { Ramp = ramp };
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes the user ramp with <paramref name="id"/>.</summary>
    /// <exception cref="ArgumentException">There's no user ramp with <paramref name="id"/>.</exception>
    public void Delete(string id)
    {
        _userRamps.RemoveAt(IndexOf(id));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private int IndexOf(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var index = _userRamps.FindIndex(ramp => ramp.Id == id);
        if (index < 0)
            throw new ArgumentException($"There's no user ramp '{id}'.", nameof(id));

        return index;
    }

    private bool IsFreeUserRampId(string id) =>
        id.Length > UserRampIdPrefix.Length && id.StartsWith(UserRampIdPrefix, StringComparison.Ordinal)
        && FindUserRamp(id) is null;

    private static string NewId() => UserRampIdPrefix + Guid.NewGuid().ToString("N");

    private static string ThrowIfBlank(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("A ramp's name can't be blank.", nameof(name));

        return trimmed;
    }
}
