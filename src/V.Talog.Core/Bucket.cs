using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace V.Talog
{
    public class Bucket
    {
        public string Index { get; set; }

        public string Key { get; set; }

        public List<Tag> Tags { get; set; }

        public string File { get; set; }

        private Mutex mutex;

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
            if (this.mutex == null)
            {
                this.mutex = new Mutex(false, $"Bucket_{this.Index}_{this.Key}");
            }

            this.mutex.WaitOne();
            System.IO.File.AppendAllLines(this.File, data);
            this.mutex.ReleaseMutex();
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
