using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class JsonIndexer : Indexer
    {
        public JsonIndexer(Index index) : base(index)
        {
        }

        /// <summary>
        /// 请使用 .Data(T t) 方法
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override Indexer Data(string data)
        {
            throw new NotImplementedException();
        }

        public JsonIndexer Data<T>(T t)
        {
            this.data = JsonConvert.SerializeObject(t);
            return this;
        }
    }
}
