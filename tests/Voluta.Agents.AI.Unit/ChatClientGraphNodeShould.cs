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

public sealed class ChatClientGraphNodeShould
{
    [Fact(DisplayName = "Given ChatClientGraphNode, when invoked, then writes assistant text to output channel")]
    public async Task WriteAssistantTextToChannel()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse([new ChatMessage(ChatRole.Assistant, "hello-from-meai")]));

        var services = new ServiceCollection();
        services.AddSingleton(chatClient);
        await using var provider = services.BuildServiceProvider();

        var graph = new StateGraph()
            .AddChannel("answer", ChannelKind.LastValue)
            .AddNode(
                "chat",
                ChatClientGraphNode.Create(
                    "answer",
                    static _ => [new ChatMessage(ChatRole.User, "hi")]))
            .AddEdge(GraphConstants.Start, "chat")
            .AddEdge("chat", GraphConstants.End)
            .Compile(
                new InMemoryCheckpointer(),
                new CompileOptions { Services = provider });

        var terminal = await graph.InvokeAsync(
            [],
            new RunOptions { ThreadId = "t1", StreamMode = StreamMode.Values });

        terminal.Kind.ShouldBe(StreamEventKind.End);
        terminal.State.ShouldNotBeNull();
        terminal.State!["answer"].ShouldBe("hello-from-meai");
    }
}
