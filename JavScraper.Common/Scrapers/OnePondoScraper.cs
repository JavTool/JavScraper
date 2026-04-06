using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;

namespace JavScraper.Common.Scrapers
{
    public class OnePondoScraper : BaseScraper
    {
        private readonly HttpClient http;
        public OnePondoScraper(HttpClient httpClient) : base("1pondo", "https://www.1pondo.tv")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var jsonUrl = $"https://www.1pondo.tv/dyn/phpauto/movie_details/movie_id/{code}.json";
            var url = $"https://www.1pondo.tv/movies/{code}/";

            var json = await http.GetStringAsync(jsonUrl);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // minimal deserialization to extract title/desc
            try
            {
                var docHtml = await http.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(docHtml ?? string.Empty);

                // try to parse some simple fields
                var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

                var video = new JavVideo
                {
                    Url = url,
                    Number = code,
                    Title = title
                };
                return video;
            }
            catch
            {
                return null;
            }
        }
    }
}
