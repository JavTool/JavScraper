using JavScraper.Tools.Entities;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// FC2 刮削器
    /// </summary>
    public class FC2Scraper : IUncensoredScraper
    {
        private readonly JavUncensoredScraper javUncensoredScraper;

        public FC2Scraper(JavUncensoredScraper javUncensoredScraper)
        {
            this.javUncensoredScraper = javUncensoredScraper;
        }

        public string Name => "FC2";

        public bool CanHandle(JavId javId)
        {
            // FC2 格式: FC2-数字 或 FC2PPV-数字 (例如: FC2-1234567, FC2PPV-1234567)
            return System.Text.RegularExpressions.Regex.IsMatch(javId, @"^FC2(PPV)?-?\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public async Task<JavVideo> GetMetadataAsync(JavId javId)
        {
            if (!CanHandle(javId))
                return null;

            return await javUncensoredScraper.GetFC2Metadata(javId);
        }
    }
}