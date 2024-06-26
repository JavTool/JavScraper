﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Domain;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// https://www.arzon.jp/itemlist.html?t=&m=all&s=&q=
    /// https://www.arzon.jp/item_1615858.html
    /// </summary>
    public class ArzonScraper : AbstractScraper
    {
        /// <summary>
        /// 适配器名称。
        /// </summary>
        public override string Name => "Arzon";

        /// <summary>
        /// 番号分段识别。
        /// </summary>
        private static Regex regex = new Regex("((?<a>[a-z]{2,})|(?<b>[0-9]{2,}))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 初始化 <seealso cref="ArzonScraper"/> 类的新实例。
        /// </summary>
        /// <param name="logManager"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public ArzonScraper(ILoggerFactory logManager)
            : base("https://www.arzon.jp/", logManager.CreateLogger<ArzonScraper>())
        {
        }

        /// <summary>
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <returns></returns>
        public override bool CheckKeyword(string key) => JavIdRecognizer.FC2(key) == null;

        /// <summary>
        /// 搜索列表。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <returns></returns>
        protected override async Task<List<JavVideoIndex>> Search(List<JavVideoIndex> ls, string key)
        {
            // https://www.arzon.jp/item_1615858.html
            var doc = await GetHtmlDocumentAsync($"/search?q={key}&f=all");
            if (doc != null)
                ParseIndex(ls, doc);

            if (ls.Any())
            {
                var ks = regex.Matches(key).Cast<Match>()
                     .Select(o => o.Groups[0].Value).ToList();

                ls.RemoveAll(i =>
                {
                    foreach (var k in ks)
                    {
                        if (i.Num?.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) //包含，则继续
                            continue;
                        if (k[0] != '0') //第一个不是0，则不用继续了。
                            return true;//移除

                        var k2 = k.TrimStart('0');
                        if (i.Num?.IndexOf(k2, StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        return true; //移除
                    }
                    return false; //保留
                });
            }

            SortIndex(key, ls);
            return ls;
        }

        /// <summary>
        /// 解析列表。
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected override List<JavVideoIndex> ParseIndex(List<JavVideoIndex> ls, HtmlDocument doc)
        {
            if (doc == null)
                return ls;
            var nodes = doc.DocumentNode.SelectNodes("//*[@id='videos']/div/div/a");
            if (nodes?.Any() != true)
                return ls;

            foreach (var node in nodes)
            {
                var url = node.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                var m = new JavVideoIndex() { Provider = Name, Url = new Uri(client.BaseAddress, url).ToString() };
                ls.Add(m);
                var img = node.SelectSingleNode("./div/img");
                if (img != null)
                {
                    m.Cover = img.GetAttributeValue("data-original", null);
                    if (string.IsNullOrEmpty(m.Cover))
                        m.Cover = img.GetAttributeValue("data-src", null);
                    if (string.IsNullOrEmpty(m.Cover))
                        m.Cover = img.GetAttributeValue("src", null);
                    if (m.Cover?.StartsWith("//") == true)
                        m.Cover = $"https:{m.Cover}";
                }

                m.Num = node.SelectSingleNode("./div[@class='uid']")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Num))
                    m.Num = node.SelectSingleNode("./div[@class='uid2']")?.InnerText.Trim();
                m.Title = node.SelectSingleNode("./div[@class='video-title']")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Title))
                    m.Title = node.SelectSingleNode("./div[@class='video-title2']")?.InnerText.Trim();
                m.Date = node.SelectSingleNode("./div[@class='meta']")?.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(m.Num) == false && m.Title?.StartsWith(m.Num, StringComparison.OrdinalIgnoreCase) == true)
                    m.Title = m.Title.Substring(m.Num.Length).Trim();
            }

            return ls;
        }

        /// <summary>
        /// 获取详情。
        /// </summary>
        /// <param name="url">地址。</param>
        /// <returns></returns>
        public override async Task<JavVideo> Get(string url)
        {
            // https://www.arzon.jp/item_1615858.html
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return null;

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'panel-block')]");
            if (nodes?.Any() != true)
                return null;

            var dic = new Dictionary<string, string>();
            foreach (var n in nodes)
            {
                var k = n.SelectSingleNode("./strong")?.InnerText?.Trim();
                string v = null;
                if (k?.Contains("演員") == true)
                {
                    var ac = n.SelectNodes("./*[@class='value']/a");
                    if (ac?.Any() == true)
                        v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                }

                if (v == null)
                    v = n.SelectSingleNode("./*[@class='value']")?.InnerText?.Trim().Replace("&nbsp;", " ");

                if (string.IsNullOrWhiteSpace(k) == false && string.IsNullOrWhiteSpace(v) == false)
                    dic[k] = v;
            }

            string GetValue(string _key)
                => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();

            string GetCover()
            {
                var coverNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class,'video-cover')]");
                var img = coverNode?.GetAttributeValue("data-original", null);
                if (string.IsNullOrEmpty(img))
                    img = coverNode?.GetAttributeValue("data-src", null);
                if (string.IsNullOrEmpty(img))
                    img = coverNode?.GetAttributeValue("src", null);

                if (string.IsNullOrWhiteSpace(img) == false)
                    return img;
                img = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);
                if (string.IsNullOrWhiteSpace(img) == false)
                    return img;
                img = doc.DocumentNode.SelectSingleNode("//meta[@class='column column-video-cover']")?.GetAttributeValue("poster", null);

                return img;
            }

            List<string> GetGenres()
            {
                var v = GetValue("类别");
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                return v.Split(',').Select(o => o.Trim()).Distinct().ToList();
            }

            List<string> GetActors()
            {
                var v = GetValue("AV女優");
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                var ac = v.Split(',').Select(o => o.Trim()).Distinct().ToList();
                for (int i = 0; i < ac.Count; i++)
                {
                    var a = ac[i];
                    if (a.Contains("(") == false)
                        continue;
                    var arr = a.Split("()".ToArray(), StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
                    if (arr.Length == 2)
                        ac[i] = arr[1];
                }
                return ac;
            }

            List<string> GetSamples()
            {
                return doc.DocumentNode.SelectNodes("//div[@class='tile-images preview-images']/a")
                      ?.Select(o => o.GetAttributeValue("href", null))
                      .Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();
            }

            var javVideo = new JavVideo()
            {
                Provider = Name,
                Url = url,
                Title = doc.DocumentNode.SelectSingleNode("//*[contains(@class,'title')]/strong")?.InnerText?.Trim(),
                Cover = GetCover(),
                Num = GetValue("品番"),
                Date = GetValue("発売日"),
                Runtime = GetValue("収録時間"),
                Maker = GetValue("片商"),
                Studio = GetValue("AVメーカー"),
                Set = GetValue("系列"),
                Director = GetValue("監督"),
                Genres = GetGenres(),
                Actors = GetActors(),
                Samples = GetSamples(),
            };

            javVideo.Plot = await GetDmmPlot(javVideo.Num);
            ////去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Num) == false && javVideo.Title?.StartsWith(javVideo.Num, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Num.Length).Trim();

            return javVideo;
        }

    }
}
