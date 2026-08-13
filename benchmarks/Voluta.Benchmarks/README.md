# Voluta.Benchmarks

BenchmarkDotNet baselines for core runtime paths. **Not gated on PR CI.**

## Run

```bash
# all jobs (Release recommended)
dotnet run -c Release --project benchmarks/Voluta.Benchmarks

# filter
dotnet run -c Release --project benchmarks/Voluta.Benchmarks -- --filter *Linear*
```

## Coverage

| Benchmark | Path |
|-----------|------|
| LinearInvoke | 1-node superstep + invoke |
| CycleFiveTicks | conditional cycle (5 steps) |
| ParallelAppend | two ready nodes + Append merge |
| CheckpointPutGet | InMemory C-shape put/get |
