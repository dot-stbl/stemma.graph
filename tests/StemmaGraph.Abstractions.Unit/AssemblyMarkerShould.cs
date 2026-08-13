using Shouldly;
using Xunit;

namespace StemmaGraph.Abstractions.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the assembly loads, then IAssemblyMarker is discoverable")]
    public void ExposeMarkerInterface()
    {
        _ = typeof(IAssemblyMarker).ShouldNotBeNull();
        typeof(IAssemblyMarker).IsInterface.ShouldBeTrue();
    }
}
