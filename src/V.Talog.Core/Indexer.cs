using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V.Talog
{
    public class Indexer
    {
        private Index index;
        private List<Tag> tags = new List<Tag>();
        private string data;

        public Indexer(Index index)
        {
            this.index = index;
        }

        public Indexer Tag(string label, string value)
        {
            this.tags.Add(new Tag
            {
                Label = label,
                Value = value
            });
            return this;
        }

        public Indexer Data(string data)
        {
            this.data = data;
            return this;
        }

        public bool Save()
        {
            if (!this.tags.Any())
            {
                throw new Exception("当前索引对象还未添加标签");
            }
            if (string.IsNullOrWhiteSpace(this.data))
            {
                throw new Exception("当前索引对象还未设置 Data");
            }

            return this.index.Push(this.tags, this.data);
        }
    }
}
