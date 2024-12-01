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
                var content = File.ReadAllText(file);
                if (string.IsNullOrEmpty(content))
                {
                    return new string[0];
                }

                return content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
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
                File.AppendAllLines(file, data);
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
                File.WriteAllText(file, content);
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
                File.Delete(file);
            }
            finally
            {
                l.ExitWriteLock();
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
    }
}
