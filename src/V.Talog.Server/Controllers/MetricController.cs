using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using V.Talog.Server.Models;

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

        [HttpGet]
        [Route("pg/add")]
        public bool AddPageView([Required] string page, string user = null)
        {
            var indexer = this.taloger.CreateJsonIndexer("pg");
            indexer.Tag("page", page).Tag("date", DateTime.Now.ToString("yyyyMMdd"));
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
    }
}
