using Bogus;
using Serilog;
using V.Talog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

using var taloger = new Taloger();
var logs = new Faker<V.Talog.Test.Log>()
    .RuleFor(l => l.Time, f => f.Date.Past())
    .RuleFor(l => l.Level, f => f.Random.Int(0, 2))
    .RuleFor(l => l.IP, f => f.Internet.Ip())
    .RuleFor(l => l.UserId, f => f.Random.Guid().ToString())
    .RuleFor(l => l.Message, f => f.Random.Words());
foreach (var i in Enumerable.Range(0, 20))
{
    var log = logs.Generate();
    taloger.CreateHeaderIndexer("log3")
        .Tag("date", log.Time.Date.ToString())
        .Tag("level", log.Level.ToString())
        .Tag("ip", log.IP)
        .Data($@"{log.Time} [{log.Level}] {log.IP} 
            {log.Message}")
        .Save();
}
foreach (var i in Enumerable.Range(0, 20))
{
    var log = logs.Generate();
    taloger.CreateHeaderIndexer("log3")
        .Tag("date", log.Time.Date.ToString())
        .Tag("level", log.Level.ToString())
        .Tag("userid", log.UserId)
        .Data($@"{log.Time} [{log.Level}] {log.UserId} 
            {log.Message}")
        .Save();
}

var query = new Query("level", "0");
var logList = taloger.CreateHeaderSearcher("log3")
    .SearchLogs(query);
Console.WriteLine(logList.Count);
query = new Query("level", "0").Not();
logList = taloger.CreateHeaderSearcher("log3")
    .SearchLogs(query);
Console.WriteLine(logList.Count);
Console.ReadLine();