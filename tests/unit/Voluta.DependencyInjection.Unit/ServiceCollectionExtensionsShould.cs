using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Results;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Xunit;

namespace Voluta.DependencyInjection.Unit;

public sealed class ServiceCollectionExtensionsShould
{
    [Fact(DisplayName = "Given compiled graph, when AddVoluta is called, then graph resolves as singleton")]
    public void RegisterCompiledGraphAsSingleton()
    {
        var graph = LinearGraphFixture.Create();

        var services = new ServiceCollection();
        services.AddVoluta(graph);

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<CompiledGraph>();
        var second = provider.GetRequiredService<CompiledGraph>();

        first.ShouldBeSameAs(graph);
        second.ShouldBeSameAs(first);
    }

    [Fact(DisplayName = "Given factory, when AddVoluta is called, then factory graph is singleton")]
    public void RegisterFactoryGraphAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddVoluta(static _ => LinearGraphFixture.Create());

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<CompiledGraph>();
        var second = provider.GetRequiredService<CompiledGraph>();

        first.ShouldBeSameAs(second);
    }
}
