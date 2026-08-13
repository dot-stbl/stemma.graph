// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// Smoke-test for the Abstractions test project. Real interface-contract
// tests land in subsequent PRs.

using Shouldly;
using Xunit;

namespace StemmaGraph.Abstractions.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the assembly loads, then IAssemblyMarker is discoverable")]
    public void ExposeMarkerInterface()
    {
        typeof(IAssemblyMarker).ShouldNotBeNull();
        typeof(IAssemblyMarker).IsInterface.ShouldBeTrue();
    }
}