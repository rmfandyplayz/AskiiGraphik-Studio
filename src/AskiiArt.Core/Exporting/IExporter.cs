// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Exporting;

/// <summary>
/// A format the document can be exported to (ARCHITECTURE §16). Exporters register through DI, the
/// way a plugin's would (§18).
/// </summary>
/// <remarks>
/// Provisional until a second exporter exists (PNG and HTML, M7): those will need options, such as a
/// scale, which may change this shape (ADR 0022).
/// </remarks>
public interface IExporter
{
    /// <summary>A stable id, for example "askiiart.export.text". Never shown to the user.</summary>
    string Id { get; }

    /// <summary>The extension of the files it writes, with the dot, for example ".txt".</summary>
    string FileExtension { get; }

    /// <summary>
    /// The whole exported file for the document's active frame. Call it on the thread that changes
    /// the document (the UI thread), since it reads the document; writing the bytes can happen anywhere.
    /// </summary>
    byte[] Export(Document document);
}
