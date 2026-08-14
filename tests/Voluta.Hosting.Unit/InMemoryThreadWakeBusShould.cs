using Shouldly;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Runtime;
using Voluta.Hosting.Wake;
using Xunit;

namespace Voluta.Hosting.Unit;

public sealed class InMemoryThreadWakeBusShould
{
    [Fact(DisplayName = "Given a start wake, when enqueued, then ReadAllAsync yields it")]
    public async Task YieldEnqueuedStartWake()
    {
        var bus = new InMemoryThreadWakeBus();
        var wake = ThreadWake.Start("t-1", new ChannelWrite("messages", "hi"));

        await bus.EnqueueAsync(wake);
        bus.Complete();

        var items = new List<ThreadWake>();
        await foreach (var item in bus.ReadAllAsync())
        {
            items.Add(item);
        }

        items.ShouldHaveSingleItem();
        items[0].ThreadId.ShouldBe("t-1");
        items[0].Input.ShouldNotBeNull();
        items[0].Command.ShouldBeNull();
    }

    [Fact(DisplayName = "Given a resume wake, when enqueued, then Command is preserved")]
    public async Task PreserveResumeCommand()
    {
        var bus = new InMemoryThreadWakeBus();
        var command = Command.Approve("ok");
        await bus.EnqueueAsync(ThreadWake.Resume("t-2", command));
        bus.Complete();

        ThreadWake? received = null;
        await foreach (var item in bus.ReadAllAsync())
        {
            received = item;
        }

        received.ShouldNotBeNull();
        received.ThreadId.ShouldBe("t-2");
        received.Command.ShouldBeSameAs(command);
        received.Input.ShouldBeNull();
    }

    [Fact(DisplayName = "Given multiple wakes, when Complete is called, then all items are drained")]
    public async Task DrainAllBeforeComplete()
    {
        var bus = new InMemoryThreadWakeBus();
        await bus.EnqueueAsync(ThreadWake.Start("a"));
        await bus.EnqueueAsync(ThreadWake.Start("b"));
        await bus.EnqueueAsync(ThreadWake.Start("c"));
        bus.Complete();

        var ids = new List<string>();
        await foreach (var item in bus.ReadAllAsync())
        {
            ids.Add(item.ThreadId);
        }

        ids.ShouldBe(["a", "b", "c"]);
    }
}
