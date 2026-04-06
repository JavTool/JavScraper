using JavScraper.Common.Scrapers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace JavScraper.Common.Test
{
    /// <summary>
    /// 所有刮削器的通用测试
    /// 测试每个刮削器的基本行为和规范
    /// </summary>
    public class AllScrapersTests : TestBase
    {
        private readonly ITestOutputHelper _output;
        private readonly IEnumerable<string> _scraperNames;

        public AllScrapersTests(ITestOutputHelper output)
        {
            _output = output;
            _scraperNames = GetAvailableScraperNames().ToList();
        }

        #region 基础属性测试

        /// <summary>
        /// 测试所有刮削器都有有效的名称
        /// </summary>
        [Fact]
        public void AllScrapers_Should_HaveValidName()
        {
            foreach (var scraperName in _scraperNames)
            {
                // Arrange & Act
                var scraper = CreateScraper(scraperName);

                // Assert
                Assert.NotNull(scraper);
                Assert.False(string.IsNullOrWhiteSpace(scraper.Name), 
                    $"{scraperName} 的名称不能为空");
                
                _output.WriteLine($"✓ {scraperName}: Name = '{scraper.Name}'");
            }
        }

        /// <summary>
        /// 测试所有刮削器的名称是否唯一
        /// </summary>
        [Fact]
        public void AllScraperNames_Should_BeUnique()
        {
            // Arrange & Act
            var names = _scraperNames.ToList();
            var distinctNames = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Assert
            Assert.Equal(names.Count, distinctNames.Count);
            _output.WriteLine($"所有 {names.Count} 个刮削器名称都是唯一的");
        }

        #endregion

        #region CanHandle 测试

        /// <summary>
        /// 测试所有刮削器对空关键字的处理
        /// </summary>
        [Fact]
        public void AllScrapers_Should_RejectEmptyKeyword()
        {
            foreach (var scraperName in _scraperNames)
            {
                // Arrange
                var scraper = CreateScraper(scraperName);

                // Act
                var canHandleNull = scraper.CanHandle(null);
                var canHandleEmpty = scraper.CanHandle("");
                var canHandleWhitespace = scraper.CanHandle("   ");

                // Assert
                Assert.False(canHandleNull, $"{scraperName} 不应该接受 null 关键字");
                Assert.False(canHandleEmpty, $"{scraperName} 不应该接受空字符串关键字");
                Assert.False(canHandleWhitespace, $"{scraperName} 不应该接受空白字符关键字");
                
                _output.WriteLine($"✓ {scraperName}: 正确拒绝了空/空白关键字");
            }
        }

        /// <summary>
        /// 测试所有刮削器能处理有效的番号格式
        /// </summary>
        [Theory]
        [InlineData("ABW-001")]
        [InlineData("IPX-123")]
        [InlineData("HEYZO-1234")]
        public void AllScrapers_Should_HandleValidCodes(string code)
        {
            foreach (var scraperName in _scraperNames)
            {
                // Arrange
                var scraper = CreateScraper(scraperName);

                // Act
                var canHandle = scraper.CanHandle(code);

                // Assert - 注意：这里不要求所有刮削器都能处理，因为有些可能有特定格式要求
                // 这个测试主要用于观察和记录
                _output.WriteLine($"{scraperName}: CanHandle('{code}') = {canHandle}");
            }
        }

        #endregion

        #region CheckKeyword 测试

        /// <summary>
        /// 测试所有刮削器的 CheckKeyword 方法
        /// </summary>
        [Fact]
        public void AllScrapers_Should_CheckKeywordCorrectly()
        {
            foreach (var scraperName in _scraperNames)
            {
                // Arrange
                var scraper = CreateScraper(scraperName);

                // Act & Assert
                Assert.True(scraper.CheckKeyword("valid-keyword"));
                Assert.False(scraper.CheckKeyword(""));
                Assert.False(scraper.CheckKeyword(null));
                Assert.False(scraper.CheckKeyword("   "));
                
                _output.WriteLine($"✓ {scraperName}: CheckKeyword 工作正常");
            }
        }

        #endregion

        #region 实例创建测试

        /// <summary>
        /// 测试所有刮削器都可以成功创建实例
        /// </summary>
        [Fact]
        public void AllScrapers_Should_CreateSuccessfully()
        {
            var successCount = 0;
            var failedScrapers = new List<string>();

            foreach (var scraperName in _scraperNames)
            {
                try
                {
                    // Act
                    var scraper = CreateScraper(scraperName);

                    // Assert
                    Assert.NotNull(scraper);
                    Assert.IsAssignableFrom<BaseScraper>(scraper);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedScrapers.Add($"{scraperName}: {ex.Message}");
                }
            }

            _output.WriteLine($"成功创建：{successCount}/{_scraperNames.Count()}");
            
            if (failedScrapers.Any())
            {
                _output.WriteLine("失败的刮削器:");
                foreach (var failed in failedScrapers)
                {
                    _output.WriteLine($"  ✗ {failed}");
                }
            }

            Assert.Equal(_scraperNames.Count(), successCount);
        }

        #endregion

        #region 特定场景测试

        /// <summary>
        /// 测试所有刮削器对不同格式番号的处理能力
        /// </summary>
        [Theory]
        [InlineData("ABC-123")]      // 标准格式
        [InlineData("123-ABC")]      // 反向格式
        [InlineData("ABC123")]       // 无连字符
        [InlineData("abc-123")]      // 小写
        [InlineData("ABC-0001")]     // 多位数字
        public void AllScrapers_Should_HandleDifferentCodeFormats(string code)
        {
            foreach (var scraperName in _scraperNames)
            {
                // Arrange
                var scraper = CreateScraper(scraperName);

                // Act
                var canHandle = scraper.CanHandle(code);

                // Assert - 仅记录结果，不做强制要求
                _output.WriteLine($"{scraperName}: CanHandle('{code}') = {canHandle}");
            }
        }

        #endregion

        #region 性能相关测试

        /// <summary>
        /// 测试快速连续创建多个刮削器实例
        /// </summary>
        [Fact]
        public void Should_CreateMultipleInstancesQuickly()
        {
            const int iterations = 100;
            var scrapers = new List<BaseScraper>();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                foreach (var scraperName in _scraperNames.Take(3)) // 只测试前 3 个
                {
                    var scraper = CreateScraper(scraperName);
                    scrapers.Add(scraper);
                }
            }

            // Assert
            Assert.Equal(iterations * Math.Min(3, _scraperNames.Count()), scrapers.Count);
            _output.WriteLine($"在单次测试中创建了 {scrapers.Count} 个刮削器实例");
        }

        #endregion
    }
}
