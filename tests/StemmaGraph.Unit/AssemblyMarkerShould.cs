// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// Smoke-test for the StemmaGraph test project. Real tests land in subsequent
// PRs alongside the runtime types they cover.

using Shouldly;
using Xunit;

namespace StemmaGraph.Unit;

public sealed class AssemblyMarkerShould
{
    [Fact(DisplayName = "When the assembly loads, then it has at least one type")]
    public void LoadAssembly()
    {
        // Sanity: the test assembly itself compiles and loads. Without this,
        // an empty test project trips no checks but proves nothing either.
        typeof(AssemblyMarker).Assembly.GetTypes().ShouldNotBeEmpty();
    }
}