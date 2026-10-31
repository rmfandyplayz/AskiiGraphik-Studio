// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Several commands that form one undo step, for example paste = add layer + write cells
/// (ARCHITECTURE §9). They apply in order and revert in reverse order.
/// </summary>
public sealed class CompositeCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly IDocumentCommand[] _commands;

    /// <summary>Groups <paramref name="commands"/>, which must not be empty.</summary>
    public CompositeCommand(IEnumerable<IDocumentCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        _commands = [.. commands];
        if (_commands.Length == 0)
            throw new ArgumentException("A composite command needs at least one command.", nameof(commands));
    }

    /// <summary>The grouped commands, in the order they apply.</summary>
    public IReadOnlyList<IDocumentCommand> Commands => _commands;

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => FixedOverheadInBytes + _commands.Sum(command => command.EstimatedMemoryInBytes);

    /// <inheritdoc/>
    /// <remarks>If one command throws, the ones before it are reverted, so the document is left unchanged.</remarks>
    public void Apply(Document document)
    {
        var appliedCount = 0;
        try
        {
            foreach (var command in _commands)
            {
                command.Apply(document);
                appliedCount++;
            }
        }
        catch
        {
            for (var index = appliedCount - 1; index >= 0; index--)
                _commands[index].Revert(document);

            throw;
        }
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        for (var index = _commands.Length - 1; index >= 0; index--)
            _commands[index].Revert(document);
    }
}
