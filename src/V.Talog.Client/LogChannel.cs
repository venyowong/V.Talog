using System;
using System.Collections.Generic;

namespace V.Talog.Client
{
    public class LogChannel
    {
        private string index;
        private Dictionary<string, string> tags;
        private int type;
        private string head;

        /// <summary>
        /// 初始化日志管道
        /// </summary>
        /// <param name="index"></param>
        /// <param name="tags"></param>
        /// <param name="type">0 单行 1 多行</param>
        /// <param name="head">多行日志需要设置一个自定义头 若该参数为空，则以 Index 替代</param>
        public LogChannel(string index, Dictionary<string, string> tags = null, int type = 0, string head = null)
        {
            this.index = index;
            this.tags = tags ?? new Dictionary<string, string>();
            this.type = type;
            this.head = head ?? index;
        }

        public void Send(string log) => LogSender.Enqueue(this.index, this.tags, log, this.type, this.head);

        public TaggedLog Info() => this.Create("info");

        public TaggedLog Debug() => this.Create("debug");

        public TaggedLog Trace() => this.Create("trace");

        public TaggedLog Warn() => this.Create("warn");

        public TaggedLog Error() => this.Create("error");

        public TaggedLog Fatal() => this.Create("fatal");

        public TaggedLog Create()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head);
        }

        private TaggedLog Create(string level)
        {
            var log = this.Create();
            return log.Tag("level", level)
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }
    }
}
