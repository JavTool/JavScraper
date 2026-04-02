using JavScraper.App;
using JavScraper.App.Models;
using JavScraper.App.Scrapers;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers.Uncensored
{
    /// <summary>
    /// Caribbeancom 刮削器
    /// </summary>
    public class CaribbeancomScraper : IUncensoredScraper
    {
        private readonly UncensoredScraper javUncensoredScraper;

        public CaribbeancomScraper(UncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "Caribbeancom";

        public bool CanHandle(JavId javId)
        {
            // Caribbeancom 格式: 数字-数字 (例如: 010122-001)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^\d{6}-\d{3}$");
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.GetCaribbeanMetadata(javId);
        }
    }
}