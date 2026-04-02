using JavScraper.App;
using JavScraper.App.Models;
using JavScraper.App.Scrapers;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers.Uncensored
{
    /// <summary>
    /// Pacopacomama 刮削器
    /// </summary>
    public class PacopacomamaScraper : IUncensoredScraper
    {
        private readonly UncensoredScraper javUncensoredScraper;

        public PacopacomamaScraper(UncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "Pacopacomama";

        public bool CanHandle(JavId javId)
        {
            // Pacopacomama 格式: 数字_数字 (例如: 010122_001)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^\d{6}_\d{3}$");
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.GetPacopacomamaMetadata(javId);
        }
    }
}