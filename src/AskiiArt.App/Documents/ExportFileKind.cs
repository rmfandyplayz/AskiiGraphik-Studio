// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Documents;

/// <summary>Which kind of file an export writes, which decides what the save dialog offers.</summary>
internal enum ExportFileKind
{
    /// <summary>Plain text (.txt).</summary>
    Text,

    /// <summary>ANSI art for terminals (.ans).</summary>
    Ansi
}
