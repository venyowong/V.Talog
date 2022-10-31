using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V.Talog
{
    public class JsonSearcher : Searcher
    {
        public JsonSearcher(Index index) : base(index)
        {
        }

        public List<TaggedJsonLog<JObject>> SearchJsonLogs(Query query)
        {
            var logs = this.SearchLogs(query);
            if (logs == null)
            {
                return null;
            }

            return logs.Select(x => new TaggedJsonLog<JObject>
            {
                Tags = x.Tags,
                Data = JsonConvert.DeserializeObject<JObject>(x.Data)
            }).ToList();
        }

        public List<TaggedJsonLog<T>> SearchJsonLogs<T>(Query query)
        {
            var logs = this.SearchLogs(query);
            if (logs == null)
            {
                return null;
            }

            return logs.Select(x => new TaggedJsonLog<T>
            {
                Tags = x.Tags,
                Data = JsonConvert.DeserializeObject<T>(x.Data)
            }).ToList();
        }
    }
}
