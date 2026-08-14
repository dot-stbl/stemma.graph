using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.Postgres.Wire;
using Xunit;

namespace Voluta.Checkpoints.Postgres.Unit;

public sealed class PostgresCheckpointValueAllowListShould
{
    [Theory(DisplayName = "Given allow-listed channel values, when FromSnapshot, then succeeds")]
    [MemberData(nameof(AllowListedCases))]
    public void AcceptAllowListedValues(string caseName, object? value)
    {
        _ = caseName;
        var snapshot = CreateSnapshot("allow", value);

        var document = PostgresCheckpointDocument.FromSnapshot(snapshot);

        document.ChannelValues.ContainsKey("payload").ShouldBeTrue();
    }

    [Fact(DisplayName = "Given custom CLR type in channel values, when FromSnapshot, then throws unsupported_value_type")]
    public void RejectCustomClrType()
    {
        var snapshot = CreateSnapshot("reject-custom", new UnsupportedDomainType("secret"));

        var exception = Should.Throw<CheckpointStoreException>(
            () => PostgresCheckpointDocument.FromSnapshot(snapshot));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        exception.Message.ShouldContain(nameof(UnsupportedDomainType));
    }

    [Fact(DisplayName = "Given Stream in channel values, when FromSnapshot, then throws unsupported_value_type")]
    public void RejectStream()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var snapshot = CreateSnapshot("reject-stream", stream);

        var exception = Should.Throw<CheckpointStoreException>(
            () => PostgresCheckpointDocument.FromSnapshot(snapshot));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
    }

    [Fact(DisplayName = "Given nesting deeper than max depth 8, when FromSnapshot, then throws unsupported_value_type")]
    public void RejectNestingBeyondMaxDepth()
    {
        object nested = "leaf";
        for (var index = 0; index < 10; index++)
        {
            nested = new Dictionary<string, object?>(StringComparer.Ordinal) { ["c"] = nested };
        }

        var exception = Should.Throw<CheckpointStoreException>(
            () => PostgresCheckpointDocument.FromSnapshot(CreateSnapshot("reject-depth", nested)));

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedValueTypeCode);
        exception.Message.ShouldContain("max depth");
    }

    [Fact(DisplayName = "Given dictionary with non-string key, when FromSnapshot, then throws unsupported_value_type")]
    public void RejectNonStringDictionaryKey()
    {
        var bad = new Dictionary<object, object?> { [42] = "value" };

        var exception = Should.Throw<CheckpointStoreException>(
            () => PostgresCheckpointDocument.FromSnapshot(CreateSnapshot("reject-key", bad)));

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
