// SPDX-License-Identifier: MIT
// Copyright (c) Stemma contributors

using BenchmarkDotNet.Running;
using StemmaGraph.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
