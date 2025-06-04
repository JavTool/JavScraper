using JavScraper.App.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App.Scrapers
{
    public class JavBus : AbstractScraper
    {

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="handler"></param>
        public JavBus(ILoggerFactory loggerFactory)
            : base("https://www.javbus.com/", loggerFactory.CreateLogger<JavBus>())
        {
        }

        /// <summary>
        /// 检查关键字是否符合。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool CheckKeyword(string key) => JavIdRecognizer.FC2(key) == null;


        public override Task<List<JavVideo>> ParseList(string url)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取详情。
        /// </summary>
        /// <param name="url">地址。</param>
        /// <returns></returns>
        public override async Task<JavVideo> ParsePage(string url)
        {
            //https://www.javbus.cloud/ABP-933
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null)
                return null;

            var node = doc.DocumentNode.SelectSingleNode("//div[@class='container']/h3/..");
            if (node == null)
                return null;

            var dic = new Dictionary<string, string>();
            var nodes = node.SelectNodes(".//span[@class='header']");
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

            var genres = node.SelectNodes(".//span[@class='genre']")?
                 .Select(o => o.InnerText.Trim()).ToList();

            var actors = node.SelectNodes(".//div[@class='star-name']")?
                 .Select(o => o.InnerText.Trim()).ToList();

            var samples = node.SelectNodes(".//a[@class='sample-box']")?
                 .Select(o => o.GetAttributeValue("href", null)).Where(o => o != null).ToList();

            var video = new JavVideo()
            {
                Url = url,
                Title = node.SelectSingleNode("./h3")?.InnerText?.Trim(),
                Cover = node.SelectSingleNode(".//a[@class='bigImage']")?.GetAttributeValue("href", null),
                Number = GetValue("識別碼"),
                Date = GetValue("發行日期"),
                Runtime = GetValue("長度"),
                Maker = GetValue("發行商"),
                Studio = GetValue("製作商"),
                Set = GetValue("系列"),
                Director = GetValue("導演"),
                //Plot = node.SelectSingleNode("./h3")?.InnerText,
                Genres = genres,
                Actors = actors,
                Samples = samples,
            };

            video.Plot = await GetDmmPlot(video.Number);
            // 去除标题中的番号
            if (string.IsNullOrWhiteSpace(video.Number) == false && video.Title?.StartsWith(video.Number, StringComparison.OrdinalIgnoreCase) == true)
                video.Title = video.Title[video.Number.Length..].Trim();

            return video;
        }
    }
}
