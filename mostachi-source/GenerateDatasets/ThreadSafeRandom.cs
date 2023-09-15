using System;
using System.Collections.Generic;
using System.Text;

namespace GenerateDatasets
{
    public class ThreadSafeRandom
    {
        private static readonly Random _global = new Random(20);
        [ThreadStatic] private static Random _local;

        public int Next()
        {
            if (_local == null)
                MakeNew();
            return _local.Next();
        }

        private static void MakeNew()
        {
            int seed;
            lock (_global)
            {
                seed = _global.Next();
            }
            _local = new Random(seed);
        }

        public double NextDouble()
        {
            if (_local == null)
                MakeNew();
            return _local.NextDouble();
        }

        public Random Instance
        {
            get
            {
                if (_local == null)
                    MakeNew();
                return _local;
            }
        }
    }
}
