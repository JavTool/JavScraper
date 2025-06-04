using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// https://www.jav321.com/
    /// </summary>
    public class Jav123Scraper : AbstractScraper
    {
        /// <summary>
        /// 适配器名称
        /// </summary>
        //public override string Name => "Jav123";

        /// <summary>
        /// 初始化 <seealso cref="ArzonScraper"/> 类的新实例。
        /// </summary>
        /// <param name="logManager"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public Jav123Scraper(ILoggerFactory logManager)
            : base("https://www.jav321.com/", logManager.CreateLogger<Jav123Scraper>())
        {
        }

        /// <summary>
        /// 检查关键字是否符合
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool CheckKeyword(string key)
            => JavIdRecognizer.FC2(key) == null;

        /// <summary>
        /// Jav123 搜索。
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

        public async Task<JavVideo> SearchAndParseJavVideo(string javId)
        {
            var ls = new List<JavVideo>();
            await Search(ls, javId);
            return ls.FirstOrDefault(); // 返回第一个匹配的 JavVideo
        }

        /// <summary>
        /// 获取列表
        /// </summary>
        /// <param name="key">关键字</param>
        /// <returns></returns>
        protected async Task<List<JavVideo>> Search(List<JavVideo> ls, string key)
        {
            ///https://www.jav321.com/search
            ///POST sn=key
            var doc = await GetHtmlDocumentByPostAsync($"/search", new Dictionary<string, string>() { ["sn"] = key });
            if (doc != null)
            {
                var video = await ParseVideo(null, doc);
                if (video != null)
                    ls.Add(video);
            }

            SortIndex(key, ls);
            return ls;
        }

        /// <summary>
        /// 不用了
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected List<JavVideo> ParseIndex(List<JavVideo> ls, HtmlDocument doc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns></returns>
        public async Task<JavVideo> Get(string url)
        {
            //https://javdb.com/v/BzbA6
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return null;

            return await ParseVideo(url, doc);
        }

        private async Task<JavVideo> ParseVideo(string url, HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//div[@class='panel-heading']/h3/../..");
            if (node == null)
                return null;
            var nodes = node.SelectNodes(".//b");
            if (nodes?.Any() != true)
                return null;

            if (string.IsNullOrWhiteSpace(url))
            {
                url = doc.DocumentNode.SelectSingleNode("//li/a[contains(text(),'简体中文')]")?.GetAttributeValue("href", null);
                if (url?.StartsWith("//") == true)
                    url = $"https:{url}";
            }

            var dic = new Dictionary<string, string>();
            foreach (var n in nodes)
            {
                var name = n.InnerText.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var arr = new List<string>();

                var next = n.NextSibling;
                while (next != null && next.Name != "b")
                {
                    arr.Add(next.InnerText);
                    next = next.NextSibling;
                }
                if (arr.Count == 0)
                    continue;

                var value = string.Join(", ", arr.Select(o => o.Replace("&nbsp;", " ").Trim(": ".ToArray())).Where(o => string.IsNullOrWhiteSpace(o) == false));

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                dic[name] = value;
            }

            string GetValue(string _key)
                => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();

            string GetCover()
            {
                var img = node.SelectSingleNode(".//*[@id='vjs_sample_player']")?.GetAttributeValue("poster", null);
                if (string.IsNullOrWhiteSpace(img) == false)
                    return img;
                if (string.IsNullOrWhiteSpace(img) == false)
                    return img;
                img = node.SelectSingleNode(".//*[@id='video-player']")?.GetAttributeValue("poster", null);
                img = doc.DocumentNode.SelectSingleNode("//img[@class='img-responsive']")?.GetAttributeValue("src", null);
                if (string.IsNullOrWhiteSpace(img) == false)
                    return img;
                return img;
            }

            List<string> GetGenres()
            {
                var v = GetValue("ジャンル");
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                return v.Split(',').Select(o => o.Trim()).Distinct().ToList();
            }

            List<string> GetActors()
            {
                var v = GetValue("出演者");
                if (string.IsNullOrWhiteSpace(v))
                    return null;
                var ac = v.Split(',').Select(o => o.Trim()).Distinct().ToList();
                return ac;
            }
            List<string> GetSamples()
            {
                return doc.DocumentNode.SelectNodes("//a[contains(@href,'snapshot')]/img")
                      ?.Select(o => o.GetAttributeValue("src", null))
                      .Where(o => string.IsNullOrWhiteSpace(o) == false).ToList();
            }

            var javVideo = new JavVideo()
            {
                //Provider = Name,
                Url = url,
                Title = node.SelectSingleNode(".//h3/text()")?.InnerText?.Trim(),
                Cover = GetCover(),
                Number = GetValue("品番")?.ToUpper(),
                Date = GetValue("配信開始日"),
                Runtime = GetValue("収録時間"),
                Maker = GetValue("メーカー"),
                Studio = GetValue("メーカー"),
                Set = GetValue("系列"),
                Director = GetValue("导演"),
                Genres = GetGenres(),
                Actors = GetActors(),
                Samples = GetSamples(),
                Plot = node.SelectSingleNode("./div[@class='panel-body']/div[last()]")?.InnerText?.Trim(),
            };
            if (string.IsNullOrWhiteSpace(javVideo.Plot))
                javVideo.Plot = await GetDmmPlot(javVideo.Number);
            //去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Number) == false && javVideo.Title?.StartsWith(javVideo.Number, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();
            //去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Number) == false && javVideo.Title?.EndsWith(javVideo.Number, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();
            javVideo.Title = RemoveActorsFromTitle(javVideo.Title, javVideo.Actors);
            javVideo.OriginalTitle = RemoveActorsFromTitle(javVideo.OriginalTitle, javVideo.Actors);
            return javVideo;
        }

        public override Task<JavVideo> ParsePage(string url)
        {
            throw new NotImplementedException();
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

        //public override bool CheckKeyword(string keyword)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
