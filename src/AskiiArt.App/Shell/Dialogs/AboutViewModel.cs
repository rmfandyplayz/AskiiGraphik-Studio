// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;

namespace AskiiArt.App.Shell.Dialogs;

internal sealed class AboutViewModel
{
    public string AppName { get; } = Strings.AppDisplayName;

    public string VersionText { get; } =
        string.Format(CultureInfo.CurrentCulture, Strings.AboutVersionFormat, AppVersion.ForDisplay);

    public string LicenseText { get; } = Strings.AboutLicense;
}
