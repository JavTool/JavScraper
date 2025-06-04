using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using Microsoft.Extensions.Logging;

namespace JavScraper.Tools.Scrapers
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
        //public override string Name => "Arzon";

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
        /// Dmm 搜索。
        /// </summary>
        /// <param name="keyword">搜索关键字。</param>
        /// <returns></returns>
        public string Search(string keyword)
        {

            // https://www.arzon.jp/index.php?action=adult_customer_agecheck&agecheck=1&redirect=https%3A%2F%2Fwww.arzon.jp%2Fitemlist.html%3Ft%3D%26m%3Dall%26s%3D%26q%3DMIDE-060

            //https://www.arzon.jp/index.php?action=adult_customer_agecheck&dchk=d41d8cd98f00b204e9800998ecf8427e&redirect=https%3A%2F%2Fwww.arzon.jp%2Fitemlist.html%3Ft%3D%26m%3Dall%26s%3D%26q%3DMIDE-060
            //https://www.arzon.jp/itemlist.html?t=&m=all&s=&q=
            string searchUrl = string.Format("https://www.arzon.jp/index.php?action=adult_customer_agecheck&dchk=d41d8cd98f00b204e9800998ecf8427e&redirect=https%3A%2F%2Fwww.arzon.jp%2Fitemlist.html%3Ft%3D%26m%3Dall%26s%3D%26q%3D{0}", keyword);
            var web = new HtmlWeb();

            //// 配置自定义请求逻辑（通过 PreRequest 委托）
            //web.PreRequest = request =>
            //{
            //    // 强制添加 Cookie（需包含 domain 和 path）
            //    request.CookieContainer = new CookieContainer();
            //    request.CookieContainer.Add(new Cookie
            //    {
            //        Name = "age_check_done",
            //        Value = "1",
            //        Domain = ".dmm.co.jp", // 关键：必须与目标域名匹配
            //        Path = "/",
            //        Secure = true          // DMM 使用 HTTPS
            //    });

            //    // 模拟浏览器请求头
            //    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36";
            //    request.Headers["Accept-Language"] = "ja-JP";
            //    request.Referer = "https://www.dmm.co.jp/";

            //    return true; // 允许请求继续
            //};

            var doc = web.Load(searchUrl);

            if (doc == null)
                return null;
            var nodes = doc.DocumentNode.SelectNodes("//*[@id='videos']/div/div/a");
            if (nodes?.Any() != true)
                return null;
            string url = string.Empty;
            foreach (var node in nodes)
            {
                url = node.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(url))
                    continue;
                //var m = new JavVideo() { Url = new Uri(client.BaseAddress, url).ToString() };
                //ls.Add(m);
                //var img = node.SelectSingleNode("./div/img");
                //if (img != null)
                //{
                //    m.Cover = img.GetAttributeValue("data-original", null);
                //    if (string.IsNullOrEmpty(m.Cover))
                //        m.Cover = img.GetAttributeValue("data-src", null);
                //    if (string.IsNullOrEmpty(m.Cover))
                //        m.Cover = img.GetAttributeValue("src", null);
                //    if (m.Cover?.StartsWith("//") == true)
                //        m.Cover = $"https:{m.Cover}";
                //}

                //m.Number = node.SelectSingleNode("./div[@class='uid']")?.InnerText.Trim();
                //if (string.IsNullOrEmpty(m.Number))
                //    m.Number = node.SelectSingleNode("./div[@class='uid2']")?.InnerText.Trim();
                //m.Title = node.SelectSingleNode("./div[@class='video-title']")?.InnerText.Trim();
                //if (string.IsNullOrEmpty(m.Title))
                //    m.Title = node.SelectSingleNode("./div[@class='video-title2']")?.InnerText.Trim();
                //m.Date = node.SelectSingleNode("./div[@class='meta']")?.InnerText.Trim();

                //if (string.IsNullOrWhiteSpace(m.Number) == false && m.Title?.StartsWith(m.Number, StringComparison.OrdinalIgnoreCase) == true)
                //    m.Title = m.Title.Substring(m.Number.Length).Trim();
            }
            //var searchResultNodes = doc.DocumentNode.SelectNodes("//*[@id='list']/li");
            //string url = string.Empty;
            //if (searchResultNodes != null)
            //{
            //    var nodes = searchResultNodes.ToList();
            //    if (nodes.Count > 0)
            //    {
            //        url = nodes.FirstOrDefault().SelectSingleNode("//*[@id='list']/li/div/p[2]/a").Attributes["href"].Value;
            //    }
            //}
            return url;
        }

        /// <summary>
        /// 搜索列表。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <returns></returns>
        protected async Task<List<JavVideo>> Search(List<JavVideo> ls, string key)
        {
            var doc = await GetHtmlDocumentAsync($"/search?q={key}&f=all");
            if (doc != null)
                ParseList(ls, doc);

            if (ls.Any())
            {
                var ks = regex.Matches(key).Cast<Match>()
                     .Select(o => o.Groups[0].Value).ToList();

                ls.RemoveAll(i =>
                {
                    foreach (var k in ks)
                    {
                        if (i.Number?.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) //包含，则继续
                            continue;
                        if (k[0] != '0') //第一个不是0，则不用继续了。
                            return true; //移除

                        var k2 = k.TrimStart('0');
                        if (i.Number?.IndexOf(k2, StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        return true; //移除
                    }
                    return false; //保留
                });
            }

            SortIndex(key, ls);
            return ls;
        }

        public async Task<JavVideo> SearchAndParseJavVideo(string javId)
        {
            var ls = new List<JavVideo>();
            await Search(ls, javId);
            return ls.FirstOrDefault(); // 返回第一个匹配的 JavVideo
        }

        /// <summary>
        /// 解析列表。
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected List<JavVideo> ParseList(List<JavVideo> ls, HtmlDocument doc)
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
                var m = new JavVideo() { Url = new Uri(client.BaseAddress, url).ToString() };
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

                m.Number = node.SelectSingleNode("./div[@class='uid']")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Number))
                    m.Number = node.SelectSingleNode("./div[@class='uid2']")?.InnerText.Trim();
                m.Title = node.SelectSingleNode("./div[@class='video-title']")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Title))
                    m.Title = node.SelectSingleNode("./div[@class='video-title2']")?.InnerText.Trim();
                m.Date = node.SelectSingleNode("./div[@class='meta']")?.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(m.Number) == false && m.Title?.StartsWith(m.Number, StringComparison.OrdinalIgnoreCase) == true)
                    m.Title = m.Title.Substring(m.Number.Length).Trim();
            }

            return ls;
        }

        /// <summary>
        /// 获取详情。
        /// </summary>
        /// <param name="url">地址。</param>
        /// <returns></returns>

        public override async Task<JavVideo> ParsePage(string url)
        {
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
                //Provider = Name,
                Url = url,
                Title = doc.DocumentNode.SelectSingleNode("//*[contains(@class,'title')]/strong")?.InnerText?.Trim(),
                Cover = GetCover(),
                Number = GetValue("品番"),
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

            javVideo.Plot = await GetDmmPlot(javVideo.Number);
            //去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Number) == false && javVideo.Title?.StartsWith(javVideo.Number, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();

            return javVideo;
        }

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

        //public override Task<JavVideo> ParsePage(string url)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<List<JavVideo>> ParseList(string url)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
