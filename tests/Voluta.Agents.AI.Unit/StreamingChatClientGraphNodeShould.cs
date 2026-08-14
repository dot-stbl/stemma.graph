using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Runtime;
using Voluta.Abstractions.Streaming;
using Voluta.Agents.AI;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;
using Xunit;

namespace Voluta.Agents.AI.Unit;

public sealed class StreamingChatClientGraphNodeShould
{
    [Fact(DisplayName = "Given Stream=true and fake streaming client, when StreamAsync, then bridges tokens and writes full text")]
    public async Task BridgeTokensIntoGraphStream()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => FakeTokensAsync(
                ["Hel", "lo", "!", ""],
                callInfo.ArgAt<CancellationToken>(2)));

        var services = new ServiceCollection();
        services.AddSingleton(chatClient);
        await using var provider = services.BuildServiceProvider();

        var graph = new StateGraph()
            .AddChannel("answer", ChannelKind.LastValue)
            .AddNode(
                "chat",
                ChatClientGraphNode.Create(
                    "answer",
                    static _ => [new ChatMessage(ChatRole.User, "hi")],
                    stream: true))
            .AddEdge(GraphConstants.Start, "chat")
            .AddEdge("chat", GraphConstants.End)
            .Compile(
                new InMemoryCheckpointer(),
                new CompileOptions { Services = provider });

        var events = new List<StreamEvent>();
        await foreach (var item in graph.StreamAsync(
                           [],
                           new RunOptions { ThreadId = "stream-meai-1", StreamMode = StreamMode.Messages }))
        {
            events.Add(item);
        }

        var tokens = events
            .Where(static item => item.Kind == StreamEventKind.Messages)
            .Select(static item => item.Payload)
            .ToList();
        tokens.ShouldBe(["Hel", "lo", "!"]);
        events.Last().Kind.ShouldBe(StreamEventKind.End);

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "stream-meai-2", StreamMode = StreamMode.Values });
        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State.ShouldNotBeNull();
        terminal.State!["answer"].ShouldBe("Hello!");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> FakeTokensAsync(
        IReadOnlyList<string> fragments,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var fragment in fragments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, fragment);
        }
    }
}
