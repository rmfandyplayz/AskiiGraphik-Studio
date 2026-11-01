// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The font a document is drawn with, by family name (ARCHITECTURE §5.10, ADR 0019). It names
/// the font rather than pointing at a file, so a document opens on any computer that has the font;
/// one that doesn't falls back to the default font.
/// </summary>
/// <remarks>
/// Only the family is stored for now: the app uses each font's regular style, and the cell size
/// at 100% zoom is the same for every document. Equality ignores case, as font names do.
/// </remarks>
public sealed record FontReference
{
    /// <summary>
    /// The family new documents use. It's the App's default bundled font (JetBrains Mono); a test
    /// keeps the two in step.
    /// </summary>
    public const string DefaultFamilyName = "JetBrains Mono";

    /// <summary>Creates a reference to the font family <paramref name="familyName"/>.</summary>
    public FontReference(string familyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        FamilyName = familyName;
    }

    /// <summary>The font new documents use.</summary>
    public static FontReference Default { get; } = new(DefaultFamilyName);

    /// <summary>The family name, for example "JetBrains Mono".</summary>
    public string FamilyName { get; }

    /// <inheritdoc/>
    public bool Equals(FontReference? other) =>
        other is not null && string.Equals(FamilyName, other.FamilyName, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FamilyName);

    /// <inheritdoc/>
    public override string ToString() => FamilyName;
}
