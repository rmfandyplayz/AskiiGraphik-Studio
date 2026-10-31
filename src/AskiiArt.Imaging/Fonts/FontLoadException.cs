// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Fonts;

/// <summary>Thrown when a file can't be read as a font.</summary>
public sealed class FontLoadException : Exception
{
    /// <summary>Creates the exception with a default message.</summary>
    public FontLoadException()
        : base("The font could not be loaded.")
    {
    }

    /// <summary>Creates the exception with <paramref name="message"/>.</summary>
    public FontLoadException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with <paramref name="message"/> and the exception that caused it.</summary>
    public FontLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
