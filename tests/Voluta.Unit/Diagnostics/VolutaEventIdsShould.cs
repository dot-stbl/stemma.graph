using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;
using Voluta.Diagnostics;
using Voluta.Exceptions;
using Voluta.Exceptions.Run;
using Xunit;

namespace Voluta.Unit.Diagnostics;

public sealed class VolutaEventIdsShould
{
    [Fact(DisplayName = "Given the EventId catalog, when All is enumerated, then ids and names are unique")]
    public void HaveUniqueIdsAndNames()
    {
        VolutaEventIds.All.ShouldNotBeEmpty();
        VolutaEventIds.All.Select(static eventId => eventId.Id).Distinct().Count()
            .ShouldBe(VolutaEventIds.All.Count);
        VolutaEventIds.All.Select(static eventId => eventId.Name).Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(VolutaEventIds.All.Count);
    }

    [Fact(DisplayName = "Given EventIds and error codes, when compared, then catalogs align 1:1 by name")]
    public void AlignWithErrorCodes()
    {
        VolutaEventIds.All.Count.ShouldBe(VolutaErrorCodes.All.Count);

        var eventNames = VolutaEventIds.All
            .Select(static eventId => eventId.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        var codes = VolutaErrorCodes.All
            .OrderBy(static code => code, StringComparer.Ordinal)
            .ToArray();

        eventNames.ShouldBe(codes);
    }

    [Fact(DisplayName = "Given typed graph exceptions, when constructed, then Code matches catalog constants")]
    public void TypedExceptionsUseCatalogCodes()
    {
        new GraphOutOfStepsException(limit: 10, step: 11).Code.ShouldBe(VolutaErrorCodes.GraphOutOfSteps);
        new GraphRunFailedException("failed").Code.ShouldBe(VolutaErrorCodes.GraphRunFailed);
        new GraphInvalidResumeException("not interrupted").Code.ShouldBe(VolutaErrorCodes.GraphInvalidResume);
        new GraphConcurrentUpdateException("two writers").Code.ShouldBe(VolutaErrorCodes.ChannelConcurrentUpdate);
        new GraphCompileException(VolutaErrorCodes.GraphMissingStart, "missing")
            .Code.ShouldBe(VolutaErrorCodes.GraphMissingStart);
    }

    [Fact(DisplayName = "Given a GraphException, when GetEventId is called, then returns the matching catalog EventId")]
    public void ResolveEventIdFromGraphException()
    {
        var exception = new GraphOutOfStepsException(limit: 5, step: 6);

        var eventId = VolutaExceptionLogging.GetEventId(exception);

        eventId.ShouldBe(VolutaEventIds.GraphOutOfSteps);
        eventId.Name.ShouldBe(exception.Code);
    }

    [Fact(DisplayName = "Given a CheckpointStoreException, when GetEventId is called, then returns the matching catalog EventId")]
    public void ResolveEventIdFromCheckpointException()
    {
        var exception = new CheckpointStoreException(
            VolutaErrorCodes.CheckpointPutFailed,
            "put failed");

        var eventId = VolutaExceptionLogging.GetEventId(exception);

        eventId.ShouldBe(VolutaEventIds.CheckpointPutFailed);
        eventId.Name.ShouldBe(VolutaErrorCodes.CheckpointPutFailed);
    }

    [Fact(DisplayName = "Given an unknown code, when GetEventId is called, then returns fallback id 2999")]
    public void FallbackForUnknownCode()
    {
        var eventId = VolutaExceptionLogging.GetEventId("host.custom_error");

        eventId.Id.ShouldBe(2999);
        eventId.Name.ShouldBe("host.custom_error");
    }
}
