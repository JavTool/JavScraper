using JavScraper.Tools.Entities;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers.Uncensored
{
    /// <summary>
    /// AvEntertainment 刮削器
    /// </summary>
    public class AvEntertainmentScraper : IUncensoredScraper
    {
        private readonly UncensoredScraper javUncensoredScraper;

        public AvEntertainmentScraper(UncensoredScraper javUncensoredScraper)
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

            return await javUncensoredScraper.GetAvEntertainmentMetadata(javId);
        }
    }
}