// Integration-test smoke check. Real integration scenarios land in
// subsequent PRs alongside the runtime.

using Shouldly;
using Xunit;

namespace StemmaGraph.Integration;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the integration assembly loads, then it has the runtime type")]
    public void ReferenceStemmaGraphTypes()
    {
        typeof(AssemblyMarker).ShouldNotBeNull();
    }
}
