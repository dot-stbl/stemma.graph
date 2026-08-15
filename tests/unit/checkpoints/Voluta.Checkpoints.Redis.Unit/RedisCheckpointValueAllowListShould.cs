using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoints.Redis;
using Xunit;

namespace Voluta.Checkpoints.Redis.Unit;

public sealed class RedisCheckpointValueAllowListShould
{
    [Theory(DisplayName = "Given allow-listed channel values, when Put/Get, then round-trips without throw")]
    [InlineData(null)]
    [InlineData("text")]
    [InlineData(true)]
    [InlineData(42L)]
    [InlineData(3.5)]
    public async Task RoundTripAllowListedValues(object? value)
    {
        var checkpointer = CreateCheckpointer();
        var snapshot = Snapshot("allow", value);

        await checkpointer.PutAsync(snapshot);
        var loaded = await checkpointer.GetAsync("allow");

        loaded.ShouldNotBeNull();
        loaded.ChannelValues.ContainsKey("payload").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given custom CLR type in channel values, when Put, then throws unsupported_value_type")]
    public async Task RejectCustomClrTypeOnPut()
    {
        var checkpointer = CreateCheckpointer();
        var snapshot = Snapshot("reject-custom", new UnsupportedDomainType("secret"));

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => checkpointer.PutAsync(snapshot));

        exception.Code.ShouldBe("checkpoint.unsupported_value_type");
        exception.Message.ShouldContain(nameof(UnsupportedDomainType));
    }

    [Fact(DisplayName = "Given over-nested value, when Put, then throws unsupported_value_type")]
    public async Task RejectDeepNestingOnPut()
    {
        var checkpointer = CreateCheckpointer();
        object? deep = "leaf";
        for (var i = 0; i < 12; i++)
        {
            deep = new List<object?> { deep };
        }

        var snapshot = Snapshot("reject-deep", deep);

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => checkpointer.PutAsync(snapshot));

        exception.Code.ShouldBe("checkpoint.unsupported_value_type");
    }

    private static RedisCheckpointer CreateCheckpointer()
    {
        return new RedisCheckpointer(
            new InMemoryRedisCheckpointStore(),
            new RedisCheckpointerOptions());
    }

    private static CheckpointSnapshot Snapshot(string threadId, object? value)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = 1,
            ChannelValues = new Dictionary<string, object?> { ["payload"] = value },
            NextNodes = [],
        };
    }
}

/// <summary>Domain type outside the wire v1 allow-list.</summary>
public sealed class UnsupportedDomainType(string secret)
{
    public string Secret { get; } = secret;
}
