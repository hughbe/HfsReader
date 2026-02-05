using BenchmarkDotNet.Running;
using HfsReader.Benchmarks;

// Run all benchmarks
BenchmarkSwitcher.FromAssembly(typeof(String16Benchmarks).Assembly).Run(args);
