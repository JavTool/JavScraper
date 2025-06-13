using HtmlAgilityPack;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// JavDB 无码专用刮削器
    /// </summary>
    public class JavUncensoredScraper : AbstractScraper
    {
        /// <summary>
        /// 适配器名称
        /// </summary>
        public string Name => "JavDB";

        /// <summary>
        /// 番号分段识别。
        /// </summary>
        private static Regex regex = new Regex("((?<a>[a-z]{2,})|(?<b>[0-9]{2,}))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// HTTP客户端处理器
        /// </summary>
        private readonly HttpClientHandler Handler;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        public JavUncensoredScraper(ILoggerFactory loggerFactory)
           : base("https://javdb.com/", loggerFactory.CreateLogger<JavUncensoredScraper>())
        {
            // 初始化HTTP客户端处理器
            Handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            // 添加over18 cookie
            var cookie = new Cookie("over18", "1", "/", ".javdb.com");
            Handler.CookieContainer.Add(cookie);
        }

        public override Task<List<JavVideo>> ParseList(string url)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 解析列表。
        /// </summary>
        /// <param name="ls"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        protected List<JavVideo> ParseIndex(List<JavVideo> ls, HtmlDocument doc)
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
                var m = new JavVideo() { Url = url };
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
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool CheckKeyword(string key) => JavIdRecognizer.FC2(key) == null;
        /// <summary>
        /// 获取列表。
        /// </summary>
        /// <param name="key">关键字。</param>
        /// <returns></returns>
        public async Task<List<JavVideo>> Query(string key)
        {
            var ls = new List<JavVideo>();
            if (CheckKeyword(key) == false)
                return ls;
            var keys = GetAllKeys(key);
            foreach (var k in keys)
            {
                await Search(ls, k);
                if (ls.Any())
                    return ls;
            }
            return ls;
        }


        public async Task<List<JavVideo>> Search(List<JavVideo> ls, string number)
        {
            var doc = await GetHtmlDocumentAsync($"/search?q={number}&f=all");
            if (doc != null)
                ParseIndex(ls, doc);

            if (ls.Any())
            {
                var ks = regex.Matches(number).Cast<Match>()
                     .Select(o => o.Groups[0].Value).ToList();

                ls.RemoveAll(i =>
                {
                    foreach (var k in ks)
                    {
                        if (i.Number?.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) //包含，则继续
                            continue;
                        if (k[0] != '0') //第一个不是0，则不用继续了。
                            return true;//移除

                        var k2 = k.TrimStart('0');
                        if (i.Number?.IndexOf(k2, StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;
                        return true; //移除
                    }
                    return false; //保留
                });
            }

            SortIndex(number, ls);
            return ls;
        }

        public async Task<JavVideo> SearchAndParseJavVideo(string javId)
        {
            // https://javdb.com/search?q=080916_356&f=all
            var searchUrl = $"https://javdb.com/search?q={javId}&f=all";
            var doc = await GetHtmlDocumentAsync(searchUrl);
            if (doc == null)
                return null;

            var videoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'movie-list')]/div/a");
            if (videoNode == null)
                return null;

            var videoUrl = videoNode.GetAttributeValue("href", null);
            var javVideo = await ParsePage(videoUrl);
            return javVideo;
        }

        public override async Task<JavVideo> ParsePage(string url)
        {
            //https://javdb.com/v/BzbA6
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
                var v = GetValue("演員");
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
                Title = $"{doc.DocumentNode.SelectSingleNode("//*[contains(@class,'title')]/strong")?.InnerText?.Trim()} {doc.DocumentNode.SelectSingleNode("//*[contains(@class,'current-title')]")?.InnerText?.Trim()}",
                Cover = GetCover(),
                Number = GetValue("番號"),
                Date = GetValue("日期"),
                Runtime = GetValue("時長"),
                Maker = GetValue("片商"),
                Studio = GetValue("發行"),
                Set = GetValue("系列"),
                Director = GetValue("導演"),
                Genres = GetGenres(),
                Actors = GetActors(),
                Samples = GetSamples(),
            };

            // 去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Number) == false && javVideo.Title?.StartsWith(javVideo.Number, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();

            return javVideo;
        }

        /// <summary>
        /// 获取 Caribbeancom 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetCaribbeanMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Maker?.Contains("カリビアンコム", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 1Pondo 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> Get1PondoMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Maker?.Contains("一本道", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 Pacopacomama 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetPacopacomamaMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Maker?.Contains("パコパコママ", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 CaribbeancomPR 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetCaribbeancomPRMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Maker?.Contains("カリビアンコムプレミアム", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 Heyzo 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetHeyzoMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Maker?.Contains("HEYZO", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 AVE 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetAVEMetadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Number?.StartsWith("AVE", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }

        /// <summary>
        /// 获取 FC2 元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        public async Task<JavVideo> GetFC2Metadata(string javId)
        {
            var searchResults = await Query(javId);
            var video = searchResults?.FirstOrDefault(v => 
                v.Number?.Equals(javId, StringComparison.OrdinalIgnoreCase) == true ||
                v.Number?.StartsWith("FC2", StringComparison.OrdinalIgnoreCase) == true);
            
            if (video?.Url != null)
            {
                return await ParsePage(video.Url);
            }
            
            return null;
        }
    }
}
