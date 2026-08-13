// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors
//
// 01-HelloWorld — minimal console canary. Confirms the project graph builds
// and the runtime package resolves. Real end-to-end sample lands in a
// subsequent PR alongside the StateGraph runtime.

namespace StemmaGraph.Samples.HelloWorld;

internal static class Program
{
    private static Task<int> Main(string[] args)
    {
        Console.WriteLine("StemmaGraph — 01-HelloWorld sample.");
        Console.WriteLine("Real implementation lands in a subsequent PR alongside the StateGraph runtime.");
        return Task.FromResult(0);
    }
}