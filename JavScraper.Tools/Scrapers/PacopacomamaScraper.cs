using JavScraper.Tools.Entities;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// Pacopacomama 刮削器
    /// </summary>
    public class PacopacomamaScraper : IUncensoredScraper
    {
        private readonly JavUncensoredScraper javUncensoredScraper;

        public PacopacomamaScraper(JavUncensoredScraper javUncensoredScraper)
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