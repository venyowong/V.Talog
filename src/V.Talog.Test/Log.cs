using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V.Talog.Test
{
    internal class Log
    {
        public DateTime Time { get; set; }

        public int Level { get; set; }

        public string IP { get; set; }

        public string UserId { get; set; }

        public string Message { get; set; }
    }
}
