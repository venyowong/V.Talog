using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class ParsedLog : TaggedLog
    {
        public Dictionary<string, string> Groups { get; set; } = new Dictionary<string, string>();
    }
}
