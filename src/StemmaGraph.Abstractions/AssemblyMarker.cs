// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// Marker for the StemmaGraph.Abstractions assembly. Real interfaces will
// land in subsequent PRs as the MVP runtime is implemented. See
// https://github.com/dot-stbl/stemma.graph — CLAUDE.md for the architecture
// overview and the MVP roadmap.

namespace StemmaGraph;

/// <summary>
/// Marker interface for the StemmaGraph.Abstractions assembly. Real contracts
/// (IStateGraph, ICompiledGraph, ICheckpointer, IReducer, …) land in
/// subsequent PRs.
/// </summary>
public interface IAssemblyMarker;