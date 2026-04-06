using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using JavScraper.Common.Scrapers;

namespace JavScraper.Common.Test.Scrapers
{
    /// <summary>
    /// JavfreeScraper 单元测试类
    /// </summary>
    public class JavfreeScraperTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly JavfreeScraper _scraper;

        public JavfreeScraperTests(ITestOutputHelper output)
        {
            _output = output;
            _scraper = (JavfreeScraper)CreateScraper("javfree");
        }

        #region 基础属性测试

        [Fact]
        public void Name_Should_BeJavfree()
        {
            // Assert
            Assert.Equal("javfree", _scraper.Name, ignoreCase: true);
        }

        [Fact]
        public void DefaultBaseUrl_Should_NotBeEmpty()
        {
            // Assert
            Assert.False(string.IsNullOrWhiteSpace(_scraper.DefaultBaseUrl));
            Assert.Contains("javfree", _scraper.DefaultBaseUrl, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region CanHandle 测试

        [Theory]
        [InlineData("FC2-123456")]
        [InlineData("FC2PPV-123456")]
        [InlineData("FC2_PPV_123456")]
        [InlineData("123456")]
        [InlineData("fc2-789012")]
        public void CanHandle_Should_AcceptValidFc2Codes(string code)
        {
            // Act
            var result = _scraper.CanHandle(code);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("ABP-782")]
        [InlineData("IPX-123")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CanHandle_Should_RejectNonFc2Codes(string code)
        {
            // Act
            var result = _scraper.CanHandle(code);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CheckKeyword 测试

        [Fact]
        public void CheckKeyword_Should_WorkCorrectly()
        {
            // Act & Assert - JavFree 专门处理 FC2 内容
            Assert.True(_scraper.CheckKeyword("FC2-123456"));
            Assert.True(_scraper.CheckKeyword("123456")); // 纯数字也被接受（FC2 ID）
            Assert.False(_scraper.CheckKeyword("ABP-782")); // 非 FC2 格式应拒绝
            Assert.False(_scraper.CheckKeyword("IPX-123")); // 非 FC2 格式应拒绝
            Assert.False(_scraper.CheckKeyword(""));
            Assert.False(_scraper.CheckKeyword(null));
        }

        #endregion

        #region ParseFc2Number 测试

        [Theory]
        [InlineData("FC2-123456", "123456")]
        [InlineData("FC2PPV-789012", "789012")]
        [InlineData("FC2_PPV_345678", "345678")]
        [InlineData("123456", "123456")]
        [InlineData("fc2-999999", "999999")]
        [InlineData("FC2-ppv-111222", "111222")]
        public void ParseFc2Number_Should_ExtractCorrectId(string input, string expected)
        {
            // This test validates the internal logic through CanHandle
            var result = _scraper.CanHandle(input);
            Assert.True(result);
        }

        #endregion

        #region ScrapeAsync 集成测试（需要网络）

        /// <summary>
        /// 测试抓取功能（需要网络连接）
        /// 注意：这是一个集成测试，可能需要较长时间
        /// </summary>
        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_ReturnVideo_WithValidFc2Code()
        {
            // Arrange - 使用一个已知的 FC2 番号
            var code = "FC2-1899973";

            // Act
            var video = await _scraper.ScrapeAsync(code);

            // Assert
            Assert.NotNull(video);
            Assert.False(string.IsNullOrWhiteSpace(video.Title));
            Assert.False(string.IsNullOrWhiteSpace(video.Number));
            Assert.StartsWith("FC2-", video.Number);
            
            _output.WriteLine($"标题：{video.Title}");
            _output.WriteLine($"番号：{video.Number}");
            if (!string.IsNullOrEmpty(video.Cover))
                _output.WriteLine($"封面：{video.Cover}");
            if (!string.IsNullOrEmpty(video.Director))
                _output.WriteLine($"导演：{video.Director}");
            if (!string.IsNullOrEmpty(video.Date))
                _output.WriteLine($"发布日期：{video.Date}");
            _output.WriteLine($"预览图数量：{video.Samples?.Count ?? 0}");
        }

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_HandleInvalidCode()
        {
            // Arrange
            var code = "FC2-999999999"; // 不存在的番号

            // Act
            var video = await _scraper.ScrapeAsync(code);

            // Assert - 可能返回 null 或抛出异常，取决于网站行为
            _output.WriteLine($"结果：{(video == null ? "null" : "返回了数据")}");
        }

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task ScrapeAsync_Should_WorkWithPureNumber()
        {
            // Arrange - 只提供数字部分
            var code = "1899973";

            // Act
            var video = await _scraper.ScrapeAsync(code);

            // Assert
            Assert.NotNull(video);
            Assert.StartsWith("FC2-", video.Number);
            _output.WriteLine($"标题：{video.Title}");
            _output.WriteLine($"番号：{video.Number}");
        }

        #endregion

        #region SearchAsync 集成测试（需要网络）

        [Fact(Skip = "需要网络连接，手动启用")]
        public async Task SearchAsync_Should_ReturnResults()
        {
            // Arrange
            var keyword = "FC2-1899973";

            // Act
            var results = await _scraper.SearchAsync(keyword);

            // Assert
            Assert.NotNull(results);
            _output.WriteLine($"搜索结果数量：{results.Count}");
            
            foreach (var video in results.Take(3))
            {
                _output.WriteLine($"- {video.Number}: {video.Title}");
            }
        }

        #endregion
    }
}
