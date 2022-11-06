namespace V.Talog.Server.Models
{
    public class LogFileConfig
    {
        /// <summary>
        /// 文件夹地址
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 文件过滤器
        /// <para></para>
        /// </summary>
        public string Filter { get; set; }

        public string Regex { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public string Index { get; set; }

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
