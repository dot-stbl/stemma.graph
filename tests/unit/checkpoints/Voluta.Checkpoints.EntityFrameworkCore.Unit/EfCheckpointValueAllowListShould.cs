using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.EntityFrameworkCore.Wire;
using Xunit;

namespace Voluta.Checkpoints.EntityFrameworkCore.Unit;

public sealed class EfCheckpointValueAllowListShould
{
    [Theory(DisplayName = "Given allow-listed channel values, when Put/Get, then round-trips without throw")]
    [MemberData(nameof(AllowListedCases))]
    public async Task RoundTripAllowListedValues(string caseName, object? value)
    {
        _ = caseName;
        await using var harness = await EfAllowListHarness.CreateAsync();
        var threadId = "allow-" + Guid.NewGuid().ToString("N");
        var original = CreateSnapshot(threadId, value);

        await harness.Checkpointer.PutAsync(original);
        var loaded = await harness.Checkpointer.GetAsync(threadId);

        loaded.ShouldNotBeNull();
        loaded.ChannelValues.ContainsKey("payload").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given custom CLR type in channel values, when Put, then throws unsupported_value_type")]
    public async Task RejectCustomClrTypeOnPut()
    {
        await using var harness = await EfAllowListHarness.CreateAsync();
        var snapshot = CreateSnapshot("reject-custom", new UnsupportedDomainType("secret"));

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => harness.Checkpointer.PutAsync(snapshot));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        exception.Message.ShouldContain(nameof(UnsupportedDomainType));
    }

    [Fact(DisplayName = "Given Stream in channel values, when Put, then throws unsupported_value_type")]
    public async Task RejectStreamOnPut()
    {
        await using var harness = await EfAllowListHarness.CreateAsync();
        await using var stream = new MemoryStream([1, 2, 3]);
        var snapshot = CreateSnapshot("reject-stream", stream);

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => harness.Checkpointer.PutAsync(snapshot));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
    }

    [Fact(DisplayName = "Given nesting deeper than max depth 8, when Put, then throws unsupported_value_type")]
    public async Task RejectNestingBeyondMaxDepth()
    {
        await using var harness = await EfAllowListHarness.CreateAsync();
        object nested = "leaf";
        for (var index = 0; index < 10; index++)
        {
            nested = new Dictionary<string, object?>(StringComparer.Ordinal) { ["c"] = nested };
        }

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => harness.Checkpointer.PutAsync(CreateSnapshot("reject-depth", nested)));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        exception.Message.ShouldContain("max depth");
    }

    [Fact(DisplayName = "Given dictionary with non-string key, when Put, then throws unsupported_value_type")]
    public async Task RejectNonStringDictionaryKey()
    {
        await using var harness = await EfAllowListHarness.CreateAsync();
        var bad = new Dictionary<object, object?> { [42] = "value" };

        var exception = await Should.ThrowAsync<CheckpointStoreException>(
            () => harness.Checkpointer.PutAsync(CreateSnapshot("reject-key", bad)));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        exception.Message.ShouldContain("string dictionary keys");
    }

    public static TheoryData<string, object?> AllowListedCases =>
        new()
        {
            { "null", null },
            { "string", "hello" },
            { "bool", true },
            { "int", 42 },
            { "long", 9_000_000_000L },
            { "double", 1.5d },
            { "decimal", 1.25m },
            { "guid", Guid.Parse("11111111-1111-1111-1111-111111111111") },
            { "byte-array", new byte[] { 1, 2, 3 } },
            { "empty-list", new List<object?>() },
            { "empty-dict", new Dictionary<string, object?>(StringComparer.Ordinal) },
            { "list-of-strings", new List<object?> { "a", "b" } },
            {
                "string-dict",
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["amount"] = 50,
                    ["note"] = "ok",
                }
            },
        };

    private static CheckpointSnapshot CreateSnapshot(string threadId, object? value)
    {
        return new CheckpointSnapshot
        {
            ThreadId = threadId,
            Step = 1,
            Status = GraphRunStatus.Running,
            ChannelValues = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["payload"] = value,
            },
        };
    }

    private sealed class UnsupportedDomainType(string name)
    {
        public string Name { get; } = name;
    }
}

file sealed class EfAllowListHarness : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private EfAllowListHarness(SqliteConnection connection, EntityFrameworkCoreCheckpointer checkpointer)
    {
        this.connection = connection;
        Checkpointer = checkpointer;
    }

    public EntityFrameworkCoreCheckpointer Checkpointer { get; }

    public static async Task<EfAllowListHarness> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<VolutaCheckpointDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setup = new VolutaCheckpointDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        var factory = new SharedOptionsFactory(options);
        var checkpointer = new EntityFrameworkCoreCheckpointer(factory);
        return new EfAllowListHarness(connection, checkpointer);
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
    }

    private sealed class SharedOptionsFactory(DbContextOptions<VolutaCheckpointDbContext> options)
        : IDbContextFactory<VolutaCheckpointDbContext>
    {
        public VolutaCheckpointDbContext CreateDbContext()
        {
            return new VolutaCheckpointDbContext(options);
        }
    }
}
