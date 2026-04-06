using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    public class FanzaScraper : BaseScraper
    {
        private readonly HttpClient http;

        public FanzaScraper(HttpClient httpClient) : base("fanza", "https://www.dmm.co.jp")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var cleaned = code.Replace("-", "").Replace("_", "").ToLower();
            var url = $"https://www.dmm.co.jp/mono/dvd/-/detail/=/cid={cleaned}/";
            var html = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//tr/td/div[@class='hreview']/h1[@class='item fn']")?.InnerText?.Trim()
                        ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

            var plot = doc.DocumentNode.SelectSingleNode("//div[@class='mg-b20 lh4']/p[@class='mg-b20']")?.InnerText?.Trim();

            var maker = doc.DocumentNode.SelectSingleNode("//th[text()='メーカー']/following-sibling::td")?.InnerText?.Trim();

            var genreNodes = doc.DocumentNode.SelectNodes("//span[@class='genre']/a | //a[contains(@href,'/list/')]");
            var genres = genreNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var actorNodes = doc.DocumentNode.SelectNodes("//span[@class='performer']/a") ?? doc.DocumentNode.SelectNodes("//a[contains(@href,'/search/person')]");
            var actors = actorNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var cover = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null)
                        ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'poster')]")?.GetAttributeValue("src", null);

            var video = new JavVideo
            {
                Url = url,
                Number = code,
                Title = title,
                Plot = plot,
                Maker = maker,
                Cover = cover
            };

            if (actors != null && actors.Count > 0)
                video.Actors = actors;
            if (genres != null && genres.Count > 0)
                video.Genres = genres;

            return video;
        }
    }
}
