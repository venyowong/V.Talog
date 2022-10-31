﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    [ProtoContract]
    public class Tag
    {
        [ProtoMember(1)]
        public string Label { get; set; }

        [ProtoMember(2)]
        public string Value { get; set; }
    }
}
