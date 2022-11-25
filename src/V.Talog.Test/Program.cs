using Bogus;
using Serilog;
using V.Talog;
using V.Talog.Client;
using V.Talog.Extension.Serilog;

V.Talog.Client.Config.TalogServer = "https://vbranch.cn/talog";
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Talog(new LogChannel("test2"))
    .CreateLogger();

using var talogger = new Talogger();
var logs = new Faker<V.Talog.Test.Log>()
    .RuleFor(l => l.Time, f => f.Date.Past())
    .RuleFor(l => l.Level, f => f.Random.Int(0, 2))
    .RuleFor(l => l.IP, f => f.Internet.Ip())
    .RuleFor(l => l.UserId, f => f.Random.Guid().ToString())
    .RuleFor(l => l.Message, f => f.Random.Words());

foreach (var i in Enumerable.Range(0, 20))
{
    var log = logs.Generate();
    Log.Information($"{log.Time} [{log.Level}] {log.IP} {log.Message}");
    Log.Warning(new Exception(log.Message), $"{log.Time} [{log.Level}] {log.IP} {log.Message}");
}

//foreach (var i in Enumerable.Range(0, 20))
//{
//    var log = logs.Generate();
//    talogger.CreateHeaderIndexer("log3")
//        .Tag("date", log.Time.Date.ToString())
//        .Tag("level", log.Level.ToString())
//        .Tag("ip", log.IP)
//        .Data($@"{log.Time} [{log.Level}] {log.IP} 
//            {log.Message}")
//        .Save();
//}
//foreach (var i in Enumerable.Range(0, 20))
//{
//    var log = logs.Generate();
//    talogger.CreateHeaderIndexer("log3")
//        .Tag("date", log.Time.Date.ToString())
//        .Tag("level", log.Level.ToString())
//        .Tag("userid", log.UserId)
//        .Data($@"{log.Time} [{log.Level}] {log.UserId} 
//            {log.Message}")
//        .Save();
//}

//var query = new Query("level", "0");
//var logList = talogger.CreateHeaderSearcher("log3")
//    .SearchLogs(query);
//Console.WriteLine(logList.Count);
//query = new Query("level", "0").Not();
//logList = talogger.CreateHeaderSearcher("log3")
//    .SearchLogs(query);
//Console.WriteLine(logList.Count);

Console.ReadLine();