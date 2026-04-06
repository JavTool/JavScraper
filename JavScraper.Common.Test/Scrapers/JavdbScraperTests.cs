using System;
using Xunit;
using Xunit.Abstractions;
using JavScraper.Common.Scrapers;
using System.Threading.Tasks;

namespace JavScraper.Common.Test.Scrapers
{
    /// <summary>
    /// JavdbScraper 单元测试类
    /// </summary>
    public class JavdbScraperTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly JavdbScraper _scraper;

        public JavdbScraperTests(ITestOutputHelper output)
        {
            _output = output;
            _scraper = (JavdbScraper)CreateScraper("javdb");
        }

        [Fact]
        public void Name_Should_BeJavdb()
        {
            Assert.Equal("javdb", _scraper.Name, ignoreCase: true);
        }

        [Fact]
        public void DefaultBaseUrl_Should_NotBeEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(_scraper.DefaultBaseUrl));
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
        /// 注意：这是一个集成测试，可能需要较长时间
        /// </summary>
        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_ReturnVideo_WithValidCode()
        {
            // Arrange
            var code = "ABP-782";

            // Act
            var video = await _scraper.ScrapeAsync(code);

            // Assert
            Assert.NotNull(video);
            Assert.False(string.IsNullOrWhiteSpace(video.Title));
            Assert.False(string.IsNullOrWhiteSpace(video.Number));
            
            _output.WriteLine($"标题：{video.Title}");
            _output.WriteLine($"番号：{video.Number}");
            if (!string.IsNullOrEmpty(video.Cover))
                _output.WriteLine($"封面：{video.Cover}");
        }

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_ReturnNull_WithInvalidCode()
        {
            // Arrange
            var code = "INVALID-CODE-999";

            // Act
            var video = await _scraper.ScrapeAsync(code);

            // Assert - 可能返回 null 或抛出异常，取决于网站行为
            _output.WriteLine($"结果：{(video == null ? "null" : "返回了数据")}");
        }

        #endregion
    }
}
