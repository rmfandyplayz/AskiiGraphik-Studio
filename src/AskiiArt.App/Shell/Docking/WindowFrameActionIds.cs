// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Docking;

/// <summary>The actions in a window's menu. Each takes the window's id as its command parameter.</summary>
internal static class WindowFrameActionIds
{
    public const string Float = "askiiart.window.float";
    public const string TurnIntoDock = "askiiart.window.turnIntoDock";
    public const string TurnIntoWidget = "askiiart.window.turnIntoWidget";
    public const string Close = "askiiart.window.close";
}
