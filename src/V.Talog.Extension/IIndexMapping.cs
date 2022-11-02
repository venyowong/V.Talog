using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public interface IIndexMapping
    {
        Type GetTagType(string index, string tag);

        /// <summary>
        /// 获取字段类型
        /// <para>只针对 RegexSearcher、JsonSearcher 使用</para>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="field">正则参数或 json 参数</param>
        /// <returns></returns>
        Type GetFieldType(string index, string field);
    }
}
