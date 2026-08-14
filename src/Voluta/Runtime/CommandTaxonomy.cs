using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
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
                VolutaErrorCodes.CommandInvalidKind,
                "Resume command Kind is required. Use Command.Approve(), Command.Reject(), or Command.Update(...).");
        }

        if (!Command.IsKnownKind(kind))
        {
            throw new GraphInvalidCommandException(
                VolutaErrorCodes.CommandInvalidKind,
                $"Unknown resume command kind '{kind}'. Expected one of: {Command.Kinds.Approve}, {Command.Kinds.Reject}, {Command.Kinds.Update}.");
        }

        if (string.Equals(kind, Command.Kinds.Update, StringComparison.Ordinal)
            && command.Values is not { Count: > 0 })
        {
            throw new GraphInvalidCommandException(
                VolutaErrorCodes.CommandInvalidPayload,
                "Resume command kind 'update' requires non-empty Values (channel writes).");
        }
    }

    /// <summary>
    ///     When multiple interrupts are pending, <see cref="Command.Resumes"/> must cover every task id.
    ///     Single-interrupt threads may use <see cref="Command.Payload"/> alone.
    /// </summary>
    /// <param name="command">Resume command from the host.</param>
    /// <param name="pendingInterrupts">Pending interrupts from the interrupted checkpoint.</param>
    /// <exception cref="GraphInvalidCommandException">When multi-interrupt map is incomplete.</exception>
    public static void EnsureMultiInterruptResumes(
        Command command,
        IReadOnlyList<PendingInterrupt> pendingInterrupts)
    {
        if (pendingInterrupts.Count <= 1)
        {
            return;
        }

        if (command.Resumes is not { Count: > 0 } resumes)
        {
            throw new GraphInvalidCommandException(
                VolutaErrorCodes.CommandInvalidPayload,
                "Multiple pending interrupts require Command.Resumes (map of task id → payload).");
        }

        foreach (var pending in pendingInterrupts)
        {
            if (!resumes.ContainsKey(pending.TaskId))
            {
                throw new GraphInvalidCommandException(
                    VolutaErrorCodes.CommandInvalidPayload,
                    $"Resume map is missing task id '{pending.TaskId}' (node '{pending.NodeName}').");
            }
        }
    }
}
