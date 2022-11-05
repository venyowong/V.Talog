using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace V.Talog
{
    public class Trie
    {
        public char Char { get; set; }

        public ConcurrentBag<Trie> Nodes { get; set; } = new ConcurrentBag<Trie>();

        public ConcurrentDictionary<string, Bucket> Buckets { get; set; } = new ConcurrentDictionary<string, Bucket>();

        public Trie() { }

        public void Append(char[] chars, Bucket bucket, int i = 0)
        {
            if (i >= chars.Length)
            {
                if (!this.Buckets.ContainsKey(bucket.Key))
                {
                    this.Buckets.TryAdd(bucket.Key, bucket);
                }
                return;
            }

            var ch = chars[i];
            var node = this.Nodes.FirstOrDefault(n => n.Char == ch);
            if (node == null)
            {
                node = new Trie
                {
                    Char = ch
                };
                this.Nodes.Add(node);
            }
            node.Append(chars, bucket, i + 1);
        }

        public List<Bucket> GetBuckets(char[] chars, int i = 0)
        {
            if (i >= chars.Length)
            {
                return this.Buckets.Values.ToList();
            }
                
            var ch = chars[i];
            var node = this.Nodes.FirstOrDefault(n => n.Char == ch);
            if (node == null)
            {
                return null;
            }

            return node.GetBuckets(chars, i + 1);
        }

        public List<string> GetLeaves()
        {
            var result = new List<string>();
            if (this.Buckets.Any() && this.Char != default)
            {
                result.Add(this.Char.ToString());
            }
            foreach (var node in this.Nodes)
            {
                if (this.Char != default)
                {
                    result.AddRange(node.GetLeaves().Select(x => this.Char + x));
                }
                else
                {
                    result.AddRange(node.GetLeaves());
                }
            }
            return result;
        }

        public bool RemoveBucket(char[] chars, string key, int i = 0)
        {
            if (i >= chars.Length)
            {
                return this.Buckets.TryRemove(key, out var bucket);
            }

            var ch = chars[i];
            var node = this.Nodes.FirstOrDefault(n => n.Char == ch);
            if (node == null)
            {
                return false;
            }

            return node.RemoveBucket(chars, key, i + 1);
        }
    }
}
