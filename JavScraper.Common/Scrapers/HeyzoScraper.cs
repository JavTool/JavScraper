using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    public class HeyzoScraper : BaseScraper
    {
        private readonly HttpClient http;

        public HeyzoScraper(HttpClient httpClient) : base("heyzo", "https://www.heyzo.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // normalize heyzo id
            var heyzoId = code.Replace("heyzo-", "", StringComparison.OrdinalIgnoreCase);
            var url = $"https://www.heyzo.com/moviepages/{heyzoId}/index.html";
            var html = await http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//div[@id='movie']/h1")?.InnerText?.Trim();
            var plot = doc.DocumentNode.SelectSingleNode("//p[@class='memo']")?.InnerText?.Trim();

            var actorNodes = doc.DocumentNode.SelectNodes("//tr[@class='table-actor']/td/a");
            var actors = actorNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var tagNodes = doc.DocumentNode.SelectNodes("//ul[@class='tag-keyword-list']/li");
            var tags = tagNodes?.Select(n => n.InnerText?.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();

            var video = new JavVideo
            {
                Url = url,
                Number = code,
                Title = title,
                Plot = plot,
                Maker = "HEYZO"
            };

            if (actors != null && actors.Count > 0)
                video.Actors = actors;
            if (tags != null && tags.Count > 0)
                video.Genres = tags;

            return video;
        }
    }
}
