using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using V.Talog.Core;

namespace V.Talog
{
    public class Bucket
    {
        public string Index { get; set; }

        public string Key { get; set; }

        public List<Tag> Tags { get; set; }

        public string File { get; set; }

        public Bucket() { }

        public Bucket(string index, List<Tag> tags, Config config)
        {
            this.Index = index;
            this.Tags = tags;
            this.Key = string.Join(";", tags.OrderBy(t => t.Label).Select(t => $"{t.Label}:{t.Value}")).Md5();
            var folder = Path.Combine(config.DataPath, index);
            this.File = Path.Combine(folder, this.Key + ".log");
        }

        public void Append(params string[] data) => FileManager.AppendText(this.File, data);

        public override bool Equals(object obj) => obj is Bucket b ? 
            this.Index == b.Index && this.Key == b.Key : 
            base.Equals(obj);

        public override int GetHashCode()
        {
            return $"{this.Index}{this.Key}".GetHashCode();
        }
    }
}
