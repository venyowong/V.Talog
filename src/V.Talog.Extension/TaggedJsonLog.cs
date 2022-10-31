using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class TaggedJsonLog<T>
    {
        public List<Tag> Tags { get; set; }

        public T Data { get; set; }
    }
}
