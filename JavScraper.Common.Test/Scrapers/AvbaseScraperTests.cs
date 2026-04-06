using JavScraper.Common.Scrapers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace JavScraper.Common.Test.Scrapers
{
    /// <summary>
    /// AvbaseScraper 单元测试类
    /// </summary>
    public class AvbaseScraperTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly AvbaseScraper _scraper;

        public AvbaseScraperTests(ITestOutputHelper output)
        {
            _output = output;
            _scraper = (AvbaseScraper)CreateScraper("avbase");
        }

        [Fact]
        public void Name_Should_BeAvbase()
        {
            Assert.Equal("avbase", _scraper.Name, ignoreCase: true);
        }

        [Fact]
        public void DefaultBaseUrl_Should_NotBeEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(_scraper.DefaultBaseUrl));
            Assert.Contains("avbase", _scraper.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("ABW-001")]
        [InlineData("IPX-123")]
        public void CanHandle_Should_AcceptValidCodes(string code)
        {
            Assert.True(_scraper.CanHandle(code));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CanHandle_Should_RejectInvalidCodes(string code)
        {
            Assert.False(_scraper.CanHandle(code));
        }

        #region CheckKeyword 测试

        [Fact]
        public void CheckKeyword_Should_WorkCorrectly()
        {
            Assert.True(_scraper.CheckKeyword("valid-keyword"));
            Assert.False(_scraper.CheckKeyword(""));
            Assert.False(_scraper.CheckKeyword(null));
        }

        #endregion

        #region ScrapeAsync 集成测试（需要网络）

        /// <summary>
        /// 测试抓取功能（需要网络连接）
        /// </summary>
        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_ReturnVideo_WithValidCode()
        {
            var code = "ABP-782";
            var video = await _scraper.ScrapeAsync(code);
            
            Assert.NotNull(video);
            Assert.False(string.IsNullOrWhiteSpace(video.Title));
            _output.WriteLine($"标题：{video.Title}");
        }

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_HandleInvalidCode()
        {
            var code = "INVALID-999";
            var video = await _scraper.ScrapeAsync(code);
            _output.WriteLine($"结果：{(video == null ? "null" : "有数据")}");
        }

        #endregion
    }
}
