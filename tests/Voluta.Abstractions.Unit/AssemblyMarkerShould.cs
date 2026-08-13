using Shouldly;
using Xunit;

namespace Voluta.Abstractions.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the assembly loads, then IAssemblyMarker is discoverable")]
    public void ExposeMarkerInterface()
    {
        typeof(IAssemblyMarker).ShouldNotBeNull();
        typeof(IAssemblyMarker).IsInterface.ShouldBeTrue();
    }
}
