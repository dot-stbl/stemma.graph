using Shouldly;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using Xunit;

namespace StemmaGraph.Abstractions.Unit.Results;

public sealed class NodeResultShould
{
    [Fact(DisplayName = "Given channel writes, when Continue is called, then returns ContinueNodeResult with those writes")]
    public void CreateContinueWithWrites()
    {
        var write = new ChannelWrite("messages", "hello");

        var result = NodeResult.Continue(write);

        _ = result.ShouldBeOfType<ContinueNodeResult>();
        result.Writes.Count.ShouldBe(1);
        result.Writes[0].ChannelName.ShouldBe("messages");
        result.Writes[0].Value.ShouldBe("hello");
    }

    [Fact(DisplayName = "When Continue is called with no writes, then returns empty write list")]
    public void CreateContinueWithEmptyWrites()
    {
        var result = NodeResult.Continue();

        _ = result.ShouldBeOfType<ContinueNodeResult>();
        result.Writes.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Given a list of writes, when Continue is called, then preserves the list")]
    public void CreateContinueFromList()
    {
        IReadOnlyList<ChannelWrite> writes =
        [
            new ChannelWrite("status", "ok"),
            new ChannelWrite("count", 2),
        ];

        var result = NodeResult.Continue(writes);

        result.Writes.Count.ShouldBe(2);
        result.Writes[0].ChannelName.ShouldBe("status");
        result.Writes[1].Value.ShouldBe(2);
    }

    [Fact(DisplayName = "Given a payload, when Interrupt is called, then returns InterruptNodeResult with that payload")]
    public void CreateInterruptWithPayload()
    {
        var payload = new Dictionary<string, object?> { ["amount"] = 50 };

        var result = NodeResult.Interrupt(payload);

        _ = result.ShouldBeOfType<InterruptNodeResult>();
        result.Payload.ShouldBeSameAs(payload);
    }

    [Fact(DisplayName = "When Interrupt is called with null payload, then payload is null")]
    public void CreateInterruptWithNullPayload()
    {
        var result = NodeResult.Interrupt(null);

        result.Payload.ShouldBeNull();
    }
}
