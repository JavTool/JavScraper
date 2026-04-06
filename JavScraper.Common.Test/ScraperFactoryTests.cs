using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using JavScraper.Common.Scrapers;

namespace JavScraper.Common.Test
{
    /// <summary>
    /// ScraperFactory 单元测试类
    /// </summary>
    public class ScraperFactoryTests : TestBase
    {
        private readonly ITestOutputHelper _output;

        public ScraperFactoryTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region 基础功能测试

        /// <summary>
        /// 测试是否成功注册了所有刮削器
        /// </summary>
        [Fact]
        public void Should_RegisterAllScrapers()
        {
            // Arrange & Act
            var availableScrapers = GetAvailableScraperNames().ToList();

            // Assert
            Assert.NotEmpty(availableScrapers);
            _output.WriteLine($"已注册的刮削器数量：{availableScrapers.Count}");
            foreach (var scraper in availableScrapers)
            {
                _output.WriteLine($"  - {scraper}");
            }
        }

        /// <summary>
        /// 测试是否可以创建已知名称的刮削器
        /// </summary>
        [Theory]
        [InlineData("javbus")]
        [InlineData("javdb")]
        [InlineData("avbase")]
        public void Should_CreateScraper_When_NameExists(string scraperName)
        {
            // Arrange & Act
            var scraper = CreateScraper(scraperName);

            // Assert
            Assert.NotNull(scraper);
            Assert.Equal(scraperName, scraper.Name, ignoreCase: true);
        }

        /// <summary>
        /// 测试创建不存在的刮削器时 TryCreateScraper 返回 false
        /// </summary>
        [Fact]
        public void TryCreateScraper_Should_ReturnFalse_When_NameNotExists()
        {
            // Arrange & Act
            var result = ScraperFactory.TryCreateScraper("NonExistentScraper", out var scraper);

            // Assert
            Assert.False(result);
            Assert.Null(scraper);
        }

        /// <summary>
        /// 测试 IsRegistered 方法是否正确工作
        /// </summary>
        [Fact]
        public void IsRegistered_Should_WorkCorrectly()
        {
            // Arrange
            var existingName = "javbus";
            var nonExistingName = "NonExistentScraper";

            // Act & Assert
            Assert.True(ScraperFactory.IsRegistered(existingName));
            Assert.False(ScraperFactory.IsRegistered(nonExistingName));
        }

        /// <summary>
        /// 测试创建空名称的刮削器抛出异常
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void CreateScraper_Should_ThrowException_When_NameIsEmpty(string name)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CreateScraper(name));
        }

        #endregion

        #region FindScrapers 测试

        /// <summary>
        /// 测试 FindScrapers 方法能否找到合适的刮削器
        /// </summary>
        [Fact]
        public void FindScrapers_Should_FindSuitableScrapers()
        {
            // Arrange
            var keyword = "ABW-001";

            // Act
            var scrapers = ScraperFactory.FindScrapers(keyword).ToList();

            // Assert
            Assert.NotEmpty(scrapers);
            _output.WriteLine($"找到 {scrapers.Count} 个可以处理 '{keyword}' 的刮削器:");
            foreach (var scraper in scrapers)
            {
                _output.WriteLine($"  - {scraper.Name}");
                Assert.NotNull(scraper);
                Assert.True(!string.IsNullOrEmpty(scraper.Name));
            }
        }

        /// <summary>
        /// 测试 FindScrapers 对空关键字的处理
        /// </summary>
        [Fact]
        public void FindScrapers_Should_HandleEmptyKeyword()
        {
            // Arrange
            var keyword = "";

            // Act
            var scrapers = ScraperFactory.FindScrapers(keyword).ToList();

            // Assert - 应该找不到任何刮削器（因为 CanHandle 会检查关键字）
            Assert.Empty(scrapers);
        }

        #endregion

        #region ForEachFactory 测试

        /// <summary>
        /// 测试 ForEachFactory 方法能否遍历所有工厂
        /// </summary>
        [Fact]
        public void ForEachFactory_Should_IterateAllFactories()
        {
            // Arrange
            var count = 0;
            var factoryNames = new System.Collections.Generic.List<string>();

            // Act
            ScraperFactory.ForEachFactory((name, factory) =>
            {
                count++;
                factoryNames.Add(name);
                Assert.NotNull(factory);
            });

            // Assert
            Assert.Equal(ScraperFactory.Count, count);
            _output.WriteLine($"遍历了 {count} 个工厂");
        }

        #endregion

        #region Count 属性测试

        /// <summary>
        /// 测试 Count 属性是否正确返回工厂数量
        /// </summary>
        [Fact]
        public void Count_Should_ReturnCorrectNumber()
        {
            // Act
            var count = ScraperFactory.Count;

            // Assert
            Assert.True(count > 0, "应该有已注册的刮削器");
            Assert.Equal(count, GetAvailableScraperNames().Count());
        }

        #endregion

        #region 线程安全测试

        /// <summary>
        /// 测试多线程环境下创建刮削器的安全性
        /// </summary>
        [Fact]
        public void Should_BeThreadSafe_When_CreatingScrapers()
        {
            // Arrange
            const int threadCount = 10;
            var tasks = new System.Threading.Tasks.Task[threadCount];
            var results = new BaseScraper[threadCount];
            var random = new Random();

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                tasks[index] = System.Threading.Tasks.Task.Run(() =>
                {
                    var scraperName = GetAvailableScraperNames().ElementAt(random.Next(0, ScraperFactory.Count));
                    results[index] = CreateScraper(scraperName);
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            foreach (var scraper in results)
            {
                Assert.NotNull(scraper);
            }
        }

        #endregion
    }
}
