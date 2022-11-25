using Quartz;
using V.Common.Extensions;
using V.Quartz;

namespace V.Talog.Server.Jobs
{
    public class AutoCleanJob : IJob, IScheduledJob
    {
        private Talogger talogger;
        private IConfiguration config;

        public AutoCleanJob(Talogger talogger, IConfiguration config)
        {
            this.talogger = talogger;
            this.config = config;
        }

        public Task Execute(IJobExecutionContext context)
        {
            var duration = this.config["DataRetentionDuration"];
            var days = 30;
            if (!string.IsNullOrWhiteSpace(duration))
            {
                if (!int.TryParse(duration, out days))
                {
                    days = 30;
                }
            }
            var dateLimit = DateTime.Now.Date.AddDays(-days);

            var indexes = this.talogger.GetIndex("pg").GetTagValues("index");
            if (!indexes.IsNullOrEmpty())
            {
                indexes.ForEach(x =>
                {
                    var pages = this.talogger.CreateJsonSearcher("pg")
                        .Search(new Query("index", x))
                        ?.Select(b => b.Tags.FirstOrDefault(t => t.Label == "page"))
                        .Where(t => t != null)
                        .Select(t => t.Value)
                        .Distinct()
                        .ToList();
                    if (pages.IsNullOrEmpty())
                    {
                        return;
                    }

                    pages.ForEach(p =>
                    {
                        var query = this.talogger.CreateQueryByExpression("pg", $"index == {x} && page == {p} && date <= {dateLimit.ToString("yyyyMMdd")}");
                        this.talogger.CreateJsonSearcher("pg")
                            .Remove(query);
                    });
                });
            }

            indexes = this.talogger.GetIndex("metric").GetTagValues("index");
            if (!indexes.IsNullOrEmpty())
            {
                indexes.ForEach(x =>
                {
                    var names = this.talogger.CreateJsonSearcher("metric")
                        .Search(new Query("index", x))
                        ?.Select(b => b.Tags.FirstOrDefault(t => t.Label == "name"))
                        .Where(t => t != null)
                        .Select(t => t.Value)
                        .Distinct()
                        .ToList();
                    if (names.IsNullOrEmpty())
                    {
                        return;
                    }

                    names.ForEach(n =>
                    {
                        var query = this.talogger.CreateQueryByExpression("metric", $"index == {x} && name == {n} && date <= {dateLimit.ToString("yyyyMMdd")}");
                        this.talogger.CreateJsonSearcher("metric")
                            .Remove(query);
                    });
                });
            }

            return Task.CompletedTask;
        }

        public IJobDetail GetJobDetail()
        {
            return JobBuilder.Create<AutoCleanJob>()
                .WithIdentity("AutoCleanJob", "V.Talog.Server")
                .StoreDurably()
                .Build();
        }

        public IEnumerable<ITrigger> GetTriggers()
        {
            yield return TriggerBuilder.Create()
                .WithIdentity("AutoCleanJob_Trigger1", "V.Talog.Server")
                .WithCronSchedule("0 0 */1 * * ?")
                .ForJob("AutoCleanJob", "V.Talog.Server")
                .Build();

            yield return TriggerBuilder.Create()
                .WithIdentity("AutoCleanJob_RightNow", "V.Talog.Server")
                .StartNow()
                .ForJob("AutoCleanJob", "V.Talog.Server")
                .Build();
        }
    }
}
