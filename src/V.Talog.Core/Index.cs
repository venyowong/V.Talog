using ProtoBuf;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace V.Talog
{
    [ProtoContract]
    public class Index : IDisposable
    {
        private Config config;
        private string indexPath;
        private string unsavedLogsPath;
        private volatile ConcurrentBag<TaggedLog> unsavedLogs;

        public string Name { get; set; }

        [ProtoMember(1)]
        public ConcurrentDictionary<string, Trie> Tries { get; set; }

        [ProtoMember(2)]
        public ConcurrentDictionary<string, Bucket> Buckets { get; set; }

        public DateTime LastUsedTime { get; private set; } = DateTime.Now;

        public Index() { }

        public Index(string name, Config config)
        {
            this.Name = name;
            this.config = config;
            if (!Directory.Exists(config.DataPath))
            {
                Directory.CreateDirectory(config.DataPath);
            }
            var folder = Path.Combine(config.DataPath, name);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            this.indexPath = Path.Combine(folder, "index");
            if (File.Exists(this.indexPath))
            {
                using (var file = File.OpenRead(this.indexPath))
                {
                    var idx = Serializer.Deserialize<Index>(file);
                    this.Tries = idx.Tries;
                    this.Buckets = idx.Buckets;
                }
            }
            else
            {
                this.Tries = new ConcurrentDictionary<string, Trie>();
                this.Buckets = new ConcurrentDictionary<string, Bucket>();
            }

            this.unsavedLogsPath = Path.Combine(folder, "unsaved");
            if (File.Exists(this.unsavedLogsPath))
            {
                using (var file = File.OpenRead(this.unsavedLogsPath))
                {
                    var logs = Serializer.Deserialize<ConcurrentBag<TaggedLog>>(file);
                    this.unsavedLogs = logs;
                }
            }
            else
            {
                this.unsavedLogs = new ConcurrentBag<TaggedLog>();
            }

            if (this.unsavedLogs.Any())
            {
                Task.Run(() =>
                {
                    Log.Information($"index {this.Name} 检测到未保存的历史数据");
                    foreach (var log in this.unsavedLogs)
                    {
                        // 由于数据已存在 unsavedLogs 中，因此不需要调用 Push 方法，否则会导致数据重复
                        this.PushWithNoGuarantee(log.Tags, log.Data);
                    }
                    Log.Information($"index {this.Name} 历史数据保存完毕");
                });
            }
        }

        /// <summary>
        /// 索引数据
        /// <para>该方法能确保数据不会丢失</para>
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="data"></param>
        public void Push(List<Tag> tags, params string[] data)
        {
            foreach (var x in data)
            {
                this.unsavedLogs.Add(new TaggedLog
                {
                    Data = x,
                    Tags = tags
                });
            }
            lock (this)
            {
                using (var file = File.OpenWrite(this.unsavedLogsPath))
                {
                    Serializer.Serialize(file, this.unsavedLogs);
                }
            }

            this.PushWithNoGuarantee(tags, data);
        }

        /// <summary>
        /// 索引数据
        /// <para>调用该方法可能会丢失数据</para>
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="data"></param>
        public void PushWithNoGuarantee(List<Tag> tags, params string[] data)
        {
            this.LastUsedTime = DateTime.Now;

            var bucket = new Bucket(this.Name, tags, this.config);
            bucket.Append(data);
            if (!this.Buckets.ContainsKey(bucket.Key))
            {
                this.Buckets.TryAdd(bucket.Key, bucket);
            }

            foreach (var tag in tags)
            {
                this.Tries.TryGetValue(tag.Label, out var trie);
                if (trie == null)
                {
                    trie = new Trie();
                    this.Tries.TryAdd(tag.Label, trie);
                }

                trie.Append(tag.Value.ToCharArray(), bucket);
            }
        }

        public List<Bucket> GetBuckets(Tag tag = null)
        {
            this.LastUsedTime = DateTime.Now;

            if (tag == null)
            {
                return this.Buckets.Values.ToList();
            }

            if (!this.Tries.TryGetValue(tag.Label, out var trie))
            {
                return null;
            }

            return trie.GetBuckets(tag.Value.ToCharArray());
        }

        public List<string> GetTagValues(string label)
        {
            if (!this.Tries.TryGetValue(label, out var trie))
            {
                return null;
            }

            return trie.GetLeaves();
        }

        public void Save()
        {
            using (var file = File.OpenWrite(this.indexPath))
            {
                Serializer.Serialize(file, this);
            }

            this.unsavedLogs = new ConcurrentBag<TaggedLog>();
            lock (this)
            {
                if (File.Exists(this.unsavedLogsPath))
                {
                    File.Delete(this.unsavedLogsPath);
                }
            }
        }

        public void Dispose()
        {
            this.Save();
        }
    }
}
