using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using V.Common.Extensions;
using V.QueryParser;
using V.Talog.Core;

namespace V.Talog
{
    public static class TaloggerExtension
    {
        private static IIndexMapping _indexMapping = null;

        public static void SetIndexMapping(IIndexMapping indexMapping)
        {
            _indexMapping = indexMapping;
        }

        /// <summary>
        /// 创建 HeaderIndexer
        /// </summary>
        /// <param name="talogger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderIndexer CreateHeaderIndexer(this Talogger talogger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderIndexer(head, talogger.GetIndex(index));
        }

        /// <summary>
        /// 创建 HeaderSearcher
        /// </summary>
        /// <param name="talogger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <param name="regex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderSearcher CreateHeaderSearcher(this Talogger talogger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderSearcher(head, talogger.GetIndex(index));
        }

        public static JsonIndexer CreateJsonIndexer(this Talogger talogger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonIndexer(talogger.GetIndex(index));
        }

        public static JsonSearcher CreateJsonSearcher(this Talogger talogger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonSearcher(talogger.GetIndex(index));
        }

        /// <summary>
        /// 查询日志，支持标签排序
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="query"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public static List<TaggedLog> SearchLogs(this Searcher searcher, Query query, string sort)
        {
            var logs = searcher.SearchLogs(query);
            if (logs.IsNullOrEmpty())
            {
                return logs;
            }
            if (string.IsNullOrWhiteSpace(sort))
            {
                throw new ArgumentNullException(nameof(sort));
            }

            return Sort(logs, searcher.GetIndexName(), sort, (x, name) => x.Tags.FirstOrDefault(t => t.Label == name)?.Value);
        }

        /// <summary>
        /// 使用正则表达式匹配日志，并支持基于匹配结果做进一步字段筛选
        /// <para>支持基于标签、正则字段进行排序</para>
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="query">标签查询</param>
        /// <param name="regex">用于匹配日志的正则表达式</param>
        /// <param name="regexQuery">基于正则匹配结果的字段查询</param>
        /// <param name="sort">排序表达式</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static List<ParsedLog> SearchLogs(this Searcher searcher, Query query, string regex, string regexQuery = null, string sort = null)
        {
            var logs = searcher.SearchLogs(query);
            if (logs.IsNullOrEmpty())
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(regex))
            {
                throw new ArgumentNullException(nameof(regex));
            }

            Func<ParsedLog, string, bool, object> getValue = (x, name, fromTag) =>
            {
                if (fromTag)
                {
                    var tag = x.Tags.FirstOrDefault(t => t.Label == name);
                    if (tag != null)
                    {
                        return tag.Value;
                    }
                }
                if (!x.Groups.ContainsKey(name))
                {
                    return null;
                }

                return x.Groups[name];
            };
            var index = searcher.GetIndexName();
            var parsedLogs = logs.SelectParsedLogs(regex);
            if (!string.IsNullOrWhiteSpace(regexQuery))
            {
                var fieldQuery = new QueryExpression(regexQuery);
                parsedLogs = parsedLogs.FindAll(x => fieldQuery.Execute(index, name => getValue(x, name, false)));
            }
            if (!string.IsNullOrWhiteSpace(sort))
            {
                parsedLogs = Sort(parsedLogs, index, sort, (x, name) => getValue(x, name, true));
            }
            return parsedLogs;
        }

        /// <summary>
        /// 搜索 json 日志，并支持基于 json 字段做进一步筛选
        /// <para>支持基于标签、json 字段进行排序</para>
        /// </summary>
        /// <param name="searcher"></param>
        /// <param name="query"></param>
        /// <param name="fieldQuery"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static List<TaggedJsonLog<JObject>> SearchJsonLogs(this JsonSearcher searcher, Query query, string sort = null, string fieldQuery = null)
        {
            var logs = searcher.SearchJsonLogs(query);
            if (logs.IsNullOrEmpty())
            {
                return logs;
            }

            var index = searcher.GetIndexName();
            if (!string.IsNullOrWhiteSpace(fieldQuery))
            {
                var fieldQueryExp = new QueryExpression(fieldQuery);
                logs = logs.FindAll(x => fieldQueryExp.Execute(index, name => GetValue(x.Data, name.Split('.'))?.ToString()));
            }
            if (!string.IsNullOrWhiteSpace(sort))
            {
                logs = Sort(logs, index, sort, (x, name) =>
                {
                    var tag = x.Tags.FirstOrDefault(t => t.Label == name);
                    if (tag != null)
                    {
                        return tag.Value;
                    }

                    return GetValue(x.Data, name.Split('.'))?.ToString();
                });
            }
            return logs;
        }

        public static void RemoveJsonLogs(this JsonSearcher searcher, Query query, string fieldQuery)
        {
            var buckets = searcher.Search(query);
            if (buckets.IsNullOrEmpty())
            {
                return;
            }

            var index = searcher.GetIndexName();
            var fieldQueryExp = new QueryExpression(fieldQuery);
            foreach (var b in buckets)
            {
                var lines = FileManager.ReadAllLines(b.File);
                int length = lines.Length;
                var lineList = lines.ToList();
                for (int i = 0; i < length;)
                {
                    var json = lineList[i].ToObj<JObject>();
                    if (json == null)
                    {
                        continue;
                    }

                    if (!fieldQueryExp.Execute(index, name => GetValue(json, name.Split('.'))?.ToString()))
                    {
                        i++;
                        continue;
                    }

                    lineList.RemoveAt(i);
                    length--;
                }

                FileManager.WriteAllText(b.File, string.Join(Environment.NewLine, lineList) + Environment.NewLine);
            }
        }

        public static void RemoveLogs(this Searcher searcher, Query query, string regex, string regexQuery)
        {
            var buckets = searcher.Search(query);
            if (buckets.IsNullOrEmpty())
            {
                return;
            }

            var index = searcher.GetIndexName();
            var fieldQueryExp = new QueryExpression(regexQuery);
            var reg = LogExtension.GetRegex(regex);
            var names = LogExtension.GetGroupNames(reg);
            foreach (var b in buckets)
            {
                var lines = FileManager.ReadAllLines(b.File);
                int length = lines.Length;
                var lineList = lines.ToList();
                for (int i = 0; i < length;)
                {
                    var match = reg.Match(lineList[i]);
                    if (!match.Success)
                    {
                        i++;
                        continue;
                    }

                    var groups = new Dictionary<string, string>();
                    foreach (var name in names)
                    {
                        groups[name] = match.Groups[name].Value;
                    }

                    if (!fieldQueryExp.Execute(index, name =>
                    {
                        if (!groups.ContainsKey(name))
                        {
                            return null;
                        }

                        return groups[name];
                    }))
                    {
                        i++;
                        continue;
                    }

                    lineList.RemoveAt(i);
                    length--;
                }

                FileManager.WriteAllText(b.File, string.Join(Environment.NewLine, lineList) + Environment.NewLine);
            }
        }

        public static void RemoveLogs(this HeaderSearcher searcher, Query query, string regex, string regexQuery)
        {
            var buckets = searcher.Search(query);
            if (buckets.IsNullOrEmpty())
            {
                return;
            }

            var index = searcher.GetIndexName();
            var fieldQueryExp = new QueryExpression(regexQuery);
            var reg = LogExtension.GetRegex(regex);
            var names = LogExtension.GetGroupNames(reg);
            foreach (var b in buckets)
            {
                var lines = FileManager.ReadAllLines(b.File);
                var text = string.Join(Environment.NewLine, lines);
                int length = lines.Length;
                var log = lines[0];
                for (int i = 1; i < length; i++)
                {
                    var line = lines[i];
                    if (!line.StartsWith(searcher.Head))
                    {
                        log += $"{Environment.NewLine}{line}";
                        continue;
                    }

                    var match = reg.Match(log);
                    if (match.Success)
                    {
                        var groups = new Dictionary<string, string>();
                        foreach (var name in names)
                        {
                            groups[name] = match.Groups[name].Value;
                        }

                        if (fieldQueryExp.Execute(index, name =>
                        {
                            if (!groups.ContainsKey(name))
                            {
                                return null;
                            }

                            return groups[name];
                        }))
                        {
                            text = text.Replace($"{log}{Environment.NewLine}", "");
                        }
                    }

                    log = line.Substring(searcher.Head.Length + 3);
                }

                FileManager.WriteAllText(b.File, text);
            }
        }

        public static IServiceCollection AddTalogger(this IServiceCollection services, Action<Config> config = null, Func<Talogger, IIndexMapping> getMapping = null)
        {
            var talogger = new Talogger();
            if (getMapping != null)
            {
                _indexMapping = getMapping(talogger);
            }
            if (config != null)
            {
                config(talogger.Config);
            }
            services.AddSingleton(talogger);
            return services;
        }

        /// <summary>
        /// 根据查询表达式构建 Query
        /// </summary>
        /// <param name="talogger"></param>
        /// <param name="index"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Query CreateQueryByExpression(this Talogger talogger, string index, string expression)
        {
            var queryExpression = new QueryExpression(expression);
            var idx = talogger.GetIndex(index);
            return BuildQuery(queryExpression, idx);
        }

        private static List<T> Sort<T>(List<T> logs, string index, string sortExp, Func<T, string, object> getValue)
        {
            var orders = sortExp.Split(new string[] { " then " }, StringSplitOptions.RemoveEmptyEntries);
            IOrderedEnumerable<T> sortResult;
            var strs = orders[0].Trim().Split(' ');
            var type = _indexMapping.GetFieldType(index, strs[0]);
            if (strs.Length > 1 && strs[1].ToLower() == "desc")
            {
                sortResult = logs.OrderByDescending(x =>
                {
                    var value = getValue(x, strs[0]);
                    if (value is string str)
                    {
                        value = Converter.FromString(str, type);
                    }
                    return value;
                });
            }
            else
            {
                sortResult = logs.OrderBy(x =>
                {
                    var value = getValue(x, strs[0]);
                    if (value is string str)
                    {
                        value = Converter.FromString(str, type);
                    }
                    return value;
                });
            }
            for (int i = 1; i < orders.Length; i++)
            {
                strs = orders[i].Trim().Split(' ');
                type = _indexMapping.GetFieldType(index, strs[0]);
                if (strs.Length > 1 && strs[1].ToLower() == "desc")
                {
                    sortResult = sortResult.ThenByDescending(x =>
                    {
                        var value = getValue(x, strs[0]);
                        if (value is string str)
                        {
                            value = Converter.FromString(str, type);
                        }
                        return value;
                    });
                }
                else
                {
                    sortResult = sortResult.ThenBy(x =>
                    {
                        var value = getValue(x, strs[0]);
                        if (value is string str)
                        {
                            value = Converter.FromString(str, type);
                        }
                        return value;
                    });
                }
            }

            return sortResult.ToList();
        }

        private static Query BuildQuery(QueryExpression expression, Index index)
        {
            if (expression.Type == QueryType.Base)
            {
                switch (expression.Ope)
                {
                    case Symbol.Eq:
                        return new Query(expression.Key, expression.Value);
                    case Symbol.Neq:
                        return new Query(expression.Key, expression.Value).Not();
                    case Symbol.Gt:
                        return BuildTagQuery(index, expression.Key, expression.Value, (v1, v2) => ((dynamic)v1).CompareTo((dynamic)v2) > 0);
                    case Symbol.Gte:
                        return BuildTagQuery(index, expression.Key, expression.Value, (v1, v2) => ((dynamic)v1).CompareTo((dynamic)v2) >= 0);
                    case Symbol.Lt:
                        return BuildTagQuery(index, expression.Key, expression.Value, (v1, v2) => ((dynamic)v1).CompareTo((dynamic)v2) < 0);
                    case Symbol.Lte:
                        return BuildTagQuery(index, expression.Key, expression.Value, (v1, v2) => ((dynamic)v1).CompareTo((dynamic)v2) <= 0);
                    case Symbol.Like:
                        return BuildTagQuery(index, expression.Key, expression.Value, (v1, v2) => v1.ToString().Contains(v2.ToString()));
                    default:
                        return null;
                }
            }
            else if (expression.Type == QueryType.Compound)
            {
                if (expression.Symbols.IsNullOrEmpty())
                {
                    return null;
                }

                var slen = expression.Symbols.Count;
                var qlen = expression.Queries.Count;
                var queries = expression.Queries.Select(x => BuildQuery(x, index)).ToList();

                // 先处理 and 运算
                for (int i = 0; i < slen; i++)
                {
                    if (expression.Symbols[i] != Symbol.And)
                    {
                        continue;
                    }

                    if (i + 1 < qlen)
                    {
                        if (queries[i] != null)
                        {
                            if (queries[i + 1] != null)
                            {
                                queries[i] = queries[i].And(queries[i + 1]);
                            }
                            else
                            {
                                queries[i] = null;
                            }
                        }
                        for (int j = i + 1; j < qlen - 1; j++)
                        {
                            queries[j] = queries[j + 1];
                        }
                        qlen--;
                        for (int j = i + 1; j < slen - 1; j++)
                        {
                            expression.Symbols[j] = expression.Symbols[j + 1];
                        }
                        slen--;
                        i--;
                    }
                    else
                    {
                        slen = i;
                    }
                }
                if (slen <= 0)
                {
                    return queries[0];
                }

                // 再处理 or 运算
                for (int i = 0; i < slen; i++)
                {
                    if (expression.Symbols[i] != Symbol.Or)
                    {
                        throw new Exception("unexcepted symbol when building, {&&, ||} is valid");
                    }

                    if (queries[i] != null)
                    {
                        if (queries[i + 1] != null)
                        {
                            queries[i + 1] = queries[i].Or(queries[i + 1]);
                        }
                        else
                        {
                            queries[i + 1] = queries[i];
                        }
                    }
                }

                return queries[slen];
            }
            else
            {
                return new Query();
            }
        }

        private static Query BuildTagQuery(Index index, string key, string value, Func<object, object, bool> comparer)
        {
            if (_indexMapping == null)
            {
                throw new ArgumentNullException("IIndexMapping", "Talogger 未配置 IIndexMapping，无法对 Tag 使用 >、>=、<、<=");
            }

            var type = _indexMapping.GetTagType(index.Name, key);
            var val = Converter.FromString(value, type);
            var tagValues = index.GetTagValues(key);
            if (tagValues.IsNullOrEmpty())
            {
                return null;
            }

            Query query = null;
            foreach (var tagValue in tagValues)
            {
                var val2 = Converter.FromString(tagValue, type);
                if (!comparer(val2, val))
                {
                    continue;
                }

                if (query == null)
                {
                    query = new Query(key, tagValue);
                }
                else
                {
                    query = query.Or(new Tag { Label = key, Value = tagValue });
                }
            }
            return query;
        }

        private static JToken GetValue(JToken obj, string[] fields, int index = 0)
        {
            if (obj == null)
            {
                return null;
            }
            if (index == fields.Length - 1)
            {
                return obj[fields[index]];
            }

            return GetValue(obj[fields[index]], fields, index + 1);
        }

        private static bool Execute(this QueryExpression query, string index, Func<string, object> getValue)
        {
            if (query.Type == QueryType.All)
            {
                return true;
            }
            if (_indexMapping == null)
            {
                throw new ArgumentNullException("IIndexMapping", "Talogger 未配置 IIndexMapping，无法使用 RegexQuery、JsonQuery");
            }

            if (query.Type == QueryType.Base)
            {
                var type = _indexMapping.GetFieldType(index, query.Key);
                var value = getValue(query.Key);
                if (value is string str)
                {
                    value = Converter.FromString(str, type);
                }
                var value2 = Converter.FromString(query.Value, type);

                switch (query.Ope)
                {
                    case Symbol.Eq:
                        return value.Equals(value2);
                    case Symbol.Neq:
                        return !value.Equals(value2);
                    case Symbol.Gt:
                        return ((dynamic)value).CompareTo((dynamic)value2) > 0;
                    case Symbol.Gte:
                        return ((dynamic)value).CompareTo((dynamic)value2) >= 0;
                    case Symbol.Lt:
                        return ((dynamic)value).CompareTo((dynamic)value2) < 0;
                    case Symbol.Lte:
                        return ((dynamic)value).CompareTo((dynamic)value2) <= 0;
                    case Symbol.Like:
                        return value.ToString().Contains(value2.ToString());
                    default:
                        return false;
                }
            }
            else
            {
                if (query.Symbols.IsNullOrEmpty())
                {
                    return false;
                }

                var slen = query.Symbols.Count;
                var results = query.Queries.Select(q => q.Execute(index, getValue)).ToList();
                var rlen = results.Count;

                // 先处理 and 运算
                for (int i = 0; i < slen; i++)
                {
                    if (query.Symbols[i] != Symbol.And)
                    {
                        continue;
                    }

                    if (i + 1 < rlen)
                    {
                        if (results[i] && results[i + 1])
                        {
                            results[i] = true;
                        }
                        else
                        {
                            results[i] = false;
                        }

                        for (int j = i + 1; j < rlen - 1; j++)
                        {
                            results[j] = results[j + 1];
                        }
                        rlen--;
                        for (int j = i + 1; j < slen - 1; j++)
                        {
                            query.Symbols[j] = query.Symbols[j + 1];
                        }
                        slen--;
                        i--;
                    }
                    else
                    {
                        slen = i;
                    }
                }
                if (slen <= 0)
                {
                    return results[0];
                }

                // 再处理 or 运算
                for (int i = 0; i < slen - 1; i++)
                {
                    if (query.Symbols[i] != Symbol.Or)
                    {
                        throw new Exception("unexcepted symbol when building, {&&, ||} is valid");
                    }

                    if (results[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
