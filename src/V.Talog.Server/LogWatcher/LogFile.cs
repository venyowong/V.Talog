using Serilog;
using System.IO;

namespace V.Talog.Server.LogWatcher
{
    public class LogFile
    {
        public string Path { get; set; }

        public long LastOffset { get; set; }

        public LogFile() { }

        public LogFile(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// 读取增量数据
        /// </summary>
        /// <returns></returns>
        public (string, long) ReadIncrement()
        {
            var tryTime = 1;
            while (tryTime <= 3)
            {
                try
                {
                    var size = new FileInfo(this.Path).Length;
                    if (size == this.LastOffset)
                    {
                        Log.Debug($"LogFile: {this.Path} 文件大小未发生变化({size})");
                        return (null, 0);
                    }
                    if (size < this.LastOffset)
                    {
                        Log.Warning($"LogFile: {this.Path} 文件变小({this.LastOffset} -> {size})，不读取任何数据，并更新偏移量");
                        this.LastOffset = size;
                        return (null, 0);
                    }

                    using (var fs = new FileStream(this.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        // Seek to the end or other start position
                        reader.BaseStream.Seek(this.LastOffset, SeekOrigin.Begin);
                        // 直接返回数据，不更新偏移量，因为可能数据未写入完毕
                        return (reader.ReadToEnd(), size);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning(e, $"LogFile: 第 {tryTime} 次读取 {this.Path} 时发生错误");
                    if (tryTime < 3)
                    {
                        Thread.Sleep(3000);
                    }
                    
                    tryTime++;
                }
            }

            return (null, 0);
        }
    }
}
