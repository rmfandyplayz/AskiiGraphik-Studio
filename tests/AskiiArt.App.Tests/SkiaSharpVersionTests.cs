// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Reflection;
using SkiaSharp;

namespace AskiiArt.App.Tests;

// ARCHITECTURE §3: we must use exactly the SkiaSharp version Avalonia depends on. A pin that's
// too old fails the build (NU1605, see the .csproj); this test catches a pin that's too new.
// Assembly versions carry only major.minor, so a patch-level mismatch isn't caught here.
public sealed class SkiaSharpVersionTests
{
    [Fact]
    public void LoadedSkiaSharp_IsTheVersionAvaloniaWasBuiltAgainst()
    {
        var versionAvaloniaExpects = Assembly.Load("Avalonia.Skia").GetReferencedAssemblies()
            .Single(reference => reference.Name == "SkiaSharp")
            .Version;

        var loadedVersion = typeof(SKBitmap).Assembly.GetName().Version;

        Assert.Equal(versionAvaloniaExpects, loadedVersion);
    }
}
