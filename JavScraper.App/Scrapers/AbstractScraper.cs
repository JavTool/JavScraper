using HtmlAgilityPack;
using JavScraper.App.Http;
using JavScraper.App.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JavScraper.Scrapers;

namespace JavScraper.App.Scrapers
{
    public abstract class AbstractScraper
    {
        protected HttpClientEx client;
        protected ILogger log;
        private static NamedLockerAsync locker = new NamedLockerAsync();


        /// <summary>
        /// 默认的基础 URL
        /// </summary>
        public string DefaultBaseUrl { get; }

        /// <summary>
        /// 基础 URL。
        /// </summary>
        private string base_url = null;

        /// <summary>
        /// 基础 URL。
        /// </summary>
        public string BaseUrl
        {
            get => base_url;
            set
            {
                if (value.IsWebUrl() != true)
                    return;
                if (base_url == value && client != null)
                    return;
                base_url = value;
                client = new HttpClientEx(client => client.BaseAddress = new Uri(base_url));
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="base_url">基础 URL。</param>
        /// <param name="log">日志记录器。</param>
        public AbstractScraper(string base_url, ILogger log)
        {
            DefaultBaseUrl = base_url;
            BaseUrl = base_url;
        }

        /// <summary>
        /// 获取详情。
        /// </summary>
        /// <param name="url">地址。</param>
        /// <returns></returns>
        public abstract Task<JavVideo> ParsePage(string url);


        // ABC-00012 --> ABC-012
        protected static Regex regexKey = new Regex("^(?<a>[a-z0-9]{3,5})(?<b>[-_ ]*)(?<c>0{1,2}[0-9]{3,5})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 7ABC-012  --> ABC-012
        protected static Regex regexKey2 = new Regex("^[0-9][a-z]+[-_a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// 获取列表。
        /// </summary>
        /// <param name="url">地址。</param>
        /// <returns></returns>
        public abstract Task<List<JavVideo>> ParseList(string url);


        public virtual async Task<string> GetDmmPlot(string number)
        {
            number = number.Replace("-", "").Replace("_", "").ToLower();
            using (await locker.LockAsync(number))
            {

                var url = $"https://www.dmm.co.jp/mono/dvd/-/detail/=/cid={number}/";
                var doc = await GetHtmlDocumentAsync(url);

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//tr/td/div[@class='mg-b20 lh4']/p[@class='mg-b20']")?.InnerText?.Trim();

                return plot;
            }
        }

        public virtual async Task<string> GetDmmTitle(string number)
        {
            number = number.Replace("-", "").Replace("_", "").ToLower();
            using (await locker.LockAsync(number))
            {

                var url = $"https://www.dmm.co.jp/mono/dvd/-/detail/=/cid={number}/";
                var doc = await GetHtmlDocumentAsync(url);

                if (doc == null)
                    return null;

                var title = doc.DocumentNode.SelectSingleNode("//tr/td/div[@class='hreview']/h1[@class='item fn']")?.InnerText?.Trim();

                return title;
            }
        }

        /// <summary>
        /// 获取 HtmlDocument，通过 Post 方法提交
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual Task<HtmlDocument> GetHtmlDocumentByPostAsync(string requestUri, Dictionary<string, string> param)
            => GetHtmlDocumentByPostAsync(requestUri, new FormUrlEncodedContent(param));

        /// <summary>
        /// 获取 HtmlDocument，通过 Post 方法提交
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual async Task<HtmlDocument> GetHtmlDocumentByPostAsync(string requestUri, HttpContent content)
        {
            try
            {
                var resp = await client.PostAsync(requestUri, content);
                if (resp.IsSuccessStatusCode == false)
                {
                    var eee = await resp.Content.ReadAsStringAsync();
                    return null;
                }

                var html = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(html) == false)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取 HtmlDocument
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual async Task<HtmlDocument> GetHtmlDocumentAsync(string requestUri)
        {
            try
            {
                var html = await client.GetStringAsync(requestUri);
                if (string.IsNullOrWhiteSpace(html) == false)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"{ex.Message}");
            }

            return null;
        }
        /// <summary>
        /// 展开全部的 Key。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <returns></returns>
        protected virtual List<string> GetAllKeys(string key)
        {
            var ls = new List<string>();

            var m = regexKey2.Match(key);
            if (m.Success)
                ls.Add(key.Substring(1));

            ls.Add(key);

            m = regexKey.Match(key);
            if (m.Success)
            {
                var a = m.Groups["a"].Value;
                var b = m.Groups["b"].Value;
                var c = m.Groups["c"].Value;
                var end = c.TrimStart('0');
                var count = c.Length - end.Length - 1;
                for (int i = 0; i <= count; i++)
                {
                    var em = i > 0 ? new string('0', i) : string.Empty;
                    ls.Add($"{a}{em}{end}");
                    ls.Add($"{a}-{em}{end}");
                    ls.Add($"{a}_{em}{end}");
                }
            }

            if (key.IndexOf('-') > 0)
                ls.Add(key.Replace("-", "_"));
            if (key.IndexOf('_') > 0)
                ls.Add(key.Replace("_", "-"));

            if (ls.Count > 1)
                ls.Add(key.Replace("-", "").Replace("_", ""));

            return ls;
        }
        /// <summary>
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="keyword">关键字。</param>
        /// <returns></returns>
        public abstract bool CheckKeyword(string keyword);
        /// <summary>
        /// 排序。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <param name="ls">索引列表。</param>
        protected virtual void SortIndex(string key, List<JavVideo> ls)
        {
            if (ls?.Any() != true)
                return;

            // 返回的多个结果中，第一个未必是最匹配的，需要手工匹配下
            if (ls.Count > 1 && string.Compare(ls[0].Number, key, true) != 0) // 多个结果，且第一个不一样
            {
                var m = ls.FirstOrDefault(o => string.Compare(o.Number, key, true) == 0)
                    ?? ls.Select(o => new { m = o, v = LevenshteinDistance.Calculate(o.Number.ToUpper(), key.ToUpper()) }).OrderBy(o => o.v).FirstOrDefault().m;

                ls.Remove(m);
                ls.Insert(0, m);
            }
        }

    }
}