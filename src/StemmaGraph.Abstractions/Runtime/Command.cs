// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Runtime;

/// <summary>
/// Resume input for an interrupted thread (approve / reject / update / free-form payload).
/// Exact taxonomy is refined at runtime implementation; this shape is the public MVP contract.
/// </summary>
public sealed class Command
{
    /// <summary>
    /// Optional kind label (for example <c>approve</c>, <c>reject</c>, <c>update</c>).
    /// </summary>
    public string? Kind { get; init; }

    /// <summary>
    /// Opaque resume payload injected according to runtime resume rules.
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    /// Optional channel-oriented values the host wants applied on resume.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Values { get; init; }
}
