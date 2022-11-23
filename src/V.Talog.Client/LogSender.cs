using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using V.Common.Extensions;

namespace V.Talog.Client
{
    internal static class LogSender
    {
        private static ConcurrentDictionary<string, LogQueue> _queues = new ConcurrentDictionary<string, LogQueue>();
        private static Task _sendTask = Task.Run(() =>
        {
            while (true)
            {
                var tasks = new List<Task>();
                var keys = _queues.Keys.ToList();
                foreach (var key in keys)
                {
                    if (!_queues.TryRemove(key, out var queue))
                    {
                        continue;
                    }

                    tasks.Add(Task.Run(() =>
                    {
                        while (queue.Data.Any())
                        {
                            var logs = new List<string>();
                            while (queue.Data.Any() && logs.Count < 10)
                            {
                                if (queue.Data.TryDequeue(out var log))
                                {
                                    logs.Add(log);
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(Config.TalogServer))
                            {
                                using (var client = new HttpClient())
                                {
                                    client.PostAsync($"{Config.TalogServer}/log/index", new StringContent(new
                                    {
                                        queue.Index,
                                        queue.Tags,
                                        data = logs,
                                        queue.Type,
                                        queue.Head
                                    }.ToJson(), Encoding.UTF8, "application/json")).Wait();
                                }
                            }
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
                Thread.Sleep(1000);
            }
        });

        public static void Enqueue(string index, Dictionary<string, string> tags, string log, int type, string head)
        {
            var key = $"{index}{string.Join(";", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}:{t.Value}"))}".Md5();
            if (!_queues.TryGetValue(key, out var queue))
            {
                queue = new LogQueue
                {
                    Index = index,
                    Tags = tags,
                    Data = new ConcurrentQueue<string>(),
                    Type = type,
                    Head = head
                };
                _queues.TryAdd(key, queue);
            }

            queue.Data.Enqueue(log);
        }
    }
}
