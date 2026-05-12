using AngleSharp;
using AngleSharp.Io;
using HtmlAgilityPack;
using JavScraper.Scrapers;
using JavScraper.Tools.Entities;
using JavScraper.Tools.Http;
using JavScraper.Tools.Tools;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace JavScraper.Tools.Scrapers
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

                var plot = doc.DocumentNode.SelectSingleNode("//div[@class='mg-b20 lh4']/p[@class='mg-b20']")?.InnerText?.Trim();

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

        #region 处理无码...

        public virtual async Task<JavVideo> Get1PondoMetadata(string number)
        {

            using (await locker.LockAsync(number))
            {
                var jsonUrl = $"https://www.1pondo.tv/dyn/phpauto/movie_details/movie_id/{number}.json";
                var url = $"https://www.1pondo.tv/movies/{number}/";
                var doc = await GetRenderedHtmlByPlaywrightAsync(url);

                if (doc == null)
                    return null;


                var sampleNodes = doc.DocumentNode.SelectNodes("//img[contains(@class,'gallery-image pointer')]");
                if (sampleNodes?.Any() != true)
                    return null;

                var json = await GetStringAsync(jsonUrl);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var video = JsonSerializer.Deserialize<OnePondoVideo>(json, options);

                async Task<String> GetPoster()
                {
                    // https://www.1pondo.tv/moviepages/061025_001/images/str.jpg
                    string posterUrl = $"https://www.1pondo.tv/moviepages/{number}/images/str.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                    {
                        return posterUrl;
                    }
                    return String.Empty;
                }
                async Task<string> GetJacket()
                {
                    string jacketUrl = $"https://www.1pondo.tv/dyn/dla/images/movies/{number}/jacket/jacket.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(jacketUrl))
                    {
                        return jacketUrl;
                    }
                    return string.Empty;
                }
                async Task<List<string>> GetSamples()
                {

                    // https://www.1pondo.tv/assets/sample/031216_261/popu/1.jpg
                    List<string> samples = new List<string>();

                    var jacketUrl = await GetJacket();
                    if (!String.IsNullOrEmpty(jacketUrl))
                    {
                        samples.Add(jacketUrl);
                    }

                    var posterUrl = await GetPoster();
                    if (!String.IsNullOrEmpty(posterUrl))
                    {
                        samples.Add(posterUrl);
                    }
                    foreach (var sampleNode in sampleNodes)
                    {

                        var sample = sampleNode.GetAttributeValue("data-vue-img-src", "");
                        if (sample != null)
                        {
                            samples.Add($"https://www.1pondo.tv/{sample}");
                        }
                    }

                    return samples;
                }



                // 支持动态匹配主流分辨率与帧率
                var regex = new Regex(@"\b(?:[1-9]\d{2,}p|4k|24|30|50|60|120|240|29\.97|\d+\.?\d*fps)\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    Title = video.Title,
                    Cover = video.ThumbHigh,
                    Number = video.MovieID,
                    Date = video.Release,
                    Runtime = video.Duration.ToString(),
                    Plot = video.Desc,
                    //Maker = GetValue("片商"),   
                    //Studio = GetValue("發行"),
                    //Set = GetValue("系列"),
                    //Director = GetValue("導演"),
                    //Fanart = $"https://www.1pondo.tv/dyn/dla/images/movies/{number}/jacket/jacket.jpg",
                    //Poster = GetPoster(),
                    //Actors = GetActors(),
                    Samples = await GetSamples(),
                };
                if (video.ActressesList != null && video.ActressesList.Count > 0)
                {
                    javVideo.Actors = video.ActressesList.Select(a => a.Value.NameJa).ToList();
                }
                if (video.UCNAME != null && video.UCNAME.Count > 0)
                {
                    javVideo.Genres = video.UCNAME;
                    javVideo.Genres.RemoveAll(tag => regex.IsMatch(tag));
                }


                return javVideo;
            }
        }

        /// <summary>
        /// 获取 CaribbeanPR 的元数据
        /// </summary>
        /// <param name="number">番号。</param>
        public virtual async Task<JavVideo> GetCaribbeanPRMetadata(string number)
        {

            using (await locker.LockAsync(number))
            {
                // https://www.caribbeancom.com/moviepages/090215-962/index.html
                var url = $"https://www.caribbeancompr.com/moviepages/{number}/index.html";
                var doc = await GetRenderedHtmlAsync(url);

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//div[@class='section is-wide']/p")?.InnerText?.Trim();
                var title = doc.DocumentNode.SelectSingleNode("//div[@class='heading']/h1")?.InnerText?.Trim();
                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'section is-wide')]/ul/li");
                if (nodes?.Any() != true)
                    return null;
                var dic = new Dictionary<string, string>();
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
                    if (k?.Contains("タグ") == true)
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

                string GetValue(string _key)
                    => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();
                List<string> GetGenres()
                {
                    var v = GetValue("タグ");
                    if (string.IsNullOrWhiteSpace(v))
                        return null;
                    return v.Replace("\t", "").Replace("\n", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
                }
                List<string> GetActors()
                {
                    var v = GetValue("出演");
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
                async Task<string> GetCover()
                {
                    string posterUrl = $"https://www.caribbeancompr.com/moviepages/{number}/images/jacket.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                    {
                        return posterUrl;
                    }
                    else
                    {
                        posterUrl = $"https://www.caribbeancompr.com/moviepages/{number}/images/main_s.jpg";
                        if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                        {
                            return posterUrl;
                        }
                    }
                    // 获取背景图片
                    var posterNode = doc.DocumentNode.SelectSingleNode("//div[@class='vjs-poster']");
                    if (posterNode != null)
                    {
                        var style = posterNode.GetAttributeValue("style", "");
                        var match = Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
                        if (match.Success)
                        {
                            var backgroundUrl = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value).Replace("\"", "");
                            // 处理相对路径，转换为完整URL
                            if (backgroundUrl.StartsWith("/"))
                            {
                                backgroundUrl = $"https://www.caribbeancompr.com{backgroundUrl}";
                            }
                            return backgroundUrl;
                        }
                    }
                    return null;
                }

                async Task<string> GetJacket()
                {
                    string jacketUrl = $"https://www.caribbeancompr.com/moviepages/{number}/images/jacket.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(jacketUrl))
                    {
                        return jacketUrl;
                    }
                    return string.Empty;
                }

                async Task<String> GetPoster()
                {
                    string posterUrl = $"https://www.caribbeancompr.com/moviepages/{number}/images/main_s.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                    {
                        return posterUrl;
                    }

                    return String.Empty;
                }

                async Task<List<string>> GetSamples()
                {

                    // https://www.caribbeancompr.com/moviepages/050115_195/images/l/001.jpg
                    List<string> samples = new List<string>();
                    var jacketUrl = await GetJacket();
                    if (!String.IsNullOrEmpty(jacketUrl))
                    {
                        samples.Add(jacketUrl);
                    }

                    var posterUrl = await GetPoster();
                    if (!String.IsNullOrEmpty(posterUrl))
                    {
                        samples.Add(posterUrl);
                    }
                    for (int i = 1; i <= 3; i++)
                    {
                        samples.Add($"https://www.caribbeancompr.com/moviepages/{number}/images/l/00{i}.jpg");
                    }
                    return samples;
                }
                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    Title = title,
                    Cover = await GetCover(),
                    Plot = plot,
                    //Number = GetValue("番號"),
                    //Date = GetValue("日期"),
                    //Runtime = GetValue("時長"),
                    //Maker = GetValue("片商"),
                    Studio = GetValue("スタジオ"),
                    //Set = GetValue("系列"),
                    //Director = GetValue("導演"),
                    Genres = GetGenres(),
                    Actors = GetActors(),
                    Samples = await GetSamples(),
                };
                return javVideo;
            }
        }

        /// <summary>
        /// 获取 Caribbean 的元数据
        /// </summary>
        /// <param name="number">番号。</param>
        public virtual async Task<JavVideo> GetCaribbeanMetadata(string number)
        {

            using (await locker.LockAsync(number))
            {
                // https://www.caribbeancom.com/moviepages/090215-962/index.html
                var url = $"https://www.caribbeancom.com/moviepages/{number}/index.html";

                // 注入年龄验证 Cookie 绕过弹窗
                var cookies = new[]
                {
                    new Microsoft.Playwright.Cookie { Name = "cmp", Value = "1", Domain = ".caribbeancom.com", Path = "/" },
                    new Microsoft.Playwright.Cookie { Name = "age_verification", Value = "1", Domain = ".caribbeancom.com", Path = "/" },
                };
                var doc = await GetRenderedHtmlWithCookiesAsync(url, cookies, waitForSelector: "div.movie-info", timeoutMs: 30000);

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//p[@itemprop='description']")?.InnerText?.Trim();
                var title = doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText?.Trim();
                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'movie-info')]/ul/li");
                if (nodes?.Any() != true)
                    return null;
                var dic = new Dictionary<string, string>();
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

                string GetValue(string _key)
                    => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();
                List<string> GetGenres()
                {
                    var v = GetValue("タグ");
                    if (string.IsNullOrWhiteSpace(v))
                        return null;
                    return v.Replace("\t", "").Replace("\n", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
                }
                List<string> GetActors()
                {
                    var v = GetValue("出演");
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

                async Task<String> GetPoster()
                {
                    string posterUrl = $"https://www.caribbeancom.com/moviepages/{number}/images/l_l.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                    {
                        return posterUrl;
                    }
                    return String.Empty;
                }



                async Task<string> GetJacket()
                {
                    string jacketUrl = $"https://www.caribbeancom.com/moviepages/{number}/images/jacket.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(jacketUrl))
                    {
                        return jacketUrl;
                    }
                    return string.Empty;
                }

                async Task<string> GetCover()
                {
                    // 获取背景图片
                    var posterNode = doc.DocumentNode.SelectSingleNode("//div[@class='vjs-poster']");
                    if (posterNode != null)
                    {
                        var style = posterNode.GetAttributeValue("style", "");
                        var match = Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
                        if (match.Success)
                        {
                            var backgroundUrl = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value).Replace("\"", "");
                            // 处理相对路径，转换为完整URL
                            if (backgroundUrl.StartsWith("/"))
                            {
                                backgroundUrl = $"https://www.caribbeancom.com{backgroundUrl}";
                            }
                            return backgroundUrl;
                        }
                    }
                    return null;
                }

                async Task<List<string>> GetSamples()
                {
                    // https://www.caribbeancom.com/moviepages/111415-022/images/l/001.jpg
                    List<string> samples = new List<string>();
                    var jacketUrl = await GetJacket();
                    if (!String.IsNullOrEmpty(jacketUrl))
                    {
                        samples.Add(jacketUrl);
                    }

                    var posterUrl = await GetPoster();
                    if (!String.IsNullOrEmpty(posterUrl))
                    {
                        samples.Add(posterUrl);
                    }

                    for (int i = 1; i <= 4; i++)
                    {
                        samples.Add($"https://www.caribbeancom.com/moviepages/{number}/images/l/00{i}.jpg");
                    }
                    return samples;
                }

                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    //Title = $"{doc.DocumentNode.SelectSingleNode("//*[contains(@class,'title')]/strong")?.InnerText?.Trim()} {doc.DocumentNode.SelectSingleNode("//*[contains(@class,'current-title')]")?.InnerText?.Trim()}",
                    Cover = await GetCover(),
                    //Number = GetValue("番號"),
                    //Date = GetValue("日期"),
                    //Runtime = GetValue("時長"),
                    //Maker = GetValue("片商"),
                    //Studio = GetValue("發行"),
                    //Set = GetValue("系列"),
                    //Director = GetValue("導演"),
                    Genres = GetGenres(),
                    Actors = GetActors(),
                    Samples = await GetSamples(),
                };
                javVideo.Plot = plot;
                javVideo.Actors = GetActors();
                javVideo.Title = title;
                javVideo.Genres = GetGenres();

                return javVideo;
            }
        }


        /// <summary>
        /// 获取 Caribbean 的元数据
        /// </summary>
        /// <param name="number">番号。</param>
        public virtual async Task<JavVideo> GetPacopacomamaMetadata(string number)
        {

            using (await locker.LockAsync(number))
            {
                // https://www.pacopacomama.com/moviepages/090215-962/index.html
                var url = $"https://www.pacopacomama.com/moviepages/{number}/index.html";
                var doc = await GetRenderedHtmlByPlaywrightAsync(url);

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//p[@class='movie-description']")?.InnerText?.Trim();
                var title = doc.DocumentNode.SelectSingleNode("//div[@class='movie-title']/h1")?.InnerText?.Trim();
                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'movie-info')]/ul/li");
                if (nodes?.Any() != true)
                    return null;
                var dic = new Dictionary<string, string>();
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
                    if (k?.Contains("タグ") == true)
                    {
                        var ac = n.SelectNodes("./*[@class='spec-content']/span/a");
                        if (ac?.Any() == true)
                            v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                    }
                    if (v == null)
                        v = n.SelectSingleNode("./*[@class='spec-content']")?.InnerText?.Trim().Replace("&nbsp;", " ");

                    if (string.IsNullOrWhiteSpace(k) == false && string.IsNullOrWhiteSpace(v) == false)
                        dic[k] = v;
                }

                string GetValue(string _key)
                    => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();
                List<string> GetGenres()
                {
                    var v = GetValue("タグ");
                    if (string.IsNullOrWhiteSpace(v))
                        return null;
                    return v.Replace("\t", "").Replace("\n", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
                }
                List<string> GetActors()
                {
                    var v = GetValue("出演");
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



                async Task<string> GetJacket()
                {
                    string jacketUrl = $"https://www.pacopacomama.com/moviepages/{number}/images/jacket.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(jacketUrl))
                    {
                        return jacketUrl;
                    }
                    return string.Empty;
                }

                async Task<String> GetPoster()
                {
                    string posterUrl = $"https://www.pacopacomama.com/moviepages/{number}/images/l_hd.jpg";
                    if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                    {
                        return posterUrl;
                    }
                    return String.Empty;
                }

                async Task<List<string>> GetSamples()
                {
                    // https://www.pacopacomama.com/moviepages/111415-022/images/l/001.jpg
                    List<string> samples = new List<string>();
                    var jacketUrl = await GetJacket();
                    if (!String.IsNullOrEmpty(jacketUrl))
                    {
                        samples.Add(jacketUrl);
                    }

                    var posterUrl = await GetPoster();
                    if (!String.IsNullOrEmpty(posterUrl))
                    {
                        samples.Add(posterUrl);
                    }
                    for (int i = 1; i <= 3; i++)
                    {
                        samples.Add($"https://www.pacopacomama.com/assets/sample/{number}/l/{i}.jpg");
                    }
                    return samples;
                }

                string GetCover()
                {
                    // 获取背景图片
                    var posterNode = doc.DocumentNode.SelectSingleNode("//div[@class='vjs-poster']");
                    if (posterNode != null)
                    {
                        var style = posterNode.GetAttributeValue("style", "");
                        var match = Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
                        if (match.Success)
                        {
                            var backgroundUrl = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value).Replace("\"", "");
                            // 处理相对路径，转换为完整URL
                            if (backgroundUrl.StartsWith("/"))
                            {
                                backgroundUrl = $"https://www.pacopacomama.com{backgroundUrl}";
                            }
                            return backgroundUrl;
                        }
                    }
                    return null;
                }

                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    Title = title,
                    Cover = GetCover(),
                    Plot = plot,
                    //Number = GetValue("番號"),
                    //Date = GetValue("日期"),
                    //Runtime = GetValue("時長"),
                    //Maker = GetValue("片商"),
                    //Studio = GetValue("發行"),
                    //Set = GetValue("系列"),
                    //Director = GetValue("導演"),
                    Genres = GetGenres(),
                    Actors = GetActors(),
                    Samples = await GetSamples(),
                };

                return javVideo;
            }
        }

        /// <summary>
        /// 获取 Heyzo 的元数据
        /// </summary>
        /// <param name="number">番号。</param>
        /// <returns></returns>
        public virtual async Task<JavVideo> GetHeyzoMetadata(string number)
        {

            using (await locker.LockAsync(number))
            {
                // https://www.heyzo.com/moviepages/0852/index.html
                // https://www.heyzo.com/moviepages/3564/index.html
                var heyzoId = number.Replace("heyzo-", "", StringComparison.OrdinalIgnoreCase);
                var url = $"https://www.heyzo.com/moviepages/{heyzoId}/index.html";

                // heyzo 年龄验证：先访问首页点击确认按钮获取 cookie，再访问目标页
                HtmlDocument doc = null;
                try
                {
                    var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        ExecutablePath = File.Exists(BrowserExecutablePath) ? BrowserExecutablePath : null,
                        Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" },
                    });
                    var context = await browser.NewContextAsync(new BrowserNewContextOptions
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                        ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                    });
                    await context.AddInitScriptAsync(
                        "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })");

                    var page = await context.NewPageAsync();

                    // 第一步：访问首页，触发年龄验证弹窗并点击「はい」
                    await page.GotoAsync("https://www.heyzo.com/", new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 20000,
                    });
                    // 尝试点击年龄确认按钮（selector 可能是 a.btn_yes 或 #agecheck a）
                    try
                    {
                        var yesBtn = page.Locator("a.btn_yes, #agecheck a, a[href*='agecheck']").First;
                        await yesBtn.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    }
                    catch { /* 没有弹窗或已通过验证，忽略 */ }

                    // 第二步：带有已写入的 cookie 访问目标页
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000,
                    });
                    try
                    {
                        await page.WaitForSelectorAsync("div#movie", new PageWaitForSelectorOptions { Timeout = 15000 });
                    }
                    catch { /* 超时则直接读取内容 */ }

                    var content = await page.ContentAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        doc = new HtmlDocument();
                        doc.LoadHtml(content);
                    }
                }
                catch (Exception ex)
                {
                    log?.LogError($"[Heyzo] Playwright 请求失败: {url}, 错误: {ex.Message}");
                }

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//p[@class='memo']")?.InnerText?.Trim();
                var title = doc.DocumentNode.SelectSingleNode("//div[@id='movie']/h1")?.InnerText?.Trim().Replace("\t", "").Replace("\n", "");


                List<string> GetGenres()
                {
                    var tagKeywordNodes = doc.DocumentNode.SelectNodes("//ul[@class='tag-keyword-list']/li");
                    if (tagKeywordNodes != null && tagKeywordNodes.Count() > 0)
                    {
                        return tagKeywordNodes.Select(n => n.InnerText).ToList();
                    }
                    return new List<string>();
                }

                List<string> GetActors()
                {
                    var actorNodes = doc.DocumentNode.SelectNodes("//tr[@class='table-actor']/td/a");
                    if (actorNodes != null && actorNodes.Count() > 0)
                    {
                        return actorNodes.Select(n => n.InnerText).ToList();
                    }
                    return new List<string>();
                }

                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'movie-info')]/ul/li");

                string GetDate()
                {
                    return doc.DocumentNode.SelectSingleNode("//tr[@class='table-release-day']/td[2]").InnerText?.Trim();
                }

                List<string> GetSamples()
                {
                    // https://www.heyzo.com/contents/3000/1147/gallery/005.jpg
                    List<string> samples = new List<string>();
                    for (int i = 1; i <= 5; i++)
                    {
                        samples.Add($"https://www.heyzo.com/contents/3000/{heyzoId}/gallery/00{i}.jpg");
                    }
                    return samples;
                }


                // 获取封面图片
                string GetCover()
                {
                    // https://www.heyzo.com/contents/3000/0273/images/thumbnail.jpg
                    // 获取背景图片
                    var posterNode = doc.DocumentNode.SelectSingleNode("//div[@class='vjs-poster']");
                    if (posterNode != null)
                    {
                        var style = posterNode.GetAttributeValue("style", "");
                        var match = Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
                        if (match.Success)
                        {
                            var backgroundUrl = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value).Replace("\"", "");
                            // 处理相对路径，转换为完整URL
                            if (backgroundUrl.StartsWith("/"))
                            {
                                backgroundUrl = $"https:{backgroundUrl}";
                            }
                            return backgroundUrl;
                        }
                    }
                    return null;
                }

                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    Title = title,
                    Cover = GetCover(),
                    Number = number.ToUpper(),
                    Date = GetDate(),
                    //Runtime = GetValue("時長"),
                    Maker = "HEYZO",
                    Studio = "HEYZO",
                    //Set = "HEYZO",
                    //Director = GetValue("導演"),
                    Genres = GetGenres(),
                    Actors = GetActors(),
                    Samples = GetSamples(),
                };
                javVideo.Plot = plot;
                javVideo.Actors = GetActors();
                javVideo.Title = title;
                javVideo.Genres = GetGenres();
                return javVideo;
            }
        }

        /// <summary>
        /// 判断是否为 AVE 番号前缀，并返回标准前缀（短写），找不到返回 null
        /// </summary>
        public static string GetAveShortPrefix(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            code = code.ToUpper().Trim();

            foreach (var kv in PrefixMap)
            {
                if (code.StartsWith(kv.Key + "-") || code.StartsWith(kv.Key))
                    return kv.Value;
            }

            return null;
        }

        /// <summary>
        /// 是否属于 AVE 系列番号
        /// </summary>
        public static bool IsAveNumber(string code)
        {
            return GetAveShortPrefix(code) != null;
        }
        private static readonly Dictionary<string, string> PrefixMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CWPBD", "CWP" },
            { "SMBD", "SMD" },
            { "SMD", "SMD" },
            { "MCB3DBD", "MCB" },
            { "MCB", "MCB" },
            { "LAFBD", "LAF" },
            { "DLLAFBD", "LAF" },
            { "BT", "BT" },
            { "DLBT", "BT" },
            { "DLPT", "PT" },
            { "DLSMD", "SMD" },
            { "DLSMBD", "SMD" }
        };

        /// <summary>
        /// 将长番号标准化为短番号，例如 CWPBD-104 → CWP-104
        /// </summary>
        public static string GetAVEShortNumber(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return code;

            code = code.Trim().ToUpper();

            foreach (var kv in PrefixMap)
            {
                string longPrefix = kv.Key;

                if (code.StartsWith(longPrefix))
                {
                    // 提取后缀编号部分（支持带/不带 "-"）
                    string number = code.Substring(longPrefix.Length).TrimStart('-');
                    return $"{kv.Value}-{number}";
                }
            }

            // 默认返回原始番号
            return code;
        }
        private async Task<string> GetAVESearchAsync(string number)
        {
            // https://www.aventertainments.com/dvd/search?lang=2&cat=29&culture=ja-JP&keyword=CWP-152&searchby=keyword&page=1
            var searchUrl = $"https://www.aventertainments.com/dvd/search?lang=2&cat=29&culture=ja-JP&keyword={number}&searchby=keyword&page=1";
            // AVE 是 SPA，内容由 JS 异步渲染，必须等待搜索结果容器出现
            var doc = await GetRenderedHtmlByPlaywrightAsync(
                searchUrl,
                waitForSelector: "div.single-slider-product__image",
                timeoutMs: 45000);
            if (doc == null)
                return null;

            var productNodes = doc.DocumentNode.SelectNodes("//div[@class='single-slider-product__image']");

            if (productNodes == null || productNodes.Count == 0)
                return null;

            foreach (var node in productNodes)
            {
                var aTag = node.SelectSingleNode(".//a");
                var imgTag = node.SelectSingleNode(".//img");

                if (imgTag != null && aTag != null)
                {
                    var src = imgTag.GetAttributeValue("src", "");
                    var href = aTag.GetAttributeValue("href", "");

                    // 提取 src 最后一段文件名进行匹配
                    var fileName = Path.GetFileName(src); // eg: dvd1bt-177.webp

                    if (!string.IsNullOrEmpty(fileName) && fileName.ToLower().Contains(number.ToLower()))
                    {
                        return href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? href.Replace("&amp;", "&")
                            : "https://www.aventertainments.com" + href;
                    }
                }
            }
            return null;
        }
        public static string EUCJPEncodeKeyword(string keyword)
        {
            // 注册 Shift_JIS 编码支持 (.NET Core / .NET 5+)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 使用 Code Page 932（Windows-31J，也就是 Shift_JIS）
            Encoding eucJp = Encoding.GetEncoding(51932); // 或 "shift_jis"

            byte[] bytes = eucJp.GetBytes(keyword);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append('%');
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
        public async Task<JavVideo> SearchVideoAsync(string keyword, string title)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // 1. 构造搜索链接
            // 使用 Shift_JIS 编码
            //Encoding shiftJis = Encoding.GetEncoding("shift_jis");
            //string encodedKeyword = HttpUtility.UrlEncode(shiftJis.GetBytes(keyword));
            // 正确方式：传入字符串 + 指定编码
            string encodedKeyword = EUCJPEncodeKeyword(keyword);
            string searchUrl = $"https://www.caribbeancompr.com/search/?q={encodedKeyword}";
            Console.WriteLine(searchUrl);
            // 2. 获取搜索结果页面
            var searchDoc = await GetRenderedHtmlAsync(searchUrl);
            if (searchDoc == null)
                return null;

            // 3. 查找匹配的标题
            var gridItems = searchDoc.DocumentNode.SelectNodes("//div[@class='grid-item']");
            if (gridItems != null)
            {
                foreach (var item in gridItems)
                {
                    var titleNode = item.SelectSingleNode(".//a[@class='meta-title']");
                    var videoTitle = titleNode?.InnerText?.Trim();
                    var matched = false;
                    if (videoTitle.Length < title.Length)
                    {
                        matched = JavVideoHelper.IsTitleMatch(videoTitle, title, keyword);
                    }
                    else
                    {
                        matched = JavVideoHelper.IsTitleMatch(title, videoTitle, keyword);
                    }
                    // 相似度计算，默认 90% 的相似度阈值
                    if (JavVideoHelper.IsTitleMatch(title, videoTitle, keyword) || matched)
                    {
                        //    videoTitle = nfoVideoInfo.Title;
                        //}
                        //if (titleNode != null && title.Contains(titleNode.InnerText.Trim()))
                        //{
                        var hrefNode = item.SelectSingleNode(".//div[@class='media-thum']/a");
                        if (hrefNode != null)
                        {
                            string relativeUrl = hrefNode.GetAttributeValue("href", "");
                            string videoUrl = $"https://www.caribbeancompr.com{relativeUrl}";

                            // 4. 获取视频页面内容
                            var videoDoc = await GetRenderedHtmlAsync(videoUrl);
                            if (videoDoc == null)
                                return null;

                            var plot = videoDoc.DocumentNode.SelectSingleNode("//div[@class='section is-wide']/p")?.InnerText?.Trim();
                            title = videoDoc.DocumentNode.SelectSingleNode("//div[@class='heading']/h1")?.InnerText?.Trim();
                            var nodes = videoDoc.DocumentNode.SelectNodes("//div[contains(@class,'section is-wide')]/ul/li");
                            if (nodes?.Any() != true)
                                return null;
                            var dic = new Dictionary<string, string>();
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
                                if (k?.Contains("タグ") == true)
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

                            string GetValue(string _key)
                                => dic.Where(o => o.Key.Contains(_key)).Select(o => o.Value).FirstOrDefault();
                            List<string> GetGenres()
                            {
                                var v = GetValue("タグ");
                                if (string.IsNullOrWhiteSpace(v))
                                    return null;
                                return v.Replace("\t", "").Replace("\n", ",").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
                            }
                            List<string> GetActors()
                            {
                                var v = GetValue("出演");
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
                            string GetCover()
                            {
                                // 获取背景图片
                                var posterNode = videoDoc.DocumentNode.SelectSingleNode("//div[@class='vjs-poster']");
                                if (posterNode != null)
                                {
                                    var style = posterNode.GetAttributeValue("style", "");
                                    var match = Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
                                    if (match.Success)
                                    {
                                        var backgroundUrl = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value).Replace("\"", "");
                                        // 处理相对路径，转换为完整URL
                                        if (backgroundUrl.StartsWith("/"))
                                        {
                                            backgroundUrl = $"https://www.caribbeancompr.com{backgroundUrl}";
                                        }
                                        return backgroundUrl;
                                    }
                                }
                                return null;
                            }
                            string ExtractNumberFromUrl(string url)
                            {
                                var match = Regex.Match(url, @"\/moviepages\/([^\/]+)\/index\.html");
                                if (match.Success)
                                {
                                    return match.Groups[1].Value;
                                }
                                return string.Empty;
                            }
                            var number = ExtractNumberFromUrl(videoUrl);
                            async Task<String> GetPoster()
                            {
                                // https://www.caribbeancompr.com/moviepages/041319_002/images/main_b.jpg
                                string posterUrl = $"https://www.caribbeancompr.com/moviepages/{number}/images/main_b.jpg";
                                if (await HttpUtils.IsUrlAvailableAsync(posterUrl))
                                {
                                    return posterUrl;
                                }
                                return String.Empty;
                            }
                            async Task<List<string>> GetSamples()
                            {

                                // https://www.caribbeancompr.com/moviepages/050115_195/images/l/001.jpg
                                List<string> samples = new List<string>();
                                var posterUrl = await GetPoster();
                                if (!String.IsNullOrEmpty(posterUrl))
                                {
                                    samples.Add(posterUrl);
                                }
                                for (int i = 1; i <= 3; i++)
                                {
                                    samples.Add($"https://www.caribbeancompr.com/moviepages/{number}/images/l/00{i}.jpg");
                                }
                                return samples;
                            }
                            var javVideo = new JavVideo()
                            {
                                //Provider = Name,
                                Url = videoUrl,
                                Title = title,
                                Cover = GetCover(),
                                Plot = plot,
                                //Number = GetValue("番號"),
                                //Date = GetValue("日期"),
                                //Runtime = GetValue("時長"),
                                //Maker = GetValue("片商"),
                                Studio = GetValue("スタジオ"),
                                //Set = GetValue("系列"),
                                //Director = GetValue("導演"),
                                Genres = GetGenres(),
                                Actors = GetActors(),
                                Samples = await GetSamples(),
                            };
                            Console.WriteLine($"成功从 CaribbeancomPR 获取番号 {number} 的数据");
                            return javVideo;
                        }
                    }
                }
            }
            // 如果未找到匹配的标题
            return null;
        }

        /// <summary>
        /// 获取 AV Entertainment 的元数据
        /// 搜索地址：https://www.aventertainments.com/search_Products.aspx?languageID=2&dept_id=29&keyword=BT-177&searchby=keyword
        /// </summary>
        /// <param name="number">番号。</param>
        public virtual async Task<JavVideo> GetAVEMetadata(string number)
        {
            using (await locker.LockAsync(number))
            {
                var aveShortNumber = GetAVEShortNumber(number);
                // https://www.aventertainments.com/product_lists.aspx?product_id={number}
                var url = await GetAVESearchAsync(aveShortNumber);
                if (string.IsNullOrEmpty(url))
                    return null;
                // AVE 详情页也是 SPA，等待内容区加载完成
                var doc = await GetRenderedHtmlByPlaywrightAsync(
                    url,
                    waitForSelector: "div.product-description",
                    timeoutMs: 45000);

                if (doc == null)
                    return null;

                var plot = doc.DocumentNode.SelectSingleNode("//div[@class='product-description mt-20']")?.InnerText?.Trim();
                var title = doc.DocumentNode.SelectSingleNode("//div[@class='section-title']/h3")?.InnerText?.Trim();
                var cover = doc.DocumentNode.SelectSingleNode("//img[@class='mejs__poster-img']")?.GetAttributeValue("src", "");

                var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'single-info')]");
                if (nodes?.Any() != true)
                    return null;
                var dic = new Dictionary<string, string>();
                foreach (var n in nodes)
                {
                    var k = n.SelectSingleNode("./span")?.InnerText?.Trim();
                    string v = null;
                    if (k?.Contains("主演女優") == true)
                    {
                        var ac = n.SelectNodes("./*[@class='value']/a");
                        if (ac?.Any() == true)
                            v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                    }
                    if (k?.Contains("商品番号") == true)
                    {
                        var ac = n.SelectNodes("./*[@class='tag-title']");
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

                // 获取封面图片
                List<string> GetSamples()
                {
                    var samplesNodes = doc.DocumentNode.SelectNodes("//a[@class='lightbox']/img");
                    if (samplesNodes != null && samplesNodes.Count > 0)
                    {
                        return samplesNodes.Select(s => s.GetAttributeValue("src", "")).ToList();
                    }
                    return new List<string>();
                }
                // 获取演员
                List<string> GetActors()
                {
                    var v = GetValue("主演女優");
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
                // 获取类型
                List<string> GetGenres()
                {
                    var tagKeywordNodes = doc.DocumentNode.SelectNodes("//span[@class='value-category']/a");
                    if (tagKeywordNodes != null && tagKeywordNodes.Count() > 0)
                    {
                        return tagKeywordNodes.Select(n => n.InnerText).Where(a => a.Length < 15).ToList();
                    }
                    return new List<string>();
                }
                // 获取日期
                string GetDate()
                {
                    var v = GetValue("発売日");
                    if (string.IsNullOrWhiteSpace(v))
                        return null;

                    // 去除括号及内容，如 "11/27/2019 (発売中)" => "11/27/2019"
                    string cleaned = Regex.Replace(v, @"\s*\(.*?\)", "").Trim();

                    // 解析为 DateTime
                    if (DateTime.TryParseExact(cleaned, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        return date.ToString("yyyy-MM-dd");
                    }

                    return null;
                }
                string GetRuntime()
                {
                    string v = GetValue("収録時間");

                    if (string.IsNullOrWhiteSpace(v))
                        return null;

                    var match = Regex.Match(v, @"\d+");

                    if (match.Success && int.TryParse(match.Value, out int minutes))
                    {
                        return minutes.ToString();
                    }

                    return null;
                }
                var javVideo = new JavVideo()
                {
                    //Provider = Name,
                    Url = url,
                    Title = title,
                    Cover = cover,
                    Plot = plot,
                    Number = number.ToUpper(),
                    Date = GetDate(),
                    Runtime = GetRuntime(),
                    //Maker = GetValue("導演"),
                    Studio = GetValue("スタジオ"),
                    Set = GetValue("シリーズ"),
                    //Director = GetValue("導演"),
                    Genres = GetGenres(),
                    Actors = GetActors(),
                    Samples = GetSamples(),
                };
                var searchResult = await SearchVideoAsync(javVideo.Actors[0], title);
                if (searchResult != null)
                {
                    javVideo.Title = searchResult.Title;
                    javVideo.Plot = searchResult.Plot;
                    javVideo.Cover = searchResult.Cover;
                    javVideo.Genres = searchResult.Genres;
                }
                return javVideo;
            }
        }
        #endregion


        /// <summary>
        /// 处理视频标题。
        /// </summary>
        /// <param name="javVideo"></param>
        /// <returns></returns>
        public virtual JavVideo TrimTitle(JavVideo javVideo)
        {
            if (javVideo.Actors != null && javVideo.Actors.Count > 0)
            {
                // 遍历演员名字
                foreach (var actor in javVideo.Actors)
                {
                    //去除标题中的演员名字
                    if (string.IsNullOrWhiteSpace(actor) == false && javVideo.Title?.EndsWith(actor, StringComparison.OrdinalIgnoreCase) == true)
                        javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();
                    javVideo.Title = javVideo.Title.Replace(actor, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    javVideo.OriginalTitle = javVideo.OriginalTitle.Replace(actor, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                }
            }
            return javVideo;
        }

        /// <summary>
        /// 移除标题尾部的演员名字
        /// </summary>
        /// <param name="title">原始标题</param>
        /// <param name="actors">演员列表</param>
        /// <returns>去除演员后的标题</returns>
        public static string RemoveActorsFromTitle(string title, List<string> actors)
        {
            // 空值处理
            if (string.IsNullOrWhiteSpace(title) || actors == null || actors.Count == 0)
                return title?.Trim();

            title = title.Replace("（DOD）", "");

            // 按名字长度倒序排序，避免短名字误删长名字
            var sortedActors = actors
                .Where(a => !string.IsNullOrEmpty(a))
                .OrderByDescending(a => a.Length)
                .ToList();

            string trimmedTitle = title.Trim();

            // 逆向匹配演员名字并删除
            foreach (var actor in sortedActors)
            {
                if (trimmedTitle.EndsWith(actor, StringComparison.OrdinalIgnoreCase))
                {
                    trimmedTitle = trimmedTitle.Substring(0, trimmedTitle.Length - actor.Length).Trim();
                }
            }

            return trimmedTitle;
        }

        //public virtual string RemoveActorsFromTitle(string title, List<string> actors)
        //{
        //    // 空值处理
        //    if (string.IsNullOrWhiteSpace(title)) return title;
        //    if (actors == null || actors.Count == 0) return title.Trim();

        //    // 创建演员名称的哈希集合（忽略大小写）
        //    var actorSet = new HashSet<string>(
        //        actors.Where(a => !string.IsNullOrEmpty(a)),
        //        StringComparer.OrdinalIgnoreCase
        //    );

        //    // 分割标题为片段（处理多种分隔符）
        //    string[] parts = Regex.Split(title.Trim(), @"[\s\.\,]+");

        //    // 逆向查找第一个非演员名称的索引
        //    int lastValidIndex = parts.Length - 1;
        //    while (lastValidIndex >= 0 && actorSet.Contains(parts[lastValidIndex]))
        //    {
        //        lastValidIndex--;
        //    }

        //    // 重组有效标题部分
        //    if (lastValidIndex < 0)
        //    {
        //        return string.Empty; // 所有部分都是演员名称
        //    }

        //    return string.Join(" ", parts.Take(lastValidIndex + 1));
        //}

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
        /// 获取 String
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public virtual async Task<string> GetStringAsync(string requestUri)
        {
            try
            {
                var response = await client.GetStringAsync(requestUri);
                if (string.IsNullOrWhiteSpace(response) == false)
                {
                    return response;
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

        public async Task<HtmlDocument> GetRenderedHtmlAsync(string url)
        {
            try
            {
                if (!File.Exists(BrowserExecutablePath))
                {
                    log?.LogError($"检测不到浏览器安装路径：{BrowserExecutablePath}");
                }

                var launchOptions = new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = BrowserExecutablePath,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                };

                using var browser = await Puppeteer.LaunchAsync(launchOptions);
                using var page = await browser.NewPageAsync();
                await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);

                var content = await page.GetContentAsync();
                if (string.IsNullOrWhiteSpace(content) == false)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"{ex.Message}");
            }

            return null;
        }

        #region Playwright 支持

        /// <summary>
        /// 浏览器可执行文件路径，供 Playwright 和 Puppeteer 共用。
        /// 修改此属性可全局替换浏览器路径。
        /// </summary>
        public static string BrowserExecutablePath { get; set; } =
            @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        /// <summary>
        /// 使用 Playwright 获取渲染后的 HTML 文档，适用于需要 JavaScript 渲染的页面（如 JavBus）。
        /// </summary>
        /// <param name="url">目标页面地址。</param>
        /// <param name="waitForSelector">可选：等待指定 CSS 选择器元素出现后再返回，若为 null 则等待 networkidle。</param>
        /// <param name="timeoutMs">超时时间（毫秒），默认 30000ms。</param>
        /// <returns>解析后的 HtmlDocument，失败时返回 null。</returns>
        public async Task<HtmlDocument> GetRenderedHtmlByPlaywrightAsync(
            string url,
            string waitForSelector = null,
            int timeoutMs = 30000)
        {
            try
            {
                var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    ExecutablePath = File.Exists(BrowserExecutablePath) ? BrowserExecutablePath : null,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" },
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                });

                // 屏蔽常见的反爬虫检测属性
                await context.AddInitScriptAsync(
                    "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })");

                var page = await context.NewPageAsync();

                // 优先用 DOMContentLoaded，避免 SPA 站点 NetworkIdle 超时
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = timeoutMs,
                });

                // 等待目标元素，超时后降级直接读取内容
                if (!string.IsNullOrEmpty(waitForSelector))
                {
                    try
                    {
                        await page.WaitForSelectorAsync(waitForSelector,
                            new PageWaitForSelectorOptions { Timeout = timeoutMs });
                    }
                    catch
                    {
                        log?.LogWarning($"[Playwright] waitForSelector '{waitForSelector}' 超时，尝试读取当前页面内容: {url}");
                    }
                }

                var content = await page.ContentAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"[Playwright] 请求页面失败: {url}, 错误: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 使用 Playwright 模拟登录后获取页面 HTML，适用于需要 Cookie/Session 的页面。
        /// </summary>
        /// <param name="url">目标地址。</param>
        /// <param name="cookies">预设的 Cookie 列表。</param>
        /// <param name="waitForSelector">可选：等待指定元素。</param>
        /// <param name="timeoutMs">超时时间（毫秒）。</param>
        /// <returns>解析后的 HtmlDocument，失败时返回 null。</returns>
        public async Task<HtmlDocument> GetRenderedHtmlWithCookiesAsync(
            string url,
            IEnumerable<Microsoft.Playwright.Cookie> cookies,
            string waitForSelector = null,
            int timeoutMs = 30000)
        {
            try
            {
                var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    ExecutablePath = File.Exists(BrowserExecutablePath) ? BrowserExecutablePath : null,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-blink-features=AutomationControlled" },
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                });

                // 屏蔽 webdriver 检测
                await context.AddInitScriptAsync(
                    "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })");

                if (cookies != null)
                {
                    await context.AddCookiesAsync(cookies);
                }

                var page = await context.NewPageAsync();

                // 先用 DOMContentLoaded ，避免 NetworkIdle 超时
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = timeoutMs,
                });

                // 等待指定元素，如果失败则直接读取当前内容
                if (!string.IsNullOrEmpty(waitForSelector))
                {
                    try
                    {
                        await page.WaitForSelectorAsync(waitForSelector,
                            new PageWaitForSelectorOptions { Timeout = timeoutMs });
                    }
                    catch
                    {
                        log?.LogWarning($"[Playwright] waitForSelector '{waitForSelector}' 超时，尝试读取当前页面内容: {url}");
                    }
                }

                var content = await page.ContentAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(content);
                    return doc;
                }
            }
            catch (Exception ex)
            {
                log?.LogError($"[Playwright] 带 Cookie 请求失败: {url}, 错误: {ex.Message}");
            }

            return null;
        }

        #endregion

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