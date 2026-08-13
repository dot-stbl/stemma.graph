// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using Shouldly;
using StemmaGraph.Checkpoint;
using Xunit;

namespace StemmaGraph.Abstractions.Unit.Checkpoint;

public sealed class ICheckpointerShould
{
    [Fact(DisplayName = "When the assembly loads, then ICheckpointer is an interface with Put/Get/List")]
    public void ExposePutGetListContract()
    {
        var type = typeof(ICheckpointer);

        type.IsInterface.ShouldBeTrue();
        _ = type.GetMethod(nameof(ICheckpointer.PutAsync)).ShouldNotBeNull();
        _ = type.GetMethod(nameof(ICheckpointer.GetAsync)).ShouldNotBeNull();
        _ = type.GetMethod(nameof(ICheckpointer.ListAsync)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Given GetAsync signature, when inspected, then returns nullable CheckpointSnapshot task")]
    public void GetAsyncReturnsNullableSnapshot()
    {
        var method = typeof(ICheckpointer).GetMethod(nameof(ICheckpointer.GetAsync));

        _ = method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<CheckpointSnapshot?>));
    }
}
