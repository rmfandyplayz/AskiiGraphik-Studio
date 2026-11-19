// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.Core.Ramps;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Ramps;

/// <summary>One ramp in the Ramps window's list: its name and its characters, lightest first.</summary>
internal sealed class RampRow : ObservableObject
{
    private RampRow(string id, string name, Ramp ramp, bool isBuiltIn)
    {
        Id = id;
        Name = name;
        Ramp = ramp;
        IsBuiltIn = isBuiltIn;
    }

    /// <summary>The ramp's id in the library.</summary>
    public string Id { get; }

    /// <summary>Whether it's one of the app's own ramps, which can't be changed.</summary>
    public bool IsBuiltIn { get; }

    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public Ramp Ramp
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(Characters));
        }
    }

    /// <summary>The characters, lightest first.</summary>
    public string Characters => Ramp.ToString();

    public static RampRow For(BuiltInRamp ramp) => new(ramp.Id, UiText.Get(ramp.NameKey), ramp.Ramp, isBuiltIn: true);

    public static RampRow For(UserRamp ramp) => new(ramp.Id, ramp.Name, ramp.Ramp, isBuiltIn: false);

    /// <summary>Shows <paramref name="ramp"/>, the user ramp this row is for, as it is now.</summary>
    public void Update(UserRamp ramp)
    {
        Name = ramp.Name;
        Ramp = ramp.Ramp;
    }
}
