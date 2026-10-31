// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// A font that ships inside the app (ARCHITECTURE §11, ADR 0013). Only the Regular weight is
/// bundled. Load it with <see cref="FontLoader.LoadBundled"/>.
/// </summary>
public sealed record BundledFont
{
    private const string ResourcePrefix = "AskiiArt.Imaging.Fonts.Bundled.";

    internal BundledFont(string familyName, string folderName, string fontFileName, string license,
        string licenseFileName)
    {
        FamilyName = familyName;
        FolderName = folderName;
        FontFileName = fontFileName;
        License = license;
        LicenseFileName = licenseFileName;
    }

    /// <summary>The family name stored in the font file, for example "JetBrains Mono".</summary>
    public string FamilyName { get; }

    /// <summary>The SPDX identifier of the font's license, for example "OFL-1.1".</summary>
    public string License { get; }

    /// <summary>
    /// The full path of the font's license text, which is copied next to the app at build time.
    /// </summary>
    public string LicenseFilePath =>
        Path.Combine(AppContext.BaseDirectory, "Licenses", "Fonts", FolderName, LicenseFileName);

    internal string FolderName { get; }

    internal string FontFileName { get; }

    internal string LicenseFileName { get; }

    internal string ResourceName => ResourcePrefix + FolderName + "." + FontFileName;

    internal Stream OpenFontStream() =>
        typeof(BundledFont).Assembly.GetManifestResourceStream(ResourceName)
        ?? throw new InvalidOperationException($"The bundled font resource '{ResourceName}' is missing.");
}
