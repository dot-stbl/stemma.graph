using Microsoft.Extensions.AI;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;

namespace Voluta.MicrosoftAi;

/// <summary>
///     Thin helpers wrapping <see cref="IChatClient" /> for graph nodes.
/// </summary>
public static class ChatClientNode
{
    /// <summary>
    ///     Completes a chat turn and returns the assistant text (empty string when missing).
    /// </summary>
    /// <param name="chatClient">MEAI chat client.</param>
    /// <param name="messages">Chat messages for this turn.</param>
    /// <param name="options">Optional chat options.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Assistant content text.</returns>
    public static async Task<string> CompleteTextAsync(
        IChatClient chatClient,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);
        return response.Text ?? string.Empty;
    }

    /// <summary>
    ///     Builds a continue result writing assistant text into a LastValue/Append channel.
    /// </summary>
    /// <param name="chatClient">MEAI chat client.</param>
    /// <param name="channelName">Channel to write.</param>
    /// <param name="messages">Chat messages.</param>
    /// <param name="options">Optional chat options.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    /// <returns>Continue result with one channel write.</returns>
    public static async Task<NodeResult> CompleteToChannelAsync(
        IChatClient chatClient,
        string channelName,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = await CompleteTextAsync(chatClient, messages, options, cancellationToken);
        return NodeResult.Continue(new ChannelWrite(channelName, text));
    }
}
