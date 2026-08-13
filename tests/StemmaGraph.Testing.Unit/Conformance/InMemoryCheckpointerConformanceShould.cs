// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using StemmaGraph.Checkpoint;
using Xunit;

namespace StemmaGraph.Testing.Unit.Conformance;

/// <summary>
/// Binds the shared checkpoint conformance suite to <see cref="InMemoryCheckpointer"/> for CI.
/// </summary>
public sealed class InMemoryCheckpointerConformanceShould
{
    [Fact(DisplayName = "Given InMemoryCheckpointer, when conformance suite runs, then all mandatory scenarios pass")]
    public async Task PassAllMandatoryScenarios()
    {
        await CheckpointerConformance.RunAllAsync(new InMemoryCheckpointer());
    }

    [Fact(DisplayName = "Given unknown thread, when GetAsync is called, then returns null")]
    public async Task GetMissingReturnsNull()
    {
        await CheckpointerConformance.GetMissingReturnsNullAsync(new InMemoryCheckpointer());
    }

    [Fact(DisplayName = "Given put snapshot, when GetAsync is called, then roundtrips C-shape fields")]
    public async Task PutGetRoundtrip()
    {
        await CheckpointerConformance.PutGetRoundtripAsync(new InMemoryCheckpointer());
    }

    [Fact(DisplayName = "Given multiple steps, when ListAsync is supported, then returns ordered by step")]
    public async Task ListOrderedWhenSupported()
    {
        await CheckpointerConformance.ListOrderedByStepWhenSupportedAsync(new InMemoryCheckpointer());
    }

    [Fact(DisplayName = "Given interrupted snapshot, when GetAsync is called, then status and payload roundtrip")]
    public async Task InterruptFieldsRoundtrip()
    {
        await CheckpointerConformance.InterruptFieldsRoundtripAsync(new InMemoryCheckpointer());
    }

    [Fact(DisplayName = "Given pending writes, when GetAsync is called, then pending writes roundtrip")]
    public async Task PendingWritesRoundtrip()
    {
        await CheckpointerConformance.PendingWritesRoundtripAsync(new InMemoryCheckpointer());
    }
}
