using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    public class AvbaseScraper : BaseScraper
    {
        private readonly HttpClient http;

        public AvbaseScraper(HttpClient httpClient) : base("avbase", "https://www.avbase.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var url = $"https://www.avbase.com/videos/{code}";
            var html = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim() ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            var desc = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
                       ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null);

            var poster = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);

            var actorNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/actor/') or contains(@href,'/star/')]");
            var actors = actorNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var genreNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/genre') or contains(@href,'/tag')]");
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
