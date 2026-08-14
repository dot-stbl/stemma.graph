using Microsoft.Extensions.AI;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Graph;

namespace Voluta.Agents.AI;

/// <summary>
///     Runs <see cref="IChatClient.GetResponseAsync" /> as an <see cref="IGraphNode" />.
///     Prefer constructor injection of <see cref="IChatClient" />; alternatively resolve from
///     <see cref="GraphContext.Services" /> when <paramref name="chatClient" /> is null.
/// </summary>
public sealed class ChatClientGraphNode(
    ChatClientNodeOptions options,
    IChatClient? chatClient = null) : IGraphNode
{
    /// <summary>
    ///     Creates a node that completes chat and writes assistant text to a channel.
    /// </summary>
    /// <param name="outputChannel">Target channel.</param>
    /// <param name="messagesFactory">Builds the message list from the graph context.</param>
    /// <param name="chatClient">Optional client; when null, resolved from <see cref="GraphContext.Services" />.</param>
    /// <returns>Graph node instance.</returns>
    public static ChatClientGraphNode Create(
        string outputChannel,
        Func<GraphContext, IEnumerable<ChatMessage>> messagesFactory,
        IChatClient? chatClient = null)
    {
        return new ChatClientGraphNode(
            new ChatClientNodeOptions
            {
                OutputChannel = outputChannel,
                Messages = messagesFactory,
            },
            chatClient);
    }

    /// <inheritdoc />
    public async Task<NodeResult> InvokeAsync(GraphContext context, CancellationToken cancellationToken = default)
    {
        var client = chatClient ?? context.GetRequiredService<IChatClient>();
        var messages = options.Messages(context);
        var response = await client.GetResponseAsync(messages, options.ChatOptions, cancellationToken);
        var text = response.Text ?? string.Empty;
        return NodeResult.Continue(new ChannelWrite(options.OutputChannel, text));
    }
}

/// <summary>
///     Options for <see cref="ChatClientGraphNode" />.
/// </summary>
public sealed class ChatClientNodeOptions
{
    /// <summary>Channel receiving assistant text.</summary>
    public required string OutputChannel { get; init; }

    /// <summary>Builds chat messages for the completion call.</summary>
    public required Func<GraphContext, IEnumerable<ChatMessage>> Messages { get; init; }

    /// <summary>Optional MEAI chat options.</summary>
    public ChatOptions? ChatOptions { get; init; }
}
