// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Reflection;

namespace AskiiArt.Core.Tests;

public sealed class CoreDependencyTests
{
    [Fact]
    public void CoreAssembly_ReferencesOnlyTheBaseClassLibrary()
    {
        var coreAssembly = Assembly.Load("AskiiArt.Core");

        var nonBaseClassLibraryReferences = coreAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not ("netstandard" or "mscorlib") && name?.StartsWith("System", StringComparison.Ordinal) != true);

        Assert.Empty(nonBaseClassLibraryReferences);
    }
}
