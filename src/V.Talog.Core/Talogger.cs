﻿using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using V.Talog.Core;

namespace V.Talog
{
    public class Talogger : IDisposable
    {
        private ConcurrentDictionary<string, Index> indexes = new ConcurrentDictionary<string, Index>();

        public Config Config { get; private set; } = new Config();

        private CancellationTokenSource cancellation = new CancellationTokenSource();

        public Talogger()
        {
            Log.Information($"Talogger {this.GetHashCode()} init.");
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

            lock (this)
            {
                if (this.indexes.TryGetValue(index, out result))
                {
                    return result;
                }

                result = new Index(index, this.Config);
                this.indexes.TryAdd(index, result);
                return result;
            }
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

        public Suggestion Suggest()
        {
            var largeBuckets = new Dictionary<string, List<Bucket>>();
            foreach (var folder in Directory.GetDirectories(this.Config.DataPath))
            {
                var index = this.GetIndex(folder.Split('/', '\\').Last());
                var buckets = new List<Bucket>();
                foreach (var bucket in index.Buckets.Values)
                {
                    var file = new FileInfo(bucket.File);
                    var size = file.Length / 1024 / 1024;
                    if (size > 5)
                    {
                        buckets.Add(bucket);
                    }
                }
                
                if (buckets.Any())
                {
                    largeBuckets.Add(index.Name, buckets);
                }
            }

            return new Suggestion
            {
                LargeBuckets = largeBuckets
            };
        }

        public void Dispose()
        {
            this.cancellation.Cancel();

            foreach (var index in this.indexes.Values)
            {
                index.Dispose();
            }

            Log.Information($"Talogger {this.GetHashCode()} disposed.");
        }

        private void AutoSave()
        {
            while (true)
            {
                if (this.cancellation.IsCancellationRequested)
                {
                    return;
                }

                Log.Debug($"Talogger {this.GetHashCode()} auto save.");
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

                Thread.Sleep(this.Config.TaloggerAutoSaveInterval);
            }
        }
    }
}
