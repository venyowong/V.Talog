using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class SearchLogRequest
    {
        [Required]
        [MinLength(1)]
        public string Index { get; set; }

        public string TagQuery { get; set; }

        /// <summary>
        /// 正则表达式
        /// <para>若该参数不为空，则只会返回与之匹配的日志</para>
        /// </summary>
        public string Regex { get; set; }

        /// <summary>
        /// 字段筛选
        /// <para>若 Regex 为空，则认为数据是 Json 格式</para>
        /// </summary>
        public string FieldQuery { get; set; }

        /// <summary>
        /// 排序规则
        /// <para>type asc then date desc then ...</para>
        /// </summary>
        public string Sort { get; set; }
    }
}
