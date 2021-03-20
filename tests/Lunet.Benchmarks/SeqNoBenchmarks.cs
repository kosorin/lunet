using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Lunet.Channels;
using Lunet.Common;
using System;
using System.Collections.Generic;

namespace Lunet.Benchmarks
{
    [Config(typeof(Config))]
    public class SeqNoBenchmarks
    {
        private Random _random;
        private SeqNo _pivot;
        private List<ReliableMessage> _messages;
        private Comparison<ReliableMessage> _messageComparison;

        private class Config : ManualConfig
        {
            public Config()
            {
                AddDiagnoser(MemoryDiagnoser.Default);
            }
        }


        [Params(1, 2, 3, 8, 12, 50, 150, 1_500, 15_000)]
        public int Count;


        [GlobalSetup]
        public void GlobalSetup()
        {
            _pivot = new SeqNo((int)(SeqNo.Range * 0.75));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _random = new Random(500);
            _messages = new List<ReliableMessage>(Count);
            for (var i = 0; i < Count; i++)
            {
                _messages.Add(new ReliableMessage
                {
                    Seq = new SeqNo(_random.Next(0, SeqNo.Range)),
                });
            }

            _messageComparison = (a, b) => a.Seq.CompareTo(b.Seq);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _messages = null;
        }


        [Benchmark]
        public int ArraySort()
        {
            var result = 0;

            _messages.Sort(_messageComparison);

            foreach (var message in _messages)
            {
                result++;
            }

            return result;
        }

        [Benchmark]
        public int StackAllocSort()
        {
            var result = 0;

            Span<SeqNo> input = stackalloc SeqNo[_messages.Count];
            Span<SortItem> outputItems = stackalloc SortItem[_messages.Count];

            for (var i = 0; i < _messages.Count; i++)
            {
                input[i] = _messages[i].Seq;
            }

            _pivot.Sort(input, outputItems);

            foreach (var item in outputItems)
            {
                var message = _messages[item.Index];
                result++;
            }

            return result;
        }
    }
}
