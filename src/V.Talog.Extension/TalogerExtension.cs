using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using V.Common.Extensions;
using V.QueryParser;

namespace V.Talog
{
    public static class TalogerExtension
    {
        private static IIndexMapping _indexMapping = null;

        /// <summary>
        /// 创建 HeaderIndexer
        /// </summary>
        /// <param name="taloger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderIndexer CreateHeaderIndexer(this Taloger taloger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderIndexer(head, taloger.GetIndex(index));
        }

        /// <summary>
        /// 创建 HeaderSearcher
        /// </summary>
        /// <param name="taloger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <param name="regex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderSearcher CreateHeaderSearcher(this Taloger taloger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderSearcher(head, taloger.GetIndex(index));
        }

        public static JsonIndexer CreateJsonIndexer(this Taloger taloger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonIndexer(taloger.GetIndex(index));
        }

        public static JsonSearcher CreateJsonSearcher(this Taloger taloger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonSearcher(taloger.GetIndex(index));
        }

        public static IServiceCollection AddTaloger(this IServiceCollection services, Action<Config> config = null, Func<Taloger, IIndexMapping> getMapping = null)
        {
            var taloger = new Taloger();
            _indexMapping = getMapping(taloger);
            if (config != null)
            {
                config(taloger.Config);
            }
            services.AddSingleton(taloger);
            return services;
        }

        public static bool Execute(this QueryExpression query, string index, Func<string, object> getValue)
        {
            if (_indexMapping == null)
            {
                throw new ArgumentNullException("IIndexMapping", "Taloger 未配置 IIndexMapping，无法使用 RegexQuery、JsonQuery");
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

        /// <summary>
        /// 根据查询表达式构建 Query
        /// </summary>
        /// <param name="taloger"></param>
        /// <param name="index"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Query CreateQueryByExpression(this Taloger taloger, string index, string expression)
        {
            var queryExpression = new QueryExpression(expression);
            var idx = taloger.GetIndex(index);
            return BuildQuery(queryExpression, idx);
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
                    default:
                        return null;
                }
            }
            else
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
                        queries[i] = queries[i].And(queries[i + 1]);
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

                // 再处理 or 运算
                for (int i = 0; i < slen - 1; i++)
                {
                    if (expression.Symbols[i] != Symbol.Or)
                    {
                        throw new Exception("unexcepted symbol when building, {&&, ||} is valid");
                    }

                    queries[i + 1] = queries[i].Or(queries[i + 1]);
                }

                return queries[slen - 1];
            }
        }

        private static Query BuildTagQuery(Index index, string key, string value, Func<object, object, bool> comparer)
        {
            if (_indexMapping == null)
            {
                throw new ArgumentNullException("IIndexMapping", "Taloger 未配置 IIndexMapping，无法对 Tag 使用 >、>=、<、<=");
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
                if (!comparer(val, val2))
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
    }
}
