// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Hosting;

internal static class BuildInfo
{
    /// <summary>
    /// True in Debug builds. Debug-only features (such as the Debug menu) check this one flag
    /// rather than scattering #if DEBUG through the code, so they stay testable in any build.
    /// </summary>
#if DEBUG
    public const bool IsDebugBuild = true;
#else
    public const bool IsDebugBuild = false;
#endif
}
