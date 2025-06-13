using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// 一本道刮削器
    /// </summary>
    public class OnePondoScraper : IUncensoredScraper
    {
        private readonly JavUncensoredScraper javUncensoredScraper;

        public OnePondoScraper(JavUncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "1Pondo";

        public bool CanHandle(JavId javId)
        {
            // 1Pondo 格式: 数字_数字 (例如: 010122_001)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^\d{6}_\d{3}$");
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.Get1PondoMetadata(javId);
        }
    }
}