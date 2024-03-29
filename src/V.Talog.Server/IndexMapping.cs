﻿namespace V.Talog.Server
{
    public class IndexMapping : IIndexMapping
    {
        private Talogger talogger;

        public IndexMapping(Talogger talogger)
        {
            this.talogger = talogger;
        }

        public Type GetFieldType(string index, string field)
        {
            var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
            var query = new Query("field_mapping", index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (json == null || !json.ContainsKey(field))
            {
                throw new MissingFieldException($"index {index} 未配置 {field} 的数据类型");
            }

            return Type.GetType(json[field].ToString());
        }

        public Type GetTagType(string index, string tag)
        {
            var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
            var query = new Query("tag_mapping", index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (json == null || !json.ContainsKey(tag))
            {
                throw new MissingFieldException($"index {index} 未配置 {tag} 的数据类型");
            }

            return Type.GetType(json[tag].ToString());
        }
    }
}
