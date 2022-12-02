using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace V.Talog
{
    public class Searcher
    {
        private Index index;

        public Searcher(Index index)
        {
            this.index = index;
        }

        public string GetIndexName() => this.index.Name;

        public List<Bucket> Search(Query query)
        {
            if (query == null)
            {
                return null;
            }
            if (query.Tag != null)
            {
                var buckets = this.index.GetBuckets(query.Tag);
                if (query.Type == 0)
                {
                    return buckets;
                }

                var allBuckets = this.index.GetBuckets();
                if (buckets == null)
                {
                    return allBuckets;
                }

                return allBuckets.Except(buckets).ToList();
            }
            if (query.Left == null && query.Right == null)
            {
                return this.index.GetBuckets();
            }

            var leftBuckets = this.Search(query.Left);
            var rightBuckets = this.Search(query.Right);
            if (query.Type == 2)
            {
                if (leftBuckets == null || rightBuckets == null)
                {
                    return null;
                }

                return leftBuckets.Intersect(rightBuckets).ToList();
            }

            if (leftBuckets == null)
            {
                return rightBuckets;
            }
            if (rightBuckets == null)
            {
                return leftBuckets;
            }

            return leftBuckets.Union(rightBuckets).ToList();
        }

        public void Remove(Query query)
        {
            var buckets = this.Search(query);
            if (buckets == null || !buckets.Any())
            {
                return;
            }

            buckets.ForEach(b => this.index.RemoveBucket(b.Key));
            return;
        }

        public virtual List<TaggedLog> SearchLogs(Query query)
        {
            var buckets = this.Search(query);
            if (buckets == null)
            {
                return null;
            }

            var result = new List<TaggedLog>();
            foreach (var b in buckets)
            {
                var lines = File.ReadAllLines(b.File);
                result.AddRange(lines?.Select(x => new TaggedLog
                {
                    Data = x,
                    Tags = b.Tags
                }).ToList());
            }
            return result;
        }
    }
}
