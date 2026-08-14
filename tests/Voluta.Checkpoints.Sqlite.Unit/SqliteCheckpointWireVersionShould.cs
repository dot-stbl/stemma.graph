using System.Text.Json;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoints.Sqlite.Wire;
using Xunit;

namespace Voluta.Checkpoints.Sqlite.Unit;

public sealed class SqliteCheckpointWireVersionShould
{
    [Fact(DisplayName = "Given a snapshot, when serialized, then formatVersion is 1")]
    public void WriteFormatVersionOne()
    {
        var document = SqliteCheckpointDocument.FromSnapshot(CreateSnapshot());

        var json = JsonSerializer.Serialize(document, JsonSerializerOptions.Web);

        using var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("formatVersion").GetInt32().ShouldBe(1);
        document.FormatVersion.ShouldBe(CheckpointWireFormat.Version);
    }

    [Fact(DisplayName = "Given JSON without formatVersion, when deserialized, then defaults to 1 and loads")]
    public void AcceptMissingFormatVersionAsOne()
    {
        const string json = """
                            {
                              "threadId": "t-legacy",
                              "step": 3,
                              "status": "Running",
                              "channelValues": {},
                              "channelVersions": {},
                              "versionsSeen": {},
                              "pendingWrites": [],
                              "pendingSends": [],
                              "nextNodes": []
                            }
                            """;

        var document = JsonSerializer.Deserialize<SqliteCheckpointDocument>(json, JsonSerializerOptions.Web);

        _ = document.ShouldNotBeNull();
        document.FormatVersion.ShouldBe(1);
        var snapshot = document.ToSnapshot();
        snapshot.ThreadId.ShouldBe("t-legacy");
        snapshot.Step.ShouldBe(3);
        snapshot.FormatVersion.ShouldBe(1);
    }

    [Fact(DisplayName = "Given formatVersion 99, when ToSnapshot is called, then throws CheckpointStoreException")]
    public void RejectUnsupportedFormatVersion()
    {
        var document = SqliteCheckpointDocument.FromSnapshot(CreateSnapshot());
        document.FormatVersion = 99;

        var exception = Should.Throw<CheckpointStoreException>(document.ToSnapshot);

        exception.Code.ShouldBe(CheckpointWireFormat.UnsupportedCode);
        exception.Message.ShouldContain("99");
    }

    private static CheckpointSnapshot CreateSnapshot()
    {
        return new CheckpointSnapshot
        {
            ThreadId = "t-wire",
            Step = 1,
            Status = GraphRunStatus.Running,
        };
    }
}
