using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace V.Talog
{
    public static class TalogerExtension
    {
        /// <summary>
        /// 创建 HeaderIndexer
        /// </summary>
        /// <param name="taloger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderIndexer CreateHeaderIndexer(this Taloger taloger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderIndexer(head, taloger.GetIndex(index));
        }

        /// <summary>
        /// 创建 HeaderSearcher
        /// </summary>
        /// <param name="taloger"></param>
        /// <param name="index"></param>
        /// <param name="head">若 head 为 null，则默认使用 index 作为 head</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HeaderSearcher CreateHeaderSearcher(this Taloger taloger, string index, string head = null)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            if (head == null)
            {
                head = index;
            }
            return new HeaderSearcher(head, taloger.GetIndex(index));
        }

        public static JsonIndexer CreateJsonIndexer(this Taloger taloger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonIndexer(taloger.GetIndex(index));
        }

        public static JsonSearcher CreateJsonSearcher(this Taloger taloger, string index)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }

            return new JsonSearcher(taloger.GetIndex(index));
        }

        public static RegexSearcher CreateRegexSearcher(this Taloger taloger, string index, string regex)
        {
            if (string.IsNullOrWhiteSpace(index))
            {
                throw new ArgumentNullException("index");
            }
            if (string.IsNullOrWhiteSpace(regex))
            {
                throw new ArgumentNullException("regex");
            }

            return new RegexSearcher(regex, taloger.GetIndex(index));
        }

        public static IServiceCollection AddTaloger(this IServiceCollection services, Action<Config> config = null)
        {
            var taloger = new Taloger();
            if (config != null)
            {
                config(taloger.Config);
            }
            services.AddSingleton(taloger);
            return services;
        }
    }
}
