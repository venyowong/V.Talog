using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog.Client
{
    public class TaggedLog
    {
        private string index;
        private Dictionary<string, string> tags;
        private int type;
        private string head;
        private string log;

        public TaggedLog(string index, Dictionary<string, string> tags, int type, string head)
        {
            this.index = index;
            this.tags = tags;
            this.type = type;
            this.head = head;
        }

        public TaggedLog Tag(string label, string value)
        {
            this.tags[label] = value;
            return this;
        }

        public TaggedLog Log(string log)
        {
            this.log = log;
            return this;
        }

        public void Send()
        {
            LogSender.Enqueue(this.index, this.tags, this.log, this.type, this.head);
        }
    }
}
