// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Serilog.Events;
using Serilog.Formatting;

namespace AskiiArt.App.Hosting.Logging;

/// <summary>
/// Files the app writes use \n line endings on every OS (CLAUDE.md). Serilog's {NewLine} and
/// exception stack traces use the OS line ending (\r\n on Windows), so this rewrites them.
/// </summary>
internal sealed class LfLineEndingFormatter(ITextFormatter inner) : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var buffer = new StringWriter(output.FormatProvider);
        inner.Format(logEvent, buffer);
        output.Write(buffer.ToString().Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
