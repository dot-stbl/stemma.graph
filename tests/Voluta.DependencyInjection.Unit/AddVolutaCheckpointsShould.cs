using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Voluta.Checkpoint;
using Voluta.DependencyInjection;
using Voluta.DependencyInjection.Checkpoints;
using Xunit;

namespace Voluta.DependencyInjection.Unit;

public sealed class AddVolutaCheckpointsShould
{
    [Fact(DisplayName = "Given UseInMemory, when ICheckpointer is resolved, then returns InMemoryCheckpointer")]
    public void ResolveInMemoryCheckpointer()
    {
        var services = new ServiceCollection();
        services.AddVolutaCheckpoints(static checkpoints => checkpoints.UseInMemory());

        using var provider = services.BuildServiceProvider();
        var checkpointer = provider.GetRequiredService<ICheckpointer>();

        checkpointer.ShouldBeOfType<InMemoryCheckpointer>();
    }
}
