using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public void Append(params string[] data)
        {
            System.IO.File.AppendAllLines(this.File, data);
        }

        public override bool Equals(object obj)
        {
            if (obj is Bucket b)
            {
                return this.Index == b.Index && this.Key == b.Key;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return $"{this.Index}{this.Key}".GetHashCode();
        }
    }
}
