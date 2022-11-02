using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V.Common.Extensions;
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
        public Result Search([FromBody] SearchLogRequest request)
        {
            var storedIndexSearcher = this.taloger.CreateJsonSearcher("stored_index");
            var query = new Query("name", request.Index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (json == null)
            {
                return new Result { Code = -1, Msg = $"index {request.Index} 不存在，请先插入数据" };
            }

            var tagQuery = this.taloger.CreateQueryByExpression(request.Index, request.TagQuery);
            if (tagQuery == null)
            {
                return new Result { Code = -1, Msg = $"{request.TagQuery} 解析失败，请检查表达式" };
            }

            if (json["type"].ToString() == "1")
            {
                var searcher = this.taloger.CreateHeaderSearcher(request.Index, json["head"].ToString());
                var logs = searcher.SearchLogs(tagQuery);
                return this.HandleRegex(logs, request.Index, request.Regex, request.RegexQuery);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.JsonQuery))
                {
                    var searcher = this.taloger.CreateJsonSearcher(request.Index);
                    var logs = searcher.SearchJsonLogs(query);
                    if (logs.IsNullOrEmpty())
                    {
                        return Result.Success(logs);
                    }

                    Func<TaggedJsonLog<JObject>, bool> filter = null;
                    if (!string.IsNullOrWhiteSpace(request.JsonQuery))
                    {
                        var jsonQuery = new QueryExpression(request.JsonQuery);
                        filter = log =>
                        {
                            return jsonQuery.Execute(request.Index, name =>
                            {
                                if (!log.Data.ContainsKey(name))
                                {
                                    throw new MissingFieldException(name);
                                }

                                return log.Data[name].ToString();
                            });
                        };
                    }

                    return Result.Success(logs.Where(l =>
                    {
                        if (filter == null)
                        {
                            return true;
                        }

                        return filter(l);
                    }).ToList());
                }
                else
                {
                    var searcher = this.taloger.CreateSearcher(request.Index);
                    var logs = searcher.SearchLogs(tagQuery);
                    return this.HandleRegex(logs, request.Index, request.Regex, request.RegexQuery);
                }
            }
        }

        private Result HandleRegex(List<TaggedLog> logs, string index, string regex, string regexQuery)
        {
            if (logs.IsNullOrEmpty())
            {
                return Result.Success(logs);
            }

            if (!string.IsNullOrWhiteSpace(regex))
            {
                Func<ParsedLog, bool> filter = null;
                if (!string.IsNullOrWhiteSpace(regexQuery))
                {
                    var query = new QueryExpression(regexQuery);
                    filter = log =>
                    {
                        return query.Execute(index, name =>
                        {
                            if (!log.Groups.ContainsKey(name))
                            {
                                throw new MissingFieldException(name);
                            }

                            return log.Groups[name];
                        });
                    };
                }

                return Result.Success(logs.SelectParsedLogs(regex, filter));
            }

            return Result.Success(logs);
        }
    }
}
