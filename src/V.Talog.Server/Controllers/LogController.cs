using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V.Common.Extensions;
using V.QueryParser;
using V.Talog.Server.Attributes;
using V.Talog.Server.Models;
using V.User.Attributes;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("log")]
    public class LogController
    {
        private Talogger talogger;

        public LogController(Talogger talogger)
        {
            this.talogger = talogger;
        }

        [HttpPost]
        [Route("index")]
        public bool Index([FromBody] IndexLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Head))
            {
                request.Head = request.Index;
            }

            var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
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
                this.talogger.CreateJsonIndexer("stored_index")
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
                indexer = this.talogger.CreateHeaderIndexer(request.Index, request.Head);
            }
            else
            {
                indexer = this.talogger.CreateIndexer(request.Index);
            }

            foreach (var tag in request.Tags)
            {
                indexer.Tag(tag.Key, tag.Value);
            }
            indexer.Data(request.Data)
                .Save();
            return true;
        }

        [HttpPost]
        [Route("search")]
        public Result Search([FromBody] SearchLogRequest request, [FromQuery] int page, [FromQuery] int perPage)
        {
            var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
            var query = new Query("name", request.Index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (json == null)
            {
                return new Result { Code = -1, Msg = $"index {request.Index} 不存在，请先插入数据" };
            }

            var tagQuery = this.talogger.CreateQueryByExpression(request.Index, request.TagQuery);
            if (tagQuery == null)
            {
                return new Result { Code = -1, Msg = $"{request.TagQuery} 解析失败，请检查表达式" };
            }

            if ((!string.IsNullOrWhiteSpace(request.FieldQuery) || !string.IsNullOrWhiteSpace(request.Sort)) && string.IsNullOrWhiteSpace(request.Regex))
            {
                var searcher = this.talogger.CreateJsonSearcher(request.Index);
                var logs = searcher.SearchJsonLogs(tagQuery, request.Sort, request.FieldQuery);
                if (logs.IsNullOrEmpty())
                {
                    return Result.Success(new List<TaggedLog>());
                }

                return Result.Success(new
                {
                    total = logs.Count,
                    items = logs.Skip((page - 1) * perPage)
                        .Take(perPage)
                        .ToList()
                });
            }
            else
            {
                Searcher searcher;
                if (json["type"].ToString() == "1")
                {
                    searcher = this.talogger.CreateHeaderSearcher(request.Index, json["head"].ToString());
                }
                else
                {
                    searcher = this.talogger.CreateSearcher(request.Index);
                }
                if (string.IsNullOrWhiteSpace(request.Regex))
                {
                    List<TaggedLog> logs;
                    if (string.IsNullOrWhiteSpace(request.Sort))
                    {
                        logs = searcher.SearchLogs(tagQuery);
                    }
                    else
                    {
                        logs = searcher.SearchLogs(tagQuery, request.Sort);
                    }

                    if (logs.IsNullOrEmpty())
                    {
                        return Result.Success(new List<TaggedLog>());
                    }

                    return Result.Success(new
                    {
                        total = logs.Count,
                        items = logs.Skip((page - 1) * perPage)
                            .Take(perPage)
                            .ToList()
                    });
                }
                else
                {
                    var logs = searcher.SearchLogs(tagQuery, request.Regex, request.FieldQuery, request.Sort);
                    if (logs.IsNullOrEmpty())
                    {
                        return Result.Success(new List<TaggedLog>());
                    }

                    return Result.Success(new
                    {
                        total = logs.Count,
                        items = logs.Skip((page - 1) * perPage)
                            .Take(perPage)
                            .ToList()
                    });
                }
            }
        }

        [HttpGet]
        [Route("index/list")]
        [JwtValidation]
        [AdminRole]
        public object GetIndexList()
        {
            var indexList = this.talogger.GetIndex("stored_index").GetTagValues("name");
            return new
            {
                status = 0,
                msg = string.Empty,
                data = indexList
            };
        }

        /// <summary>
        /// 删除日志
        /// <para>只有 TagQuery 生效</para>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("remove")]
        [JwtValidation]
        [AdminRole]
        public Result Remove([FromBody] SearchLogRequest request)
        {
            var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
            var query = new Query("name", request.Index);
            var json = storedIndexSearcher.SearchJsonLogs(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (json == null)
            {
                return new Result { Msg = "删除成功" };
            }
            var tagQuery = this.talogger.CreateQueryByExpression(request.Index, request.TagQuery);
            if (tagQuery == null)
            {
                return new Result { Code = -1, Msg = $"{request.TagQuery} 解析失败，请检查表达式" };
            }

            int.TryParse(json["type"].ToString(), out var type);
            if (type == 1)
            {
                var searcher = this.talogger.CreateHeaderSearcher(request.Index);
                if (!string.IsNullOrEmpty(request.Regex) && !string.IsNullOrEmpty(request.FieldQuery))
                {
                    searcher.RemoveLogs(tagQuery, request.Regex, request.FieldQuery);
                }
                else
                {
                    searcher.Remove(tagQuery);
                }
            }
            else 
            {
                if (!string.IsNullOrEmpty(request.Regex))
                {
                    var searcher = this.talogger.CreateSearcher(request.Index);
                    if (!string.IsNullOrEmpty(request.FieldQuery))
                    {
                        searcher.RemoveLogs(tagQuery, request.Regex, request.FieldQuery);
                    }
                    else
                    {
                        searcher.Remove(tagQuery);
                    }
                }
                else
                {
                    var searcher = this.talogger.CreateJsonSearcher(request.Index);
                    if (!string.IsNullOrEmpty(request.FieldQuery))
                    {
                        searcher.RemoveJsonLogs(tagQuery, request.FieldQuery);
                    }
                    else
                    {
                        searcher.Remove(tagQuery);
                    }
                }
            }
            return new Result { Msg = "删除成功" };
        }
    }
}
