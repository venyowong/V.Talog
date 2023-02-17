using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace V.Talog.Core
{
    public static class FileManager
    {
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _locks = new ConcurrentDictionary<string, ReaderWriterLockSlim>();

        private static readonly ConcurrentDictionary<string, FileStream> _streams = new ConcurrentDictionary<string, FileStream>();

        public static string[] ReadAllLines(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            var l = GetLock(file);
            l.EnterReadLock();

            try
            {
                var stream = GetStream(file);
                var bytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(bytes, 0, bytes.Length);
                var content = Encoding.UTF8.GetString(bytes);
                if (string.IsNullOrEmpty(content))
                {
                    return new string[0];
                }

                return content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
            finally
            {
                l.ExitReadLock();
            }
        }

        public static void AppendText(string file, params string[] data)
        {
            if (string.IsNullOrWhiteSpace(file) || data == null || data.Length <= 0)
            {
                return;
            }

            var l = GetLock(file);
            l.EnterWriteLock();

            try
            {
                var stream = GetStream(file);
                foreach (var item in data)
                {
                    var content = Encoding.UTF8.GetBytes(item + Environment.NewLine);
                    stream.Position = stream.Length;
                    stream.Write(content, 0, content.Length);
                }
            }
            finally
            {
                l.ExitWriteLock();
            }
        }

        public static void WriteAllText(string file, string content)
        {
            if (string.IsNullOrWhiteSpace(file) || content == null)
            {
                return;
            }

            var l = GetLock(file);
            l.EnterWriteLock();

            try
            {
                var stream = GetStream(file);
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Seek(stream.Length, SeekOrigin.Begin);
                stream.Write(bytes, 0, bytes.Length);
            }
            finally
            {
                l.ExitWriteLock();
            }
        }

        public static void Delete(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            var l = GetLock(file);
            l.EnterWriteLock();

            try
            {
                lock (_streams)
                {
                    if (_streams.TryRemove(file, out var stream))
                    {
                        stream.Dispose();
                    }

                    File.Delete(file);
                }
            }
            finally
            {
                l.ExitWriteLock();
            }
        }

        public static void Flush()
        {
            var keys = _streams.Keys.ToList();
            foreach (var key in keys)
            {
                if (!_streams.TryGetValue(key, out var stream))
                {
                    continue;
                }

                stream.Flush();
            }
        }

        public static void TryRelease(params string[] files)
        {
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var l = GetLock(file);
                if (l.TryEnterWriteLock(500))
                {
                    try
                    {
                        if (_streams.TryRemove(file, out var stream))
                        {
                            Log.Debug($"FileManager 释放文件流 {file}");
                            stream.Dispose();
                        }
                    }
                    finally
                    {
                        l.ExitWriteLock();
                    }
                }
            }
        }

        private static ReaderWriterLockSlim GetLock(string file)
        {
            if (_locks.ContainsKey(file))
            {
                return _locks[file];
            }

            lock (_locks)
            {
                if (_locks.ContainsKey(file))
                {
                    return _locks[file];
                }

                var l = new ReaderWriterLockSlim();
                _locks.TryAdd(file, l);
                return l;
            }
        }

        private static FileStream GetStream(string file)
        {
            if (_streams.ContainsKey(file))
            {
                return _streams[file];
            }

            lock (_streams)
            {
                if (_streams.ContainsKey(file))
                {
                    return _streams[file];
                }

                var stream = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                _streams.TryAdd(file, stream);
                return stream;
            }
        }
    }
}
