using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Runtime;

namespace Voluta.Hosting.Wake;

/// <summary>
///     Wake signal for a worker: start a new thread or resume an interrupted one.
/// </summary>
public sealed class ThreadWake
{
    /// <summary>
    ///     Thread id (checkpoint key). Required.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    ///     Seed writes for a fresh invoke. Null when this wake is a resume.
    /// </summary>
    public IReadOnlyList<ChannelWrite>? Input { get; init; }

    /// <summary>
    ///     Resume command when the thread is interrupted. Null for a fresh invoke.
    /// </summary>
    public Command? Command { get; init; }

    /// <summary>
    ///     Builds a wake that starts a new graph run.
    /// </summary>
    /// <param name="threadId">Checkpoint / conversation id.</param>
    /// <param name="input">Initial channel writes.</param>
    public static ThreadWake Start(string threadId, params ChannelWrite[] input)
    {
        return new ThreadWake
        {
            ThreadId = threadId,
            Input = input,
        };
    }

    /// <summary>
    ///     Builds a wake that resumes an interrupted thread.
    /// </summary>
    /// <param name="threadId">Checkpoint / conversation id.</param>
    /// <param name="command">HITL resume command.</param>
    public static ThreadWake Resume(string threadId, Command command)
    {
        return new ThreadWake
        {
            ThreadId = threadId,
            Command = command,
        };
    }
}
