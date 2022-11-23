using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace V.Talog.Client
{
    public static class MetricSender
    {
        private static ConcurrentQueue<Metric> _metrics = new ConcurrentQueue<Metric>();
        private static Task _sendTask = Task.Run(() =>
        {
            while (true)
            {
                if (_metrics.Any())
                {
                    using (var client = new HttpClient())
                    {
                        while (_metrics.TryDequeue(out var metric))
                        {
                            client.GetAsync($"{Config.TalogServer}/metric/add?index={metric.Index}&name={metric.Name}&value={metric.Value}").Wait();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        });

        public static void Enqueue(string index, string name, decimal value)
        {
            _metrics.Enqueue(new Metric
            {
                Index = index,
                Name = name,
                Value = value
            });
        }
    }
}
