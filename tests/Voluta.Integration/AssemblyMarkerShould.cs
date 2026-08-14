using Shouldly;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Integration;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the integration assembly loads, then it references the runtime builder")]
    public void ReferenceVolutaTypes()
    {
        typeof(StateGraph).ShouldNotBeNull();
    }
}
