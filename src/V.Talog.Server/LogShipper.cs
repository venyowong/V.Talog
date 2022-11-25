using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using V.Common.Extensions;
using V.Talog.Server.LogWatcher;
using V.Talog.Server.Models;

namespace V.Talog.Server
{
    public class LogShipper : IHostedService, IDisposable
    {
        private const string _watchingFileName = "watching_files.json";

        private List<LogFileConfig> logs = new List<LogFileConfig>();
        private ConcurrentBag<FileSystemWatcher> watchers = new ConcurrentBag<FileSystemWatcher>();
        private ConcurrentDictionary<string, LogFile> files;
        private CancellationToken token;
        private Task task;
        private volatile bool needSave = false;
        private Talogger talogger;

        public LogShipper(IConfiguration config, Talogger talogger)
        {
            this.talogger = talogger;
            var section = config.GetSection("Logs");
            if (section != null)
            {
                section.Bind(this.logs);
            }

            if (File.Exists(_watchingFileName))
            {
                this.files = JsonConvert.DeserializeObject<ConcurrentDictionary<string, LogFile>>(File.ReadAllText(_watchingFileName));
            }
            else
            {
                this.files = new ConcurrentDictionary<string, LogFile>();
            }

            this.task = Task.Run(() =>
            {
                while (true)
                {
                    if (token != default && token.IsCancellationRequested)
                    {
                        return;
                    }

                    Thread.Sleep(1000);
                    this.SaveFiles();
                }
            });

            Log.Information("LogShipper inited.");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.token = cancellationToken;

            logs.AsParallel().ForAll(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Head))
                {
                    x.Head = x.Index;
                }

                // 初始化 index
                var storedIndexSearcher = this.talogger.CreateJsonSearcher("stored_index");
                var json = storedIndexSearcher.SearchJsonLogs(new Query("name", x.Index))
                    ?.Select(x => x.Data)
                    .FirstOrDefault();
                if (json == null)
                {
                    this.talogger.CreateJsonIndexer("stored_index")
                        .Tag("name", x.Index)
                        .Data(JsonConvert.SerializeObject(new
                        {
                            index = x.Index,
                            type = x.Type,
                            head = x.Head
                        }))
                        .Save();
                }
                else
                {
                    if (json["type"]?.ToString() != x.Type.ToString())
                    {
                        Log.Warning($"LogShipper: 配置({x.ToJson()})与 index 配置(Type={json["type"]})冲突，因此将不会对相应文件进行监听");
                        return;
                    }
                }

                // 启动后先加载一遍历史日志文件
                var files = Directory.GetFiles(x.Path, x.Filter, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var f = file.ToLower();
                    if (!this.files.TryGetValue(f, out var logFile))
                    {
                        this.needSave = true;
                        logFile = new LogFile(f);
                        this.files.TryAdd(f, logFile);
                    }

                    var (content, offset) = logFile.ReadIncrement();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        continue;
                    }

                    if (this.IndexLogs(content, f, x))
                    {
                        logFile.LastOffset = offset;
                        this.needSave = true;
                    }
                }

                var watcher = new FileSystemWatcher(x.Path);
                if (!string.IsNullOrWhiteSpace(x.Filter))
                {
                    watcher.Filter = x.Filter;
                }
                watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
                watcher.Created += this.OnCreated;
                watcher.Changed += this.OnChanged;
                watcher.Error += this.OnError;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;

                this.watchers.Add(watcher);
            });

            Log.Information("LogShipper started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var w in this.watchers)
            {
                w.Dispose();
            }

            this.watchers = new ConcurrentBag<FileSystemWatcher>();
            Log.Information("LogShipper stopped.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var w in this.watchers)
            {
                w.Dispose();
            }

            Log.Information("LogShipper disposed.");
        }

        private void SaveFiles()
        {
            if (!this.needSave)
            {
                return;
            }

            lock (this)
            {
                File.WriteAllText(_watchingFileName, this.files.ToJson());
            }

            this.needSave = false;
            Log.Debug($"LogShipper saved {_watchingFileName}.");
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (this.token != default && this.token.IsCancellationRequested)
            {
                return;
            }

            var path = e.FullPath.ToLower();
            Log.Information($"LogShipper: {path} created, start watching");
            this.needSave = true;

            // 文件可能被删除之后重新创建
            if (this.files.TryGetValue(path, out var file))
            {
                file.LastOffset = 0;
            }
            else
            {
                file = new LogFile(path);
                this.files.TryAdd(path, file);
            }
        }
        
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (this.token != default && this.token.IsCancellationRequested)
            {
                return;
            }

            var path = e.FullPath.ToLower();
            if (!this.files.TryRemove(path, out var logFile)) // 如果在字典中不存在，说明已有其他线程在处理文件变化，则该线程不处理
            {
                return;
            }

            var (content, offset) = logFile.ReadIncrement();
            if (!string.IsNullOrWhiteSpace(content))
            {
                var config = this.logs.FirstOrDefault(x =>
                {
                    var files = Directory.GetFiles(x.Path, x.Filter, SearchOption.AllDirectories);
                    return files.Any(f => f.ToLower() == path);
                });
                if (config != null)
                {
                    if (this.IndexLogs(content, path, config))
                    {
                        logFile.LastOffset = offset;
                        this.needSave = true;
                    }
                }
                else
                {
                    Log.Warning($"LogShipper: {path} 处理增量数据失败，找不到对应配置");
                }
            }

            this.files.TryAdd(path, logFile);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            if (this.token != default && this.token.IsCancellationRequested)
            {
                return;
            }

            Log.Warning(e.GetException(), "LogShipper got an exception, and restart now.");

            foreach (var w in this.watchers)
            {
                w.Dispose();
            }

            this.watchers = new ConcurrentBag<FileSystemWatcher>();
            this.StartAsync(this.token).Wait();
        }

        private bool IndexLogs(string content, string path, LogFileConfig config)
        {
            string[] logs;
            if (string.IsNullOrWhiteSpace(config.Regex))
            {
                logs = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                var regex = this.GetRegex(config.Regex);
                var collection = regex.Matches(content);
                logs = collection.Select(x => x.Value).ToArray();
            }
            if (logs.IsNullOrEmpty())
            {
                return false;
            }

            foreach (var log in logs)
            {
                Indexer indexer;
                if (config.Type == 1)
                {
                    indexer = this.talogger.CreateHeaderIndexer(config.Index, config.Head);
                }
                else
                {
                    indexer = this.talogger.CreateIndexer(config.Index);
                }

                foreach (var tag in config.Tags)
                {
                    indexer.Tag(tag.Key, tag.Value);
                }
                indexer.Tag("path", path)
                    .Data(log)
                    .Save();
            }

            return true;
        }

        private static ConcurrentDictionary<string, Regex> _regexCache = new ConcurrentDictionary<string, Regex>();
        private Regex GetRegex(string regex)
        {
            if (_regexCache.TryGetValue(regex, out var reg))
            {
                return reg;
            }

            reg = new Regex(regex);
            _regexCache.TryAdd(regex, reg);
            return reg;
        }
    }
}
