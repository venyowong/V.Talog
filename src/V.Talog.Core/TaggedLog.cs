using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    [ProtoContract]
    public class TaggedLog
    {
        [ProtoMember(1)]
        public string Data { get; set; }

        [ProtoMember(2)]
        public List<Tag> Tags { get; set; }
    }
}
