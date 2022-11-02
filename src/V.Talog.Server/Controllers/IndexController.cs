using Microsoft.AspNetCore.Mvc;
using V.Common.Extensions;
using V.Talog.Server.Models;

namespace V.Talog.Server.Controllers
{
    [ApiController]
    [Route("index")]
    public class IndexController
    {
        private Taloger taloger;

        public IndexController(Taloger taloger)
        {
            this.taloger = taloger;
        }

        [HttpPut]
        [Route("mapping")]
        public bool UpdateMapping([FromBody] UpdateMappingRequest request)
        {
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
            return true;
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
