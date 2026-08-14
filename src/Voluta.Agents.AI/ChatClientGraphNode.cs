using System.Text;
using Microsoft.Extensions.AI;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Graph;

namespace Voluta.Agents.AI;

/// <summary>
///     Runs <see cref="IChatClient" /> as an <see cref="IGraphNode" />.
///     Prefer constructor injection of <see cref="IChatClient" />; alternatively resolve from
///     <see cref="GraphContext.Services" /> when <paramref name="chatClient" /> is null.
///     When <see cref="ChatClientNodeOptions.Stream" /> is true, uses
///     <see cref="IChatClient.GetStreamingResponseAsync" /> and bridges token fragments via
///     <see cref="GraphContext.Stream" />.
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
    /// <param name="stream">When true, stream tokens into the graph stream (default false).</param>
    /// <returns>Graph node instance.</returns>
    public static ChatClientGraphNode Create(
        string outputChannel,
        Func<GraphContext, IEnumerable<ChatMessage>> messagesFactory,
        IChatClient? chatClient = null,
        bool stream = false)
    {
        return new ChatClientGraphNode(
            new ChatClientNodeOptions
            {
                OutputChannel = outputChannel,
                Messages = messagesFactory,
                Stream = stream,
            },
            chatClient);
    }

    /// <inheritdoc />
    public async Task<NodeResult> InvokeAsync(GraphContext context, CancellationToken cancellationToken = default)
    {
        var client = chatClient ?? context.GetRequiredService<IChatClient>();
        var messages = options.Messages(context);
        var text = options.Stream
            ? await StreamAndBridgeAsync(client, messages, context, cancellationToken)
            : await CompleteAsync(client, messages, cancellationToken);
        return NodeResult.Continue(new ChannelWrite(options.OutputChannel, text));
    }

    private async Task<string> CompleteAsync(
        IChatClient client,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var response = await client.GetResponseAsync(messages, options.ChatOptions, cancellationToken);
        return response.Text ?? string.Empty;
    }

    private async Task<string> StreamAndBridgeAsync(
        IChatClient client,
        IEnumerable<ChatMessage> messages,
        GraphContext context,
        CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        await foreach (var update in client.GetStreamingResponseAsync(
                           messages,
                           options.ChatOptions,
                           cancellationToken))
        {
            var fragment = update.Text;
            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            buffer.Append(fragment);
            await context.Stream.WriteMessageAsync(fragment, cancellationToken);
        }

        return buffer.ToString();
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

    /// <summary>
    ///     When true, uses streaming MEAI API and bridges each text delta via
    ///     <see cref="GraphContext.Stream" /> as <c>StreamEventKind.Messages</c>.
    /// </summary>
    public bool Stream { get; init; }
}
