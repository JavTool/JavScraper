using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    public class Jav123Scraper : BaseScraper
    {
        private readonly HttpClient http;

        public Jav123Scraper(HttpClient httpClient) : base("jav123", "https://www.jav123.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var url = $"https://www.jav123.com/{code}";
            var html = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim() ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            var desc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
                       ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null);

            var poster = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null)
                         ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'poster')]")?.GetAttributeValue("src", null);

            var actorNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/star/') or contains(@href,'/actor/')]");
            var actors = actorNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var genreNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/genre') or contains(@href,'/category')]");
            var genres = genreNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var video = new JavVideo
            {
                Url = url,
                Number = code,
                Title = title,
                Plot = desc,
                Cover = poster
            };

            if (actors != null && actors.Count > 0)
                video.Actors = actors;
            if (genres != null && genres.Count > 0)
                video.Genres = genres;

            return video;
        }
    }
}
