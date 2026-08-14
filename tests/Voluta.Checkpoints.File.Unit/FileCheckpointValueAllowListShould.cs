using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.File.Wire;
using Xunit;

namespace Voluta.Checkpoints.File.Unit;

public sealed class FileCheckpointValueAllowListShould
{
    [Theory(DisplayName = "Given allow-listed channel values, when Put/Get, then round-trips without throw")]
    [MemberData(nameof(AllowListedCases))]
    public async Task RoundTripAllowListedValues(string caseName, object? value)
    {
        _ = caseName;
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-allow-" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpointer = new FileCheckpointer(root);
            var threadId = "allow-" + Guid.NewGuid().ToString("N");
            var original = CreateSnapshot(threadId, value);

            await checkpointer.PutAsync(original);
            var loaded = await checkpointer.GetAsync(threadId);

            loaded.ShouldNotBeNull();
            loaded.ChannelValues.ContainsKey("payload").ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "Given custom CLR type in channel values, when Put, then throws unsupported_value_type")]
    public async Task RejectCustomClrTypeOnPut()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-reject-" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpointer = new FileCheckpointer(root);
            var snapshot = CreateSnapshot("reject-custom", new UnsupportedDomainType("secret"));

            var exception = await Should.ThrowAsync<CheckpointStoreException>(
                () => checkpointer.PutAsync(snapshot));

            exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
            exception.Message.ShouldContain(nameof(UnsupportedDomainType));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "Given Stream in channel values, when Put, then throws unsupported_value_type")]
    public async Task RejectStreamOnPut()
    {
        var root = Path.Combine(Path.GetTempPath(), "voluta-file-stream-" + Guid.NewGuid().ToString("N"));
        try
        {
            var checkpointer = new FileCheckpointer(root);
            await using var stream = new MemoryStream([1, 2, 3]);
            var snapshot = CreateSnapshot("reject-stream", stream);

            var exception = await Should.ThrowAsync<CheckpointStoreException>(
                () => checkpointer.PutAsync(snapshot));

            exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
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
