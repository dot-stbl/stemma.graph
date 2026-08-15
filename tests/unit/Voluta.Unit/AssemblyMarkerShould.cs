using Shouldly;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the Voluta assembly loads, then it exposes the StateGraph builder")]
    public void LoadAssembly()
    {
        typeof(StateGraph).Assembly.GetTypes().ShouldNotBeEmpty();
    }
}
