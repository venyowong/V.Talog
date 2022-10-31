using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    /// <summary>
    /// 使用自定义字符串作为数据头部的索引器
    /// <para>可结合 HeaderSearcher 用于记录多行日志</para>
    /// <para>将以 [{head}] {data} 的格式记录日志</para>
    /// </summary>
    public class HeaderIndexer : Indexer
    {
        private string head;

        public HeaderIndexer(string head, Index index) : base(index)
        {
            this.head = $"[{head}]";
        }

        public override Indexer Data(string data)
        {
            this.data = $"{this.head} {data}";
            return this;
        }
    }
}
