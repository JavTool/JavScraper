using HtmlAgilityPack;
using JavScraper.App.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.App.Scrapers
{
    /// <summary>
    /// 痴汉队长刮片器。
    /// </summary>
    public class JavCaptain : AbstractScraper
    {
        /// <summary>
        /// 番号分段识别。
        /// </summary>
        private static Regex regex = new Regex("((?<a>[a-z]{2,})|(?<b>[0-9]{2,}))", RegexOptions.Compiled | RegexOptions.IgnoreCase);



        private static Regex regexBrackets = new Regex(@"\s*\(.*?\)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public JavCaptain(ILoggerFactory loggerFactory)
            : base("https://javcaptain.com/zh/", loggerFactory.CreateLogger<JavCaptain>())
        {
        }

        /// <summary>
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool CheckKeyword(string key) => JavIdRecognizer.Western(key) != null;

        public override async Task<List<JavVideo>> ParseList(string url)
        {
            throw new NotImplementedException();
        }

        public override async Task<JavVideo> ParsePage(string url)
        {
            // https://javcaptain.com/zh/DoctorAdventures.19.12.11
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return null;

            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'card-body')]/div");
            if (nodes?.Any() != true)
                return null;

            var dic = new Dictionary<string, string>();

            string RemoveTextInBrackets(string input) => Regex.Replace(input, @"\s*\(.*?\)\s*", string.Empty);

            foreach (var n in nodes)
            {
                var k = n.SelectSingleNode("span")?.InnerText?.Trim().Replace("&nbsp;", " ").Replace(": ", "");
                string v = null;
                if (k?.Contains("女优") == true)
                {
                    var ac = n.SelectNodes("div/a[@class='actress']");
                    if (ac?.Any() == true)
                        v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                }
                //if (k?.Contains("番号") == true)
                //{
                //    v = n.InnerText?.Trim().Replace("&nbsp;", " ").Replace(k, "").Replace("\n", "");
                //}

                if (k?.Contains("类别") == true)
                {
                    var ac = n.SelectNodes("div/a[@class='genre']");
                    if (ac?.Any() == true)
                        v = string.Join(",", ac.Select(o => o.InnerText?.Trim()));
                }
                if (k?.Contains("製作") == true)
                {
                    v = n.InnerText?.Trim().Replace("&nbsp;", " ").Replace(k, "").Replace(": ", "").Replace("\n", "");

                    // 使用正则表达式匹配括号中的内容
                    Match match = Regex.Match(v, @"\((.*?)\)");
                    // 如果找到匹配项，返回括号中的内容
                    if (match.Success)
                    {
                        if (!dic.ContainsKey("出版"))
                        {
                            dic.Add("出版", match.Groups[1].Value);
                        }
                        v = RemoveTextInBrackets(v);
                    }
                }
                if (!string.IsNullOrWhiteSpace(k) && v == null)
                    v = n.InnerText?.Trim().Replace("&nbsp;", " ").Replace(k, "").Replace(": ", "").Replace("\n", "");

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
                var v = GetValue("女优");
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
            //[@class='genre'] h1[contains(@class,'display-5')]
            //doc.DocumentNode.SelectSingleNode("//div[contains(@class,'container-fluid')]//h1/strong");
            var javVideo = new JavVideo()
            {
                //Provider = Name,
                Url = url,
                Title = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'container-fluid')]//h1/strong")?.InnerText?.Replace("免费AV在线看", "").Trim(),
                Cover = GetCover(),
                Number = GetValue("番号")?.ToUpper(),
                Date = GetValue("日期"),
                Runtime = GetValue("时长"),
                Maker = GetValue("製作"),
                Studio = GetValue("製作"),
                Set = GetValue("系列"),
                Director = GetValue("导演"),
                Genres = GetGenres(),
                Actors = GetActors(),
                Samples = GetSamples(),
            };

            if (javVideo.Title.Contains(javVideo.Number))
            {
                javVideo.Title = javVideo.Title.Replace(javVideo.Number, "").Trim();
            }
            if (javVideo.OriginalTitle.Contains(javVideo.Number))
            {
                javVideo.OriginalTitle = javVideo.OriginalTitle.Replace(javVideo.Number, "").Trim();
            }
            //if (StringUtil.IsBGI5(javVideo.Title))
            //{
            //    javVideo.Title = StringUtil.TraditionalToSimplified(javVideo.Title);
            //    javVideo.OriginalTitle = javVideo.Title;
            //}


            //javVideo.Plot = await GetDmmPlot(javVideo.Number);
            ////去除标题中的番号
            if (string.IsNullOrWhiteSpace(javVideo.Number) == false && javVideo.Title?.StartsWith(javVideo.Number, StringComparison.OrdinalIgnoreCase) == true)
                javVideo.Title = javVideo.Title.Substring(javVideo.Number.Length).Trim();

            return javVideo;
        }

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
            var nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'card-body')]/a");
            if (nodes?.Any() != true)
                return ls;

            foreach (var node in nodes)
            {
                //var n = node.SelectNodes("//div[contains(@class,'card-body')]");
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

                m.Number = node.SelectSingleNode("./h5")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Number))
                    m.Number = node.SelectSingleNode("./div[@class='uid2']")?.InnerText.Trim();
                m.Title = node.SelectSingleNode("./div[@class='video-title']")?.InnerText.Trim();
                if (string.IsNullOrEmpty(m.Title))
                    m.Title = node.SelectSingleNode("./div[@class='video-title2']")?.InnerText.Trim();
                m.Date = node.SelectSingleNode("./span[@class='text-muted']")?.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(m.Number) == false && m.Title?.StartsWith(m.Number, StringComparison.OrdinalIgnoreCase) == true)
                    m.Title = m.Title.Substring(m.Number.Length).Trim();
            }

            return ls;
        }
        public async Task<List<JavVideo>> Search(List<JavVideo> ls, string number)
        {
            //List<JavVideo> ls;
            // https://javcaptain.com/zh/search?wd=DoctorAdventures.19.12.11
            var doc = await GetHtmlDocumentAsync($"/search?wd={number}");
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
    }
}
