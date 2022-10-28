using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public class Index
    {
        private string name;
        private Config config;

        public Index(string name, Config config)
        {
            this.name = name;
            this.config = config;
        }

        public bool Push(List<Tag> tags, string data)
        {

        }
    }
}
