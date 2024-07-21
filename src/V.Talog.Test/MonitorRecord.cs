using Bogus.DataSets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V.Talog.Mapper.Attributes;

namespace V.Talog.Test
{
    /// <summary>
    /// 监察记录
    /// </summary>
    [Index(Name = "monitor")]
    public class MonitorRecord
    {
        /// <summary>
        /// 监察数据类型，如 bond
        /// </summary>
        [Tag(Name = "type")]
        public string Type { get; set; }

        /// <summary>
        /// 监察数据对象，如 fundcode
        /// </summary>
        [Tag(Name = "target")]
        public string Target { get; set; }

        /// <summary>
        /// 交互结果
        /// </summary>
        public string Interaction { get; set; }

        public string Remark { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
