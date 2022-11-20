using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace V.Talog
{
    public class Taloger : IDisposable
    {
        private ConcurrentDictionary<string, Index> indexes = new ConcurrentDictionary<string, Index>();

        public Config Config { get; private set; } = new Config();

        private CancellationTokenSource cancellation = new CancellationTokenSource();

        public Taloger()
        {
            Log.Information($"Taloger {this.GetHashCode()} init.");
            Task.Run(this.AutoSave, this.cancellation.Token);
        }

        public Indexer CreateIndexer(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new Indexer(this.GetIndex(index));
        }

        public Searcher CreateSearcher(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new Searcher(this.GetIndex(index));
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

        public void RemoveIndex(string index)
        {
            var folder = Path.Combine(this.Config.DataPath, index);
            if (!Directory.Exists(folder))
            {
                return;
            }

            if (this.indexes.TryRemove(index, out var idx))
            {
                idx.Dispose();
            }
            Directory.Delete(folder, true);
        }

        public void Dispose()
        {
            this.cancellation.Cancel();

            foreach (var index in this.indexes.Values)
            {
                index.Dispose();
            }

            Log.Information($"Taloger {this.GetHashCode()} disposed.");
        }

        private void AutoSave()
        {
            while (true)
            {
                if (this.cancellation.IsCancellationRequested)
                {
                    return;
                }

                Log.Debug($"Taloger {this.GetHashCode()} auto save.");
                var now = DateTime.Now;
                var keys = this.indexes.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!this.indexes.TryGetValue(key, out var index))
                    {
                        continue;
                    }

                    if (index.LastUsedTime.AddSeconds(this.Config.IdleIndexInterval) <= now)
                    {
                        if (this.indexes.TryRemove(key, out index))
                        {
                            Log.Information($"index {key} 已超过 {this.Config.IdleIndexInterval}s 未被使用，Dispose 释放资源");
                            index.Dispose();
                        }
                    }
                    else
                    {
                        index.Save();
                    }
                }

                Thread.Sleep(this.Config.TalogerAutoSaveInterval);
            }
        }
    }
}
