using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace V.Talog
{
    public class JsonIndexer : Indexer
    {
        public JsonIndexer(Index index) : base(index)
        {
        }

        public JsonIndexer Data<T>(params T[] ts)
        {
            this.data.AddRange(ts.Select(t => JsonConvert.SerializeObject(t)));
            return this;
        }
    }
}
