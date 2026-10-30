// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Actions;

/// <summary>
/// Contributes actions to the <see cref="ActionRegistry"/>. Each feature registers one provider in
/// DI, the same way a future plugin would (ARCHITECTURE §18).
/// </summary>
internal interface IActionProvider
{
    IEnumerable<AppAction> CreateActions();
}
