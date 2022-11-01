using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class SearchLogRequest
    {
        [Required]
        [MinLength(1)]
        public string Index { get; set; }

        [Required]
        [MinLength(1)]
        public string TagQuery { get; set; }

        /// <summary>
        /// 正则表达式
        /// <para>若该参数不为空，则只会返回与之匹配的日志</para>
        /// </summary>
        public string Regex { get; set; }

        public string RegexQuery { get; set; }

        public string JsonQuery { get; set; }
    }
}
