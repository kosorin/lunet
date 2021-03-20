using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    [Config(typeof(Config))]
    public class ProgramBenchmarks
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddDiagnoser(MemoryDiagnoser.Default);
            }
        }


        private List<int> _data;


        [Params(10, 100, 1000)]
        public int Count;


        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _data = Enumerable.Repeat(1, Count).Select((x, i) => x * i).ToList();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
        }


        [Benchmark]
        public List<int> Benchmark()
        {
            return _data.Select(x => x + 1).ToList();
        }
    }
}
