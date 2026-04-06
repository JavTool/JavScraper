using JavScraper.App;
using JavScraper.App.Models;
using JavScraper.App.Scrapers;
using System.Threading.Tasks;

namespace JavScraper.App.Scrapers.Uncensored
{
    /// <summary>
    /// Heyzo 刮削器
    /// </summary>
    public class HeyzoScraper : IUncensoredScraper
    {
        private readonly UncensoredScraper javUncensoredScraper;

        public HeyzoScraper(UncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "Heyzo";

        public bool CanHandle(JavId javId)
        {
            // Heyzo 格式: HEYZO-数字 (例如: HEYZO-1234)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^HEYZO-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.GetHeyzoMetadata(javId);
        }
    }
}