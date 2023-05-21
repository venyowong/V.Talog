using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace V.Talog
{
    public static class LogExtension
    {
        public static List<ParsedLog> SelectParsedLogs(this IEnumerable<TaggedLog> logs, string regex, Func<ParsedLog, bool> filter = null)
        {
            var reg = GetRegex(regex);
            var names = GetGroupNames(reg);

            return logs.Select(x =>
            {
                var match = reg.Match(x.Data);
                if (!match.Success)
                {
                    return null;
                }

                var log = new ParsedLog
                {
                    Data = x.Data,
                    Tags = x.Tags
                };
                foreach (var name in names)
                {
                    log.Groups[name] = match.Groups[name].Value;
                }

                return log;
            }).Where(x => x != null)
            .Where(x =>
            {
                if (filter == null)
                {
                    return true;
                }

                return filter(x);
            })
            .ToList();
        }

        private static ConcurrentDictionary<string, Regex> _regexCache = new ConcurrentDictionary<string, Regex>();
        public static Regex GetRegex(string regex)
        {
            if (_regexCache.TryGetValue(regex, out var reg))
            {
                return reg;
            }

            reg = new Regex(regex);
            _regexCache.TryAdd(regex, reg);
            return reg;
        }

        private static ConcurrentDictionary<Regex, List<string>> _groupNames = new ConcurrentDictionary<Regex, List<string>>();
        public static List<string> GetGroupNames(Regex regex)
        {
            if (_groupNames.TryGetValue(regex, out var groupNames))
            {
                return groupNames;
            }

            groupNames = regex.GetGroupNames()
                .Where(x => !int.TryParse(x, out int _))
                .ToList();
            _groupNames.TryAdd(regex, groupNames);
            return groupNames;
        }
    }
}
