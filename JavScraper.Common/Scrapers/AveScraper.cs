using System.Threading.Tasks;
using JavScraper.Common.Models;
using System.Net.Http;

namespace JavScraper.Common.Scrapers
{
    public class AveScraper : BaseScraper
    {
        private readonly HttpClient http;
        public AveScraper(HttpClient httpClient) : base("ave", "https://www.aventertainments.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            // This provider needs search; implemented elsewhere. Provide placeholder.
            await Task.CompletedTask;
            return null;
        }
    }
}
