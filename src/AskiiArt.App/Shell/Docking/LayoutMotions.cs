// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Moves things smoothly to where the solver says, when the layout changed because of something the user did.
/// Resizing the app window snaps instead, or everything would trail behind the window's edge.
/// </summary>
internal sealed class LayoutMotions
{
    private sealed record Motion(LayoutRect From, LayoutRect To, TimeSpan StartedAt);

    private readonly Dictionary<string, LayoutRect> _shown = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Motion> _moving = new(StringComparer.Ordinal);

    /// <summary>Whether anything hasn't arrived yet.</summary>
    public bool IsMoving => _moving.Count > 0;

    /// <summary>Where something should show now, on its way to <paramref name="target"/>.</summary>
    /// <param name="id">What's moving.</param>
    /// <param name="target">Where it's going.</param>
    /// <param name="now">The time, on any clock that keeps running.</param>
    /// <param name="duration">How long a move takes.</param>
    /// <param name="animateChanges">Whether a new target is moved to, or snapped to.</param>
    public LayoutRect Advance(string id, LayoutRect target, TimeSpan now, TimeSpan duration, bool animateChanges)
    {
        if (!_shown.TryGetValue(id, out var shown))
            return Arrive(id, target);

        if (!_moving.TryGetValue(id, out var motion) || motion.To != target)
        {
            if (shown == target || !animateChanges)
                return Arrive(id, target);

            motion = new Motion(shown, target, now);
            _moving[id] = motion;
        }

        var progress = duration > TimeSpan.Zero ? (now - motion.StartedAt) / duration : 1;
        if (progress >= 1)
            return Arrive(id, target);

        var current = Between(motion.From, motion.To, EaseOut(progress));
        _shown[id] = current;
        return current;
    }

    /// <summary>Says where something already is, such as a toolbar following the pointer.</summary>
    public void Place(string id, LayoutRect bounds)
    {
        _moving.Remove(id);
        _shown[id] = bounds;
    }

    /// <summary>Forgets something that's gone, so it doesn't slide in from its old place when it's back.</summary>
    public void Forget(string id)
    {
        _moving.Remove(id);
        _shown.Remove(id);
    }

    private LayoutRect Arrive(string id, LayoutRect target)
    {
        _moving.Remove(id);
        _shown[id] = target;
        return target;
    }

    private static double EaseOut(double progress)
    {
        var left = 1 - progress;
        return 1 - left * left * left;
    }

    private static LayoutRect Between(LayoutRect from, LayoutRect to, double amount) => new(
        from.X + (to.X - from.X) * amount,
        from.Y + (to.Y - from.Y) * amount,
        from.Width + (to.Width - from.Width) * amount,
        from.Height + (to.Height - from.Height) * amount);
}
