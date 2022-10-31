using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        private Regex regex;
        private List<string> names;
        private Dictionary<string, Func<string, bool>> funcs = new Dictionary<string, Func<string, bool>>();

        public HeaderSearcher(string head, Index index, string regex = null) : base(index)
        {
            this.head = $"[{head}]";
            this.regex = new Regex(regex);
            this.regex.GetGroupNames()
                .Where(x => !int.TryParse(x, out int _))
                .ToList();
        }

        /// <summary>
        /// 添加过滤器
        /// </summary>
        /// <param name="name">若 name 不在正则表达式中，func 将不起作用</param>
        /// <param name="func"></param>
        /// <returns></returns>
        public HeaderSearcher AddFilter(string name, Func<string, bool> func)
        {
            this.funcs[name] = func;
            return this;
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
                var lines = File.ReadAllLines(b.File);
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
                result.Add(log);
            }
            return result;
        }

        public List<ParsedLog> SearchParsedLogs(Query query)
        {
            var logs = this.SearchLogs(query);
            return logs.Select(x => x.Convert2ParsedLog(this.regex, this.names, this.funcs))
                .Where(x => x != null)
                .ToList();
        }
    }
}
