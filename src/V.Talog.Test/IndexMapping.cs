using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V.Talog.Test
{
    internal class IndexMapping : IIndexMapping
    {
        public Type GetFieldType(string index, string field)
        {
            return typeof(string);
        }

        public Type GetTagType(string index, string tag)
        {
            return typeof(string);
        }
    }
}
