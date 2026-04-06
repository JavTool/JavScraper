using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;
using System.Net.Http;

namespace JavScraper.Common.Scrapers
{
    // 这是对 metatube 的 provider/caribbeancom 的简化移植示例
    public class CaribbeanComScraper : BaseScraper
    {
        private readonly HttpClient http;

        public CaribbeanComScraper(HttpClient httpClient) : base("caribbeancom", "https://www.caribbeancom.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var url = $"https://www.caribbeancom.com/moviepages/{code}/index.html";

            // 使用 AbstractScraper 中类似的渲染方法不可用，所以做最小请求
            var html = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText?.Trim();
            var plot = doc.DocumentNode.SelectSingleNode("//p[@itemprop='description']")?.InnerText?.Trim();

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'movie-info')]/ul/li");
            var dic = new Dictionary<string, string>();
            if (nodes?.Any() == true)
            {
                foreach (var n in nodes)
                {
                    var k = n.SelectSingleNode("./span")?.InnerText?.Trim();
                    string v = null;
                    if (k?.Contains("出演") == true)
                    {
                        var ac = n.SelectNodes("./*[@class='spec-content']/a");
                        if (ac?.Any() == true)
                            v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                    }
                    if (v == null)
                        v = n.SelectSingleNode("./*[@class='spec-content']")?.InnerText?.Trim().Replace("&nbsp;", " ");

                    if (string.IsNullOrWhiteSpace(k) == false && string.IsNullOrWhiteSpace(v) == false)
                        dic[k] = v;
                }
            }

            List<string> GetGenres()
            {
                var v = dic.Where(o => o.Key.Contains("タグ")).Select(o => o.Value).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                return v.Replace("\t", "").Replace("\n", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
            }

            List<string> GetActors()
            {
                var v = dic.Where(o => o.Key.Contains("出演")).Select(o => o.Value).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                var ac = v.Split(',').Select(o => o.Trim()).Distinct().ToList();
                for (int i = 0; i < ac.Count; i++)
                {
                    var a = ac[i];
                    if (a.Contains("(") == false)
                        continue;
                    var arr = a.Split("()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
                    if (arr.Length == 2)
                        ac[i] = arr[1];
                }
                return ac;
            }

            var video = new JavVideo
            {
                Url = url,
                Number = code,
                Title = title,
                Plot = plot,
                Maker = dic.Where(o => o.Key.Contains("スタジオ")).Select(o => o.Value).FirstOrDefault()
            };

            var actors = GetActors();
            if (actors != null && actors.Count > 0)
                video.Actors = actors;

            var genres = GetGenres();
            if (genres != null && genres.Count > 0)
                video.Genres = genres;

            return video;
        }
    }
}
