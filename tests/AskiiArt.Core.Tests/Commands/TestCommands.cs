// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

/// <summary>Helpers for building commands and comparing whole documents in tests.</summary>
internal static class TestCommands
{
    /// <summary>Marks a cell that <see cref="Paint"/> leaves alone.</summary>
    public const char SkipMarker = '~';

    /// <summary>
    /// Builds a cell edit that writes <paramref name="rows"/> from the layer's top left, in the
    /// format <see cref="TestCells.Paint"/> takes, except that <see cref="SkipMarker"/> leaves a
    /// cell alone. Throws if nothing would change.
    /// </summary>
    public static CellEditCommand Paint(Document document, Guid layerId, params string[] rows)
    {
        var builder = new CellEditBuilder(document, layerId);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                var character = rows[y][x];
                if (character == SkipMarker)
                    continue;

                builder.Set(new CellPoint(x, y),
                    character == TestCells.EmptyMarker ? Cell.Empty : TestCells.Glyph(character));
            }
        }

        return builder.Build() ?? throw new InvalidOperationException("The paint changes nothing.");
    }

    /// <summary>
    /// Describes everything about a document that commands can change, as text, so two
    /// documents can be compared and a difference shows up readably in a failed assertion.
    /// </summary>
    public static string Snapshot(Document document)
    {
        var snapshot = new StringBuilder();
        snapshot.Append("canvas ").Append(document.CanvasSize).Append(' ').Append(document.CharacterSet).Append('\n');
        foreach (var frame in document.Frames)
        {
            snapshot.Append("frame\n");
            foreach (var layer in frame.Layers)
            {
                snapshot.Append("layer ").Append(layer.Id).Append(' ').Append(layer.Properties).Append('\n');
                foreach (var row in TestCells.Read(layer))
                    snapshot.Append("  ").Append(row).Append('\n');
            }
        }

        return snapshot.ToString();
    }
}
