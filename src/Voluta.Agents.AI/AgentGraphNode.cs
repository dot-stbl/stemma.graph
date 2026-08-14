using Microsoft.Agents.AI;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Graph;
using Voluta.Graph.Builder;

namespace Voluta.Agents.AI;

/// <summary>
///     Runs a Microsoft Agent Framework <see cref="AIAgent" /> as a Voluta <see cref="IGraphNode" />.
///     Register with <see cref="StateGraph.AddNode(string, IGraphNode)" /> or DI + <c>AddNode&lt;T&gt;</c>.
/// </summary>
public sealed class AgentGraphNode(AIAgent agent, AgentNodeOptions options) : IGraphNode
{
    /// <summary>
    ///     Creates a node that writes agent text to <paramref name="outputChannel" />.
    /// </summary>
    /// <param name="agent">MAF agent instance.</param>
    /// <param name="outputChannel">Target channel name.</param>
    /// <param name="inputChannel">Optional source channel for the user message.</param>
    /// <returns>Ready-to-register graph node.</returns>
    public static AgentGraphNode Create(AIAgent agent, string outputChannel, string? inputChannel = null)
    {
        return new AgentGraphNode(
            agent,
            new AgentNodeOptions
            {
                OutputChannel = outputChannel,
                InputChannel = inputChannel,
            });
    }

    /// <inheritdoc />
    public async Task<NodeResult> InvokeAsync(GraphContext context, CancellationToken cancellationToken = default)
    {
        var message = ResolveMessage(context);
        var session = await agent.CreateSessionAsync(cancellationToken);
        var response = await agent.RunAsync(message, session, cancellationToken: cancellationToken);
        var text = response.Text ?? string.Empty;
        return NodeResult.Continue(new ChannelWrite(options.OutputChannel, text));
    }

    private string ResolveMessage(GraphContext context)
    {
        return options.InputChannel is { } channelName
            && context.Read<string>(channelName) is { Length: > 0 } fromChannel
            ? fromChannel
            : context.TaskPayload is string taskText && taskText.Length > 0
                ? taskText
                : context.ResumePayload is string resumeText && resumeText.Length > 0
                    ? resumeText
                    : string.Empty;
    }
}
