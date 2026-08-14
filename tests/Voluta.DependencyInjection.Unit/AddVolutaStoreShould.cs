using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Store;
using Voluta.DependencyInjection;
using Voluta.DependencyInjection.Store;
using Voluta.Store;
using Xunit;

namespace Voluta.DependencyInjection.Unit;

public sealed class AddVolutaStoreShould
{
    [Fact(DisplayName = "Given UseInMemory, when IVolutaStore is resolved, then returns InMemoryVolutaStore")]
    public void ResolveInMemoryStore()
    {
        var services = new ServiceCollection();
        services.AddVolutaStore(static store => store.UseInMemory());

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IVolutaStore>();

        store.ShouldBeOfType<InMemoryVolutaStore>();
    }

    [Fact(DisplayName = "Given Store.UseInMemory on AddVoluta, when resolved, then singleton is shared")]
    public void RegisterViaVolutaBuilder()
    {
        var services = new ServiceCollection();
        services.AddVoluta(static voluta => voluta.Store.UseInMemory());

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IVolutaStore>();
        var second = provider.GetRequiredService<IVolutaStore>();

        first.ShouldBeSameAs(second);
        first.ShouldBeOfType<InMemoryVolutaStore>();
    }

    [Fact(DisplayName = "Given AddVolutaStore without Use*, when completed, then throws")]
    public void RequireUseProvider()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddVolutaStore(_ => { }));
    }
}
