using JavScraper.App.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App.Scrapers
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


        public override Task<List<JavVideo>> ParseList(string url)
        {
            throw new NotImplementedException();
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

            //var genres = doc.DocumentNode.SelectNodes("//table[@class='mg-b20']/tr[8]/td[2]/a")?
            //     .Select(o => o.InnerText.Trim()).ToList();
            var genres = doc.DocumentNode.SelectNodes("//ul[@class='list-keyword']")?
                            .Select(o => o.InnerText.Trim()).ToList();

            var actors = doc.DocumentNode.SelectNodes("//span[@id='performer']/a")?
                 .Select(o => o.InnerText.Trim()).ToList();

            var samples = doc.DocumentNode.SelectNodes("//a[@class='fn-sample-image crs_full']/img")?
                 .Select(o => o.GetAttributeValue("data-lazy", null)).Where(o => o != null).ToList();

            var video = new JavVideo()
            {
                Url = url,
                Title = node.SelectSingleNode("./h1")?.InnerText?.Trim(),
                Cover = node.SelectSingleNode(".//a[@class='bigImage']")?.GetAttributeValue("href", null),
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
                video.Title = video.Title[video.Number.Length..].Trim();

            return video;
        }
    }
}
