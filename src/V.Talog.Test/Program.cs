using Bogus;
using Serilog;
using System.Diagnostics;
using V.Talog;
using V.Talog.Client;
using V.Talog.Extension.Serilog;
using V.Talog.Test;

//V.Talog.Client.Config.TalogServer = "https://vbranch.cn/talog";
Serilog.Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Talog(new LogChannel("test2"))
    .CreateLogger();

using var talogger = new Talogger();
TaloggerExtension.SetIndexMapping(new IndexMapping());
var logs = new Faker<V.Talog.Test.Log>()
    .RuleFor(l => l.Time, f => f.Date.Between(DateTime.Now.AddDays(-5), DateTime.Now))
    .RuleFor(l => l.Level, f => f.Random.Int(0, 2))
    .RuleFor(l => l.IP, f => f.Internet.Ip())
    .RuleFor(l => l.UserId, f => f.Random.Guid().ToString())
    .RuleFor(l => l.Message, f => f.Random.Words());

//talogger.CreateJsonSearcher("1")
//    .RemoveJsonLogs(talogger.CreateQueryByExpression("1", "platform == WinUI && deviceName == COLORFUL"), "path == 'C:\\Users\\Venyo Wong\\Pictures\\amazarashi_百年以后专辑封面.jpg'");

var jsonLogs = talogger.CreateJsonSearcher("1")
    .SearchJsonLogs(talogger.CreateQueryByExpression("1", "platform == WinUI && deviceName == COLORFUL"));

//foreach (var i in Enumerable.Range(0, 20))
//{
//    var log = logs.Generate();
//    Log.Information($"{log.Time} [{log.Level}] {log.IP} {log.Message}");
//    Log.Warning(new Exception(log.Message), $"{log.Time} [{log.Level}] {log.IP} {log.Message}");
//}

var stopwatch = new Stopwatch();
//var times = 200000;
//var logList = Enumerable.Range(0, times)
//    .AsParallel()
//    .Select(x => logs.Generate())
//    .ToList();

//stopwatch.Start();
//logList.AsParallel()
//    .ForAll(log =>
//    {
//        talogger.CreateHeaderIndexer("log3")
//            .Tag("date", log.Time.Date.ToString())
//            .Tag("level", log.Level.ToString())
//            .Data($@"{log.Time} [{log.Level}] {log.IP} 
//                    {log.Message}")
//            .Save();
//    });
//stopwatch.Stop();
//foreach (var i in Enumerable.Range(0, times))
//{
//    var log = logs.Generate();
//    stopwatch.Start();
//    talogger.CreateHeaderIndexer("log3")
//        .Tag("date", log.Time.Date.ToString())
//        .Tag("level", log.Level.ToString())
//        .Data($@"{log.Time} [{log.Level}] {log.IP} 
//            {log.Message}")
//        .Save();
//    stopwatch.Stop();
//}
//Console.WriteLine($"索引 {times} 条日志，总耗时：{stopwatch.Elapsed}");

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

var query = new Query("level", "0");
stopwatch.Start();
var searcher = talogger.CreateHeaderSearcher("log3");
stopwatch.Stop();
Console.WriteLine($"初始化 searcher，总耗时：{stopwatch.Elapsed}");
stopwatch.Restart();
var logList = searcher.SearchLogs(query);
stopwatch.Stop();
Console.WriteLine($"查询日志，总耗时：{stopwatch.Elapsed}");
//Console.WriteLine(logList.Count);
//query = new Query("level", "0").Not();
//logList = talogger.CreateHeaderSearcher("log3")
//    .SearchLogs(query);
//Console.WriteLine(logList.Count);

Console.ReadLine();