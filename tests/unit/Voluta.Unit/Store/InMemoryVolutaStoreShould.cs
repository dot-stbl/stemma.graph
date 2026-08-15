using Shouldly;
using Voluta.Store;
using Xunit;

namespace Voluta.Unit.Store;

public sealed class InMemoryVolutaStoreShould
{
    private static readonly string[] UsersMemories = ["users", "memories"];
    private static readonly string[] OtherNs = ["other"];

    [Fact(DisplayName = "Given unknown key, when GetAsync is called, then returns null")]
    public async Task ReturnNullOnGetMiss()
    {
        var store = new InMemoryVolutaStore();

        var item = await store.GetAsync(UsersMemories, "missing");

        item.ShouldBeNull();
    }

    [Fact(DisplayName = "Given put item, when GetAsync is called, then roundtrips value and namespace")]
    public async Task RoundtripPutGet()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-08-15T12:00:00Z"));
        var store = new InMemoryVolutaStore(clock);

        await store.PutAsync(UsersMemories, "pref", "dark");
        var item = await store.GetAsync(UsersMemories, "pref");

        item.ShouldNotBeNull();
        item!.Key.ShouldBe("pref");
        item.Value.ShouldBe("dark");
        item.Namespace.ShouldBe(UsersMemories);
        item.UpdatedAt.ShouldBe(clock.GetUtcNow());
    }

    [Fact(DisplayName = "Given multiple keys, when ListAsync is called, then returns ordered by key")]
    public async Task ListOrderedByKey()
    {
        var store = new InMemoryVolutaStore();
        await store.PutAsync(UsersMemories, "z", 1);
        await store.PutAsync(UsersMemories, "a", 2);
        await store.PutAsync(OtherNs, "noise", 3);

        var list = await store.ListAsync(UsersMemories);

        list.Count.ShouldBe(2);
        list[0].Key.ShouldBe("a");
        list[1].Key.ShouldBe("z");
    }

    [Fact(DisplayName = "Given put then delete, when GetAsync is called, then returns null")]
    public async Task DeleteRemovesItem()
    {
        var store = new InMemoryVolutaStore();
        await store.PutAsync([], "root-key", "v");
        await store.DeleteAsync([], "root-key");

        var item = await store.GetAsync([], "root-key");

        item.ShouldBeNull();
    }

    [Fact(DisplayName = "Given missing key, when DeleteAsync is called, then does not throw")]
    public async Task DeleteMissIsNoOp()
    {
        var store = new InMemoryVolutaStore();

        await store.DeleteAsync(UsersMemories, "never-put");
    }

    [Fact(DisplayName = "Given empty store, when ListAsync is called, then returns empty list")]
    public async Task ListEmptyNamespace()
    {
        var store = new InMemoryVolutaStore();

        var list = await store.ListAsync(UsersMemories);

        list.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Given concurrent puts, when ListAsync is called, then all keys are visible")]
    public async Task ConcurrentPutsAreVisible()
    {
        var store = new InMemoryVolutaStore();
        var tasks = Enumerable.Range(0, 50)
            .Select(index => store.PutAsync(UsersMemories, $"k{index:D2}", index))
            .ToArray();

        await Task.WhenAll(tasks);
        var list = await store.ListAsync(UsersMemories);

        list.Count.ShouldBe(50);
        list[0].Key.ShouldBe("k00");
        list[^1].Key.ShouldBe("k49");
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
