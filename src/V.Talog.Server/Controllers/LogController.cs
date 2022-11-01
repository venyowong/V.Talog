using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using V.QueryParser;
using V.Talog.Server.Models;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("log")]
    public class LogController
    {
        private Taloger taloger;

        public LogController(Taloger taloger)
        {
            this.taloger = taloger;
        }

        [HttpPost]
        [Route("index")]
        public bool Index([FromBody] IndexLogRequest request)
        {
            var storedIndexSearcher = this.taloger.CreateJsonSearcher("stored_index");
            var query = new Query("name", request.Index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .FirstOrDefault();
            if (json != null) // 若 index 已存在，则使用已有配置，防止出现同一个 index 下存储的数据格式不一致的情况
            {
                if (int.TryParse(json["type"].ToString(), out var type))
                {
                    request.Type = type;
                    if (request.Type == 1)
                    {
                        request.Head = json["head"].ToString();
                    }
                }
            }
            else
            {
                this.taloger.CreateJsonIndexer("stored_index")
                    .Tag("name", request.Index)
                    .Data(JsonConvert.SerializeObject(new
                    {
                        index = request.Index,
                        type = request.Type,
                        head = request.Head
                    }))
                    .Save();
            }

            Indexer indexer;
            if (request.Type == 1)
            {
                indexer = this.taloger.CreateHeaderIndexer(request.Index, request.Head);
            }
            else
            {
                indexer = this.taloger.CreateIndexer(request.Index);
            }

            request.Tags.ForEach(t => indexer.Tag(t.Label, t.Value));
            indexer.Data(request.Data)
                .Save();
            return true;
        }

        [HttpPost]
        [Route("search")]
        public void Search([FromBody] SearchLogRequest request)
        {
            var storedIndexSearcher = this.taloger.CreateJsonSearcher("stored_index");
            var query = new Query("name", request.Index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .FirstOrDefault();
            if (json == null)
            {
                return;
            }

            Searcher searcher;
            if (json["type"].ToString() == "1")
            {
                searcher = this.taloger.CreateHeaderSearcher(request.Index, json["head"].ToString(), request.Regex);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.JsonQuery))
                {
                    searcher = this.taloger.CreateJsonSearcher(request.Index);
                }
                else if (!string.IsNullOrWhiteSpace(request.Regex))
                {
                    searcher = this.taloger.CreateRegexSearcher(request.Index, request.Regex);
                }
                else
                {
                    searcher = this.taloger.CreateSearcher(request.Index);
                }
            }
        }
    }
}
