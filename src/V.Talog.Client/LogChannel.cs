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
            if (tags != null)
            {
                this.tags = tags;
            }
            else
            {
                this.tags = new Dictionary<string, string>();
            }
            this.type = type;
            if (head == null)
            {
                this.head = index;
            }
            else
            {
                this.head = head;
            }
        }

        public void Send(string log)
        {
            LogSender.Enqueue(this.index, this.tags, log, this.type, this.head);
        }

        public TaggedLog Info()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "info")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Debug()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "debug")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Trace()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "trace")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Warn()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "warn")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Error()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "error")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Fatal()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head)
                .Tag("level", "fatal")
                .Tag("date", DateTime.Now.ToString("yyyyMMdd"));
        }

        public TaggedLog Create()
        {
            var tags = new Dictionary<string, string>();
            foreach (var tag in this.tags)
            {
                tags.Add(tag.Key, tag.Value);
            }
            return new TaggedLog(this.index, tags, this.type, this.head);
        }
    }
}
