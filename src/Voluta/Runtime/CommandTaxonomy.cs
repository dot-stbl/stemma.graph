using Voluta.Abstractions.Runtime;
using Voluta.Exceptions.Run;

namespace Voluta.Runtime;

/// <summary>
///     Validates HITL resume commands against the closed 0.2 taxonomy.
/// </summary>
public static class CommandTaxonomy
{
    /// <summary>
    ///     Ensures <paramref name="command"/> has a known kind and satisfies kind-specific rules.
    /// </summary>
    /// <param name="command">Resume command from the host.</param>
    /// <exception cref="GraphInvalidCommandException">When kind or payload/values are invalid.</exception>
    public static void EnsureValid(Command command)
    {
        if (command.Kind is not { Length: > 0 } kind)
        {
            throw new GraphInvalidCommandException(
                "Resume command Kind is required. Use Command.Approve(), Command.Reject(), or Command.Update(...).");
        }

        if (!Command.IsKnownKind(kind))
        {
            throw new GraphInvalidCommandException(
                $"Unknown resume command kind '{kind}'. Expected one of: {Command.Kinds.Approve}, {Command.Kinds.Reject}, {Command.Kinds.Update}.");
        }

        if (string.Equals(kind, Command.Kinds.Update, StringComparison.Ordinal)
            && command.Values is not { Count: > 0 })
        {
            throw new GraphInvalidCommandException(
                "Resume command kind 'update' requires non-empty Values (channel writes).");
        }
    }
}
