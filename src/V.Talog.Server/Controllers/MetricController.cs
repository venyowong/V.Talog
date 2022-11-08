using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Reflection.Emit;
using V.Common.Extensions;
using V.Talog.Server.Attributes;
using V.Talog.Server.Models;
using V.User.Attributes;
using V.User.Models;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("metric")]
    public class MetricController : Controller
    {
        private Taloger taloger;

        public MetricController(Taloger taloger)
        {
            this.taloger = taloger;
        }

        /// <summary>
        /// 添加页面访问记录
        /// </summary>
        /// <param name="index"></param>
        /// <param name="page"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("pg/add")]
        public bool AddPageView([Required] string index, [Required] string page, string user = null)
        {
            var indexer = this.taloger.CreateJsonIndexer("pg");
            indexer.Tag("index", index)
                .Tag("page", page)
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
            indexer.Data(new PageView
            {
                UserAgent = this.HttpContext.Request.Headers.UserAgent,
                IP = this.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Page = page,
                Time = DateTime.Now,
                User = user
            })
            .Save();
            return true;
        }

        /// <summary>
        /// 获取 index 列表
        /// </summary>
        /// <param name="type">0 PageView 1 Metric</param>
        /// <returns></returns>
        [HttpGet]
        [Route("index/list")]
        [JwtValidation]
        [AdminRole]
        public object GetIndexList(int type)
        {
            var index = "pg";
            if (type == 1)
            {
                index = "metric";
            }
            var result = this.taloger.GetIndex(index).GetTagValues("index");
            return new
            {
                status = 0,
                msg = string.Empty,
                data = result
            };
        }

        [HttpGet]
        [Route("pg/pages")]
        [JwtValidation]
        [AdminRole]
        public object GetPages(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = new Query("index", index);
            var result = this.taloger.CreateJsonSearcher("pg")
                .Search(query)
                ?.Select(b => b.Tags.FirstOrDefault(t => t.Label == "page"))
                .Where(t => t != null)
                .Select(t => t.Value)
                .Distinct();
            return new
            {
                status = 0,
                msg = string.Empty,
                data = result
            };
        }

        [HttpGet]
        [Route("pg/latest")]
        [JwtValidation]
        [AdminRole]
        public object GetLatestView([FromQuery] string index, [FromQuery] string page)
        {
            if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(page))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = new Query("index", index)
                .And("page", page);
            var logs = this.taloger.CreateJsonSearcher("pg")
                .SearchJsonLogs<PageView>(query);
            if (logs.IsNullOrEmpty())
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var gs = logs.Select(x => x.Data)
                .GroupBy(x => x.Time.Date)
                .OrderBy(x => x.Key)
                .ToList();
            var latestDate = gs.Last().Key.ToString("yyyy-MM-dd");
            var latestPV = gs.Last().Count();
            var latestUV = gs.Last().Where(x => !string.IsNullOrWhiteSpace(x.User))
                .Select(x => x.User)
                .Distinct()
                .Count();
            return new
            {
                status = 0,
                msg = string.Empty,
                data = new
                {
                    latestDate,
                    latestPV,
                    latestUV
                }
            };
        }

        [HttpGet]
        [Route("pg/sparkline")]
        [JwtValidation]
        [AdminRole]
        public object GetPgSparkline([FromQuery] string index, [FromQuery] string page, [FromQuery] DateTime begin, [FromQuery] DateTime end)
        {
            if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(page))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = this.taloger.CreateQueryByExpression("pg", $"index == {index} && page == {page} && date >= {begin.ToString("yyyyMMdd")} && date <= {end.ToString("yyyyMMdd")}");
            var logs = this.taloger.CreateJsonSearcher("pg")
                ?.SearchJsonLogs<PageView>(query);
            if (logs.IsNullOrEmpty())
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var gs = logs.Select(x => x.Data)
                .GroupBy(x => x.Time.Date)
                .OrderBy(x => x.Key)
                .ToList();
            var pvLine = gs.Select(x => x.Count()).ToList();
            var uvLine = gs.Select(x => x.Where(v => !string.IsNullOrWhiteSpace(v.User))
                .Select(v => v.User)
                .Distinct()
                .Count()).ToList();
            return new
            {
                status = 0,
                msg = string.Empty,
                data = new
                {
                    dates = gs.Select(x => x.Key.ToString("yyyyMMdd")).ToList(),
                    pvLine,
                    uvLine
                }
            };
        }

        /// <summary>
        /// 记录 index 当前时刻的 metric
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("add")]
        public bool AddMetric([Required] string index, [Required] string name, decimal value)
        {
            var indexer = this.taloger.CreateJsonIndexer("metric");
            indexer.Tag("index", index)
                .Tag("name", name)
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
            indexer.Data(new Metric
            {
                Name = name,
                Time = DateTime.Now,
                Value = value
            }).Save();
            return true;
        }

        [HttpGet]
        [Route("names")]
        [JwtValidation]
        [AdminRole]
        public object GetNames(string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = new Query("index", index);
            var result = this.taloger.CreateJsonSearcher("metric")
                .Search(query)
                ?.Select(b => b.Tags.FirstOrDefault(t => t.Label == "name"))
                .Where(t => t != null)
                .Select(t => t.Value)
                .Distinct();
            return new
            {
                status = 0,
                msg = string.Empty,
                data = result
            };
        }

        [HttpGet]
        [Route("sparkline")]
        [JwtValidation]
        [AdminRole]
        public object GetMetricSparkline(string index, string name, DateTime begin, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(index) || string.IsNullOrWhiteSpace(name))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = this.taloger.CreateQueryByExpression("metric", $"index == {index} && name == {name} && date >= {begin.ToString("yyyyMMdd")} && date <= {end.ToString("yyyyMMdd")}");
            var logs = this.taloger.CreateJsonSearcher("metric")
                ?.SearchJsonLogs<Metric>(query);
            if (logs.IsNullOrEmpty())
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var beginTime = logs.Min(x => x.Data.Time);
            beginTime = DateTime.Parse(beginTime.ToString("yyyy-MM-dd HH:mm"));
            var endTime = logs.Max(x => x.Data.Time);
            endTime = DateTime.Parse(endTime.AddMinutes(1).ToString("yyyy-MM-dd HH:mm"));
            var span = (endTime - beginTime).TotalMinutes;
            var step = Math.Floor(span / 100);
            if (step < 1)
            {
                step = 1;
            }
            var time = beginTime;
            var times = new List<string>();
            var line = new List<decimal>();
            while (time <= endTime)
            {
                var nextTime = time.AddMinutes(step);
                var logList = logs.Where(x => x.Data.Time >= time && x.Data.Time < nextTime).ToList();
                if (logList.Any())
                {
                    times.Add(time.ToString("yyyy-MM-dd HH:mm"));
                    line.Add(logList.Average(x => x.Data.Value));
                }

                time = nextTime;
            }

            return new
            {
                status = 0,
                msg = string.Empty,
                data = new
                {
                    times,
                    line
                }
            };
        }
    }
}
