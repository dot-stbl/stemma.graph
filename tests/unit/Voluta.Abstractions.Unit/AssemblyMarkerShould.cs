using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Xunit;

namespace Voluta.Abstractions.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the Abstractions assembly loads, then ICheckpointer is discoverable")]
    public void ExposeCheckpointerContract()
    {
        typeof(ICheckpointer).ShouldNotBeNull();
        typeof(ICheckpointer).IsInterface.ShouldBeTrue();
    }
}
