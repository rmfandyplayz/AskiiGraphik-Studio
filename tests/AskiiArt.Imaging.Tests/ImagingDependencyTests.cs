// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Reflection;

namespace AskiiArt.Imaging.Tests;

public sealed class ImagingDependencyTests
{
    [Fact]
    public void ImagingAssembly_DoesNotReferenceAvalonia()
    {
        var imagingAssembly = Assembly.Load("AskiiArt.Imaging");

        var avaloniaReferences = imagingAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("Avalonia", StringComparison.Ordinal) == true);

        Assert.Empty(avaloniaReferences);
    }
}
