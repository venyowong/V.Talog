using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using V.Talog.Mapper.Attributes;

namespace V.Talog.Test
{
    [Index(Name = "log")]
    internal class Log
    {
        [Tag(Name = "date", Format = "yyyy-MM-dd")]
        public DateTime Time { get; set; }

        [Tag(Name = "level")]
        public int Level { get; set; }

        public string IP { get; set; }

        public string UserId { get; set; }

        public string Message { get; set; }
    }
}
