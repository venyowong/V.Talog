using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using V.Common.Extensions;
using V.Talog.Server.Attributes;
using V.Talog.Server.Models;
using V.User.Attributes;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("index")]
    public class IndexController : Controller
    {
        private Taloger taloger;

        public IndexController(Taloger taloger)
        {
            this.taloger = taloger;
        }

        [HttpPut]
        [Route("mapping")]
        [JwtValidation]
        [AdminRole]
        public Result UpdateMapping([FromBody] UpdateMappingRequest request)
        {
            if (request.Mapping == null)
            {
                if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.ValueType))
                {
                    return new Result { Code = -1, Msg = "参数不合法" };
                }

                request.Mapping = new Dictionary<string, string>();
                request.Mapping.Add(request.Key, request.ValueType);
            }

            var storedIndexSearcher = this.taloger.CreateJsonSearcher("stored_index");
            var label = "tag_mapping";
            if (request.Type == 1)
            {
                label = "field_mapping";
            }
            var query = new Query(label, request.Index);
            var mapping = storedIndexSearcher.SearchJsonLogs<Dictionary<string, string>>(query)
                ?.Select(x => x.Data)
                .LastOrDefault();
            if (mapping == null)
            {
                mapping = new Dictionary<string, string>();
            }

            foreach (var item in request.Mapping)
            {
                var type = this.GetTypeName(item.Value);
                mapping[item.Key] = type;
            }

            this.taloger.CreateJsonIndexer("stored_index")
                .Tag(label, request.Index)
                .Data(mapping.ToJson())
                .Save();
            return new Result { Msg = "设置成功" };
        }

        [HttpPost]
        [Route("remove")]
        [JwtValidation]
        [AdminRole]
        public Result RemoveIndex(string index)
        {
            this.taloger.CreateSearcher("stored_index")
                .Remove(new Query("name", index));
            this.taloger.RemoveIndex(index);
            return new Result { Msg = "删除成功" };
        }

        [HttpGet]
        [Route("suggest")]
        [JwtValidation]
        [AdminRole]
        public object Suggest()
        {
            return new
            {
                status = 0,
                data = new
                {
                    suggestion = this.taloger.Suggest()
                }
            };
        }

        private string GetTypeName(string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return typeof(string).FullName;
                case "bool":
                    return typeof(bool).FullName;
                case "byte":
                    return typeof(byte).FullName;
                case "char":
                    return typeof(char).FullName;
                case "datetime":
                    return typeof(DateTime).FullName;
                case "decimal":
                    return typeof(decimal).FullName;
                case "double":
                    return typeof(double).FullName;
                case "int16":
                    return typeof(short).FullName;
                case "int":
                    return typeof(int).FullName;
                case "int64":
                    return typeof(long).FullName;
                case "sbyte":
                    return typeof(sbyte).FullName;
                case "float":
                    return typeof(float).FullName;
                case "uint16":
                    return typeof(ushort).FullName;
                case "uint":
                    return typeof(uint).FullName;
                case "uint64":
                    return typeof(ulong).FullName;
                default:
                    throw new Exception($"无法识别数据类型 {type}");
            }
        }
    }
}
