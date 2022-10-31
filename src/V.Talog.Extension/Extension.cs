using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace V.Talog
{
    internal static class Extension
    {
        public static ParsedLog Convert2ParsedLog(this TaggedLog x, Regex regex, List<string> names,
            Dictionary<string, Func<string, bool>> funcs = null)
        {
            var match = regex.Match(x.Data);
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

            if (funcs != null && funcs.Any())
            {
                foreach (var item in funcs)
                {
                    if (!names.Contains(item.Key))
                    {
                        continue;
                    }

                    if (!item.Value(log.Groups[item.Key]))
                    {
                        return null;
                    }
                }
            }

            return log;
        }
    }
}
