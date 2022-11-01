﻿using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V.Talog
{
    [ProtoContract]
    public class Trie
    {
        [ProtoMember(1)]
        public char Char { get; set; }

        [ProtoMember(2)]
        public ConcurrentBag<Trie> Nodes { get; set; } = new ConcurrentBag<Trie>();

        [ProtoMember(3)]
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
    }
}
