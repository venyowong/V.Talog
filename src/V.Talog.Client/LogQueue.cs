using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace V.Talog.Client
{
    internal class LogQueue
    {
        public string Index { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public ConcurrentQueue<string> Data { get; set; }

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
