using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using V.Talog.Core;

namespace V.Talog
{
    /// <summary>
    /// 使用自定义字符串作为数据头部的搜索器
    /// <para>可结合 HeaderIndexer 用于记录多行日志</para>
    /// <para>将以 [{index}] 为开头的数据认为是新的一行，因此需要确保 head 正确，否则整个文件的数据会被认为是同一行数据</para>
    /// </summary>
    public class HeaderSearcher : Searcher
    {
        private string head;

        public HeaderSearcher(string head, Index index) : base(index)
        {
            this.head = $"[{head}]";
        }

        public override List<TaggedLog> SearchLogs(Query query)
        {
            var buckets = this.Search(query);
            if (buckets == null)
            {
                return null;
            }

            var result = new List<TaggedLog>();
            foreach (var b in buckets)
            {
                var lines = FileManager.ReadAllLines(b.File);
                if (!lines.Any())
                {
                    continue;
                }

                var log = new TaggedLog
                {
                    Data = lines[0],
                    Tags = b.Tags
                };
                for (var i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith(this.head))
                    {
                        log.Data = log.Data.Substring(this.head.Length + 3);
                        result.Add(log);

                        log = new TaggedLog
                        {
                            Data = line,
                            Tags = b.Tags
                        };
                    }
                    else
                    {
                        log.Data += $"\n{line}";
                    }
                }

                log.Data = log.Data.Substring(this.head.Length + 3);
                result.Add(log);
            }
            return result;
        }
    }
}
