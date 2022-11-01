using System.ComponentModel.DataAnnotations;

namespace V.Talog.Server.Models
{
    public class IndexLogRequest
    {
        [Required]
        [MinLength(1)]
        public string Index { get; set; }

        [Required]
        [MinLength(1)]
        public List<Tag> Tags { get; set; }

        [Required]
        [MinLength(1)]
        public string[] Data { get; set; }

        /// <summary>
        /// 0 单行 1 多行
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 多行日志需要设置一个自定义头
        /// <para>若该参数为空，则以 Index 替代</para>
        /// </summary>
        public string Head { get; set; }
    }
}
