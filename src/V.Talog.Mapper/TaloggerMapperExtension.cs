using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using V.Common.Extensions;

namespace V.Talog.Mapper
{
    public static class TaloggerMapperExtension
    {
        public static void Save(this Talogger talogger, object data)
        {
            Save(talogger, data.GetType(), data);
        }

        public static void Save<T>(this Talogger talogger, T t)
        {
            Save(talogger, typeof(T), t);
        }

        /// <summary>
        /// 由于对于标签字段的查询是特殊的，因此需要把标签查询以及字段查询独立开来
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="talogger"></param>
        /// <param name="tagQuery">标签查询</param>
        /// <param name="fieldQuery">字段查询</param>
        /// <param name="sort">排序 field desc</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<T> Query<T>(this Talogger talogger, string tagQuery, string fieldQuery, string sort = null)
        {
            var type = typeof(T);
            var mapper = new TypeMapper();
            var info = mapper.GetInfo(type) ?? throw new Exception($"{type.Name} 必须使用 IndexAttribute 标注才可调用该方法");
            var searcher = talogger.CreateJsonSearcher(info.IndexName);
            var logs = searcher.SearchJsonLogs(talogger.CreateQueryByExpression(info.IndexName, tagQuery), fieldQuery: fieldQuery, sort: sort);
            if (logs == null)
            {
                return new List<T>();
            }

            return logs.Select(l => l.Data.ToObject<T>()).ToList();
        }

        /// <summary>
        /// 查询最新版本的数据
        /// </summary>
        /// <returns></returns>
        public static List<T> QueryLatest<T>(this Talogger talogger, string tagQuery, string fieldQuery, string sort = null) where T : IVersioned
        {
            var list = talogger.Query<T>(tagQuery, fieldQuery, sort);
            return list.GroupBy(x => x.Id)
                .Select(x => x.OrderByDescending(item => item.UpdateTime).First())
                .Where(x => x.IsValid)
                .ToList();
        }

        public static void Remove<T>(this Talogger talogger, string tagQuery, string fieldQuery)
        {
            var type = typeof(T);
            var mapper = new TypeMapper();
            var info = mapper.GetInfo(type) ?? throw new Exception($"{type.Name} 必须使用 IndexAttribute 标注才可调用该方法");
            var searcher = talogger.CreateJsonSearcher(info.IndexName);
            searcher.RemoveJsonLogs(talogger.CreateQueryByExpression(info.IndexName, tagQuery), fieldQuery);
        }

        private static void Save(Talogger talogger, Type type, object data)
        {
            var mapper = new TypeMapper();
            var info = mapper.GetInfo(type) ?? throw new Exception($"{type.Name} 必须使用 IndexAttribute 标注才可调用该方法");
            var indexer = talogger.CreateJsonIndexer(info.IndexName);
            foreach (var tag in info.TagTypes)
            {
                string name = tag.Value.OriginalName;
                var val = type.GetProperty(name)?.GetValue(data) ?? type.GetField(name)?.GetValue(data);
                if (val != null)
                {
                    indexer.Tag(tag.Key, val.ToString());
                }
            }
            indexer.Data(data)
                .Save();
        }
    }
}