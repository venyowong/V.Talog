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
    public static class PageViewSender
    {
        private static ConcurrentQueue<PageView> _pageViews = new ConcurrentQueue<PageView>();
        private static Task _sendTask = Task.Run(() =>
        {
            while (true)
            {
                if (_pageViews.Any())
                {
                    using (var client = new HttpClient())
                    {
                        while (_pageViews.TryDequeue(out var pageView))
                        {
                            client.GetAsync($"{Config.TalogServer}/metric/pg/add?index={pageView.Index}&page={pageView.Page}&user={pageView.User}").Wait();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        });

        public static void Enqueue(string index, string page, string user)
        {
            _pageViews.Enqueue(new PageView
            {
                Index = index,
                Page = page,
                User = user
            });
        }
    }
}
