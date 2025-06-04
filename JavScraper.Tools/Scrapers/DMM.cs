using AngleSharp.Dom;
using Dapper;
using HtmlAgilityPack;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace JavScraper.Tools.Scrapers
{
    public class DMM : AbstractScraper
    {

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="handler"></param>
        public DMM(ILoggerFactory loggerFactory)
            : base("https://www.dmm.co.jp/", loggerFactory.CreateLogger<DMM>())
        {
        }


        /// <summary>
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool CheckKeyword(string key) => JavIdRecognizer.FC2(key) == null;


        public override async Task<List<JavVideo>> ParseList(string url)
        {
            var ls = new List<JavVideo>();
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return ls;

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'video-list')]/a");
            if (nodes?.Any() != true)
                return ls;

            foreach (var node in nodes)
            {
                var videoUrl = node.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(videoUrl))
                    continue;

                var video = new JavVideo { Url = videoUrl };
                ls.Add(video);
            }

            return ls;
        }

        /// <summary>
        /// Dmm 搜索。
        /// </summary>
        /// <param name="keyword">搜索关键字。</param>
        /// <returns></returns>
        public string Search(string keyword)
        {
            string searchUrl = string.Format("https://www.dmm.co.jp/mono/dvd/-/search/=/searchstr={0}/", keyword);
            var web = new HtmlWeb();

            // 配置自定义请求逻辑（通过 PreRequest 委托）
            web.PreRequest = request =>
            {
                // 强制添加 Cookie（需包含 domain 和 path）
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Cookie
                {
                    Name = "age_check_done",
                    Value = "1",
                    Domain = ".dmm.co.jp", // 关键：必须与目标域名匹配
                    Path = "/",
                    Secure = true          // DMM 使用 HTTPS
                });

                // 模拟浏览器请求头
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36";
                request.Headers["Accept-Language"] = "ja-JP";
                request.Referer = "https://www.dmm.co.jp/";

                return true; // 允许请求继续
            };

            var doc = web.Load(searchUrl);
            var searchResultNodes = doc.DocumentNode.SelectNodes("//*[@id='list']/li");
            string url = string.Empty;
            if (searchResultNodes != null)
            {
                var nodes = searchResultNodes.ToList();
                if (nodes.Count > 0)
                {
                    url = nodes.FirstOrDefault().SelectSingleNode("//*[@id='list']/li/div/p[2]/a").Attributes["href"].Value;
                }
            }
            return url;
        }

        public string GetPageUrl(string javId)
        {
            var number = javId.Replace("-", "").Replace("_", "").ToLower();
            var url = $"https://www.dmm.co.jp/mono/dvd/-/detail/=/cid={number}/";
            return url;
        }

        public override async Task<JavVideo> ParsePage(string url)
        {

            //  $"https://www.dmm.co.jp/mono/dvd/-/detail/=/cid={javId}/";
            //url = "https://www.dmm.co.jp/mono/dvd/-/detail/=/cid=ipzz300/";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return null;

            var title = doc.DocumentNode.SelectSingleNode("//div[@class='hreview']/h1[@class='item fn']")?.InnerText?.Trim();
            var node = doc.DocumentNode.SelectSingleNode("//div[@class='hreview']/h1/..");
            if (node == null)
                return null;

            var dic = new Dictionary<string, string>();
            var nodes = doc.DocumentNode.SelectNodes(".//td[@class='nw']");
            foreach (var n in nodes)
            {
                var next = n.NextSibling;
                while (next != null && string.IsNullOrWhiteSpace(next.InnerText))
                    next = next.NextSibling;
                if (next != null)
                    dic[n.InnerText.Trim()] = next.InnerText.Trim();
            }

            string GetValue(string _key)
                => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();

            var genres = ExtractValidParts(GetValue("ジャンル"));
            var actors = doc.DocumentNode.SelectNodes("//span[@id='performer']/a")?
                 .Select(o => o.InnerText.Trim()).ToList();

            var samples = doc.DocumentNode.SelectNodes("//a[@class='fn-sample-image crs_full']/img")?
                 .Select(o => o.GetAttributeValue("data-lazy", null)).Where(o => o != null).ToList();

            var video = new JavVideo()
            {
                Url = url,
                Title = node.SelectSingleNode("./h1")?.InnerText?.Trim(),
                Cover = doc.DocumentNode.SelectSingleNode(".//img[@class='tdmm is-zoom']")?.GetAttributeValue("src", null),
                Number = GetValue("品番"),
                Date = GetValue("発売日"),
                Runtime = GetValue("収録時間"),
                Maker = GetValue("メーカー"),
                Studio = GetValue("レーベル"),
                Set = GetValue("シリーズ"),
                Director = GetValue("監督"),
                //Plot = node.SelectSingleNode("./h3")?.InnerText,
                Genres = genres,
                Actors = actors,
                Samples = samples,
            };

            video.Plot = await GetDmmPlot(video.Number);
            // 去除标题中的番号
            if (string.IsNullOrWhiteSpace(video.Number) == false && video.Title?.StartsWith(video.Number, StringComparison.OrdinalIgnoreCase) == true)
            {
                video.Title = video.Title[video.Number.Length..].Trim();
            }
            video.Title = RemoveActorsFromTitle(video.Title, video.Actors);
            video.OriginalTitle = RemoveActorsFromTitle(video.OriginalTitle, video.Actors);
            return video;
        }

        /// <summary>
        /// 取出 &nbsp; 分割的不含 "40％" 字符的部分
        /// </summary>
        /// <param name="input">原始 HTML 编码字符串</param>
        /// <returns>过滤后的字符串数组</returns>
        public static List<string> ExtractValidParts(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Array.Empty<string>().ToList();

            // 解码 HTML 实体，将 "&nbsp;" 转换为空格
            //string decodedString = HttpUtility.HtmlDecode(input);
            string decodedString = input.Replace("&nbsp;", "|");

            // 按空格拆分，并过滤掉包含 "40％" 的部分
            return decodedString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) // 按空格拆分
                .Where(part => !part.Contains("40％")) // 过滤包含 "40％" 的部分
                .ToList();
        }

        public async Task<JavVideo> SearchAndParseJavVideo(string javId)
        {
            var searchUrl = $"https://www.dmm.co.jp/mono/dvd/-/search/=/searchstr={javId}/";
            var doc = await GetHtmlDocumentAsync(searchUrl);
            if (doc == null)
                return null;

            var videoNode = doc.DocumentNode.SelectSingleNode("//p[contains(@class,'tmb')]/a");
            if (videoNode == null)
                return null;

            var videoUrl = videoNode.GetAttributeValue("href", null);
            var javVideo = await ParsePage(videoUrl);
            return javVideo;
        }
    }
}
