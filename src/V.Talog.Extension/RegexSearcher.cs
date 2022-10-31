using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace V.Talog
{
    /// <summary>
    /// 对 Data 进行正则匹配，并支持 named groups 筛选
    /// <para>正则表达式中，请不要使用数字作为 name</para>
    /// </summary>
    public class RegexSearcher : Searcher
    {
        private Regex regex;
        private List<string> names;
        private Dictionary<string, Func<string, bool>> funcs = new Dictionary<string, Func<string, bool>>();

        public RegexSearcher(string regex, Index index) : base(index)
        {
            this.regex = new Regex(regex);
            this.regex.GetGroupNames()
                .Where(x => !int.TryParse(x, out int _))
                .ToList();
        }

        /// <summary>
        /// 添加过滤器
        /// </summary>
        /// <param name="name">若 name 不在正则表达式中，func 将不起作用</param>
        /// <param name="func"></param>
        /// <returns></returns>
        public RegexSearcher AddFilter(string name, Func<string, bool> func)
        {
            this.funcs[name] = func;
            return this;
        }

        public List<ParsedLog> SearchParsedLogs(Query query)
        {
            var logs = base.SearchLogs(query);
            return logs.Select(x => x.Convert2ParsedLog(this.regex, this.names, this.funcs))
                .Where(x => x != null)
                .ToList();
        }
    }
}
