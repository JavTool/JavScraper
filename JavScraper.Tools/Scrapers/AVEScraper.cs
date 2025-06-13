using JavScraper.Tools.Entities;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// AVE 刮削器
    /// </summary>
    public class AVEScraper : IUncensoredScraper
    {
        private readonly JavUncensoredScraper javUncensoredScraper;

        public AVEScraper(JavUncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "AVE";

        public bool CanHandle(JavId javId)
        {
            // AVE 格式: 数字 (例如: 123456)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^\d+$");
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.GetAVEMetadata(javId);
        }
    }
}