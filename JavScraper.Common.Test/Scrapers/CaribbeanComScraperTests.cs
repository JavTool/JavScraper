using System;
using Xunit;
using Xunit.Abstractions;
using JavScraper.Common.Scrapers;
using System.Threading.Tasks;

namespace JavScraper.Common.Test.Scrapers
{
    /// <summary>
    /// CaribbeanComScraper 单元测试类
    /// </summary>
    public class CaribbeanComScraperTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly CaribbeanComScraper _scraper;

        public CaribbeanComScraperTests(ITestOutputHelper output)
        {
            _output = output;
            _scraper = (CaribbeanComScraper)CreateScraper("caribbeancom");
        }

        [Fact]
        public void Name_Should_BeCaribbeanCom()
        {
            Assert.Equal("caribbeancom", _scraper.Name, ignoreCase: true);
        }

        [Fact]
        public void DefaultBaseUrl_Should_NotBeEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(_scraper.DefaultBaseUrl));
            Assert.Contains("caribbeancom", _scraper.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase);
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

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_ReturnVideo_WithValidCode()
        {
            var code = "042922_001"; // CaribbeanCom 特有格式
            var video = await _scraper.ScrapeAsync(code);
            Assert.NotNull(video);
            _output.WriteLine($"标题：{video?.Title}");
        }

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_HandleInvalidCode()
        {
            var video = await _scraper.ScrapeAsync("INVALID-999");
            _output.WriteLine($"结果：{(video == null ? "null" : "有数据")}");
        }

        #endregion
    }
}
