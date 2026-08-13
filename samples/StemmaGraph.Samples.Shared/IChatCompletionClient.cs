// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

namespace StemmaGraph.Samples.Shared;

/// <summary>
///     Minimal chat completion for harness samples.
/// </summary>
public interface IChatCompletionClient
{
    /// <summary>
    ///     Completes a chat turn with system + user prompts.
    /// </summary>
    public Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
