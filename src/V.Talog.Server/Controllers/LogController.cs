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

            if (json["type"].ToString() == "1")
            {
                var searcher = this.talogger.CreateHeaderSearcher(request.Index, json["head"].ToString());
                var logs = searcher.SearchLogs(tagQuery);
                return this.HandleRegex(logs, request.Index, request.Regex, request.FieldQuery, page, perPage, request.Sort);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.FieldQuery) && string.IsNullOrWhiteSpace(request.Regex))
                {
                    var searcher = this.talogger.CreateJsonSearcher(request.Index);
                    var logs = searcher.SearchJsonLogs(tagQuery);
                    if (logs.IsNullOrEmpty())
                    {
                        return Result.Success(logs);
                    }

                    var jsonQuery = new QueryExpression(request.FieldQuery);
                    Func<TaggedJsonLog<JObject>, bool> filter = log =>
                    {
                        return jsonQuery.Execute(request.Index, name =>
                        {
                            return this.GetValue(log.Data, name.Split('.'))?.ToString();
                        });
                    };

                    logs = logs.Where(l =>
                    {
                        if (filter == null)
                        {
                            return true;
                        }

                        return filter(l);
                    }).ToList();

                    logs = logs.Sort(request.Index, request.Sort, (x, key) => this.GetValue(x.Data, key.Split('.'))?.ToString());

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
                    var searcher = this.talogger.CreateSearcher(request.Index);
                    var logs = searcher.SearchLogs(tagQuery);
                    return this.HandleRegex(logs, request.Index, request.Regex, request.FieldQuery, page, perPage, request.Sort);
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

            this.talogger.CreateSearcher(request.Index)
                .Remove(tagQuery);
            return new Result { Msg = "删除成功" };
        }

        private Result HandleRegex(List<TaggedLog> logs, string index, string regex, string regexQuery, int page, int perPage, string sortExp)
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
                                return null;
                            }

                            return log.Groups[name];
                        });
                    };
                }

                var parsedLogs = logs.SelectParsedLogs(regex, filter);
                parsedLogs = parsedLogs.Sort(index, sortExp, (x, key) => x.Groups[key]);

                return Result.Success(new
                {
                    total = parsedLogs.Count,
                    items = parsedLogs.Skip((page - 1) * perPage)
                        .Take(perPage)
                        .ToList()
                });
            }

            return Result.Success(new
            {
                total = logs.Count,
                items = logs.Skip((page - 1) * perPage)
                        .Take(perPage)
                        .ToList()
            });
        }

        private JToken GetValue(JToken obj, string[] fields, int index = 0)
        {
            if (obj == null)
            {
                return null;
            }
            if (index == fields.Length - 1)
            {
                return obj[fields[index]];
            }

            return this.GetValue(obj[fields[index]], fields, index + 1);
        }
    }
}
