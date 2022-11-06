﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using V.Common.Extensions;
using V.Talog.Server.Attributes;
using V.Talog.Server.Models;
using V.User.Attributes;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("setting")]
    public class SettingController : Controller
    {
        private Taloger taloger;

        public SettingController(Taloger taloger)
        {
            this.taloger = taloger;
        }

        [JwtValidation]
        [AdminRole]
        [HttpPost]
        [Route("savequery")]
        public object SaveQuery([FromBody] SaveQueryRequest request)
        {
            var indexer = this.taloger.CreateJsonIndexer("setting");
            indexer.Tag("query", request.Name);
            indexer.Data(request)
                .Save();
            return new
            {
                status = 0,
                msg = string.Empty
            };
        }

        [HttpGet]
        [Route("query")]
        [JwtValidation]
        [AdminRole]
        public object GetQuery(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new
                {
                    status = 0,
                    msg = string.Empty
                };
            }

            var query = new Query("query", name);
            var result = this.taloger.CreateJsonSearcher("setting")
                    .SearchJsonLogs<SaveQueryRequest>(query)
                    ?.LastOrDefault();
            return new
            {
                status = 0,
                msg = string.Empty,
                data = result?.Data
            };
        }

        [HttpGet]
        [Route("query/list")]
        [JwtValidation]
        [AdminRole]
        public object GetQueryNames()
        {
            return new
            {
                status = 0,
                msg = string.Empty,
                data = this.taloger.GetIndex("setting")
                    .GetTagValues("query")
            };
        }

        [HttpPost]
        [Route("query/delete")]
        [JwtValidation]
        [AdminRole]
        public bool DeleteQueryName(string name)
        {
            var buckets = this.taloger.CreateJsonSearcher("setting")
                    .Search(new Query("query", name));
            if (buckets.IsNullOrEmpty())
            {
                return true;
            }

            var index = this.taloger.GetIndex("setting");
            foreach (var bucket in buckets)
            {
                index.RemoveBucket(bucket.Key);
            }
            return true;
        }
    }
}