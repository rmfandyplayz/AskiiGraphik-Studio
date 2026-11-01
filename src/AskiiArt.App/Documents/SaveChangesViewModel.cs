// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Resources;

namespace AskiiArt.App.Documents;

/// <summary>The question in the "save the changes?" dialog.</summary>
/// <param name="DocumentName">The document's file name, or "Untitled".</param>
internal sealed record SaveChangesViewModel(string DocumentName)
{
    public string Message => string.Format(CultureInfo.CurrentCulture, Strings.SaveChangesMessageFormat,
        DocumentName);
}
