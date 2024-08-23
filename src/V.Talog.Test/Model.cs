using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V.Talog.Mapper;
using V.Talog.Mapper.Attributes;

namespace V.Talog.Test
{
    [Index(Name = "model")]
    internal class Model : IVersioned
    {
        [Tag(Name = "id")]
        public string Id { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}
