using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class Taloger
    {
        private ConcurrentDictionary<string, Index> indexes = new ConcurrentDictionary<string, Index>();

        public Config Config { get; private set; } = new Config();

        public Indexer In(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new Indexer(GetIndex(index));
        }

        public Index GetIndex(string index)
        {
            if (this.indexes.TryGetValue(index, out Index result))
            {
                return result;
            }

            result = new Index(index, this.Config);
            this.indexes.TryAdd(index, result);
            return result;
        }
    }
}
