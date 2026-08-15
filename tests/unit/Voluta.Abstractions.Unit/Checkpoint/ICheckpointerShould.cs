using Shouldly;
using Voluta.Abstractions.Checkpoint;
using Xunit;

namespace Voluta.Abstractions.Unit.Checkpoint;

public sealed class ICheckpointerShould
{
    [Fact(DisplayName = "When the assembly loads, then ICheckpointer is an interface with Put/Get/List")]
    public void ExposePutGetListContract()
    {
        var type = typeof(ICheckpointer);

        type.IsInterface.ShouldBeTrue();
        type.GetMethod(nameof(ICheckpointer.PutAsync)).ShouldNotBeNull();
        type.GetMethod(nameof(ICheckpointer.GetAsync)).ShouldNotBeNull();
        type.GetMethod(nameof(ICheckpointer.ListAsync)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Given GetAsync signature, when inspected, then returns nullable CheckpointSnapshot task")]
    public void GetAsyncReturnsNullableSnapshot()
    {
        var method = typeof(ICheckpointer).GetMethod(nameof(ICheckpointer.GetAsync));

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<CheckpointSnapshot?>));
    }

    [Fact(DisplayName = "When the assembly loads, then IThreadDiscovery exposes ListThreadIdsAsync")]
    public void ExposeThreadDiscoveryContract()
    {
        var type = typeof(IThreadDiscovery);

        type.IsInterface.ShouldBeTrue();
        type.GetMethod(nameof(IThreadDiscovery.ListThreadIdsAsync)).ShouldNotBeNull();
    }
}
