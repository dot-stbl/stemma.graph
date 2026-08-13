using BenchmarkDotNet.Running;
using StemmaGraph.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
