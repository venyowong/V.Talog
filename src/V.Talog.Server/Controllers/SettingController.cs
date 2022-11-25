using Microsoft.AspNetCore.Mvc;
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
        private Talogger talogger;

        public SettingController(Talogger talogger)
        {
            this.talogger = talogger;
        }

        [JwtValidation]
        [AdminRole]
        [HttpPost]
        [Route("savequery")]
        public object SaveQuery([FromBody] SaveQueryRequest request)
        {
            var indexer = this.talogger.CreateJsonIndexer("setting");
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
            var result = this.talogger.CreateJsonSearcher("setting")
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
                data = this.talogger.GetIndex("setting")
                    .GetTagValues("query")
            };
        }

        [HttpPost]
        [Route("query/delete")]
        [JwtValidation]
        [AdminRole]
        public bool DeleteQueryName(string name)
        {
            this.talogger.CreateJsonSearcher("setting")
                    .Remove(new Query("query", name));
            return true;
        }
    }
}
