using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using JavScraper.Scrapers.Implementations;

namespace JavScraper.Scrapers.Tests
{
    /// <summary>
    /// ScraperFactory 单元测试
    /// </summary>
    public class ScraperFactoryTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ScraperFactory _scraperFactory;

        public ScraperFactoryTests()
        {
            // 配置依赖注入容器
            var services = new ServiceCollection();
            
            // 添加日志服务
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // 添加 HttpClient 工厂
            services.AddSingleton<ScraperHttpClientFactory>();
            
            // 添加缓存服务
            services.AddSingleton<ScraperCacheService>();
            
            // 添加示例图片服务
            services.AddSingleton<SampleImageService>();
            
            // 添加配置选项
            services.AddSingleton(new ScraperOptions
            {
                SiteSettings = new Dictionary<string, SiteSetting>
                {
                    ["JavBus"] = new SiteSetting 
                    { 
                        BaseUrl = "https://www.javbus.com",
                        Enabled = true 
                    },
                    ["JavDB"] = new SiteSetting 
                    { 
                        BaseUrl = "https://javdb.com",
                        Enabled = true 
                    },
                    ["Dmm"] = new SiteSetting 
                    { 
                        BaseUrl = "https://www.dmm.co.jp",
                        Enabled = true 
                    }
                }
            });

            // 添加 ScraperFactory
            services.AddSingleton<ScraperFactory>();

            _serviceProvider = services.BuildServiceProvider();
            _scraperFactory = _serviceProvider.GetRequiredService<ScraperFactory>();
        }

        /// <summary>
        /// 测试获取所有可用刮削器
        /// </summary>
        [Fact]
        public void GetAvailableScrapers_ShouldReturnNonEmptyList()
        {
            // Act
            var scrapers = _scraperFactory.GetAvailableScrapers().ToList();

            // Assert
            Assert.NotEmpty(scrapers);
            Assert.Contains("JavBus", scrapers);
        }

        /// <summary>
        /// 测试创建指定名称的刮削器
        /// </summary>
        [Fact]
        public void CreateScraper_ShouldReturnInstanceOfScraper()
        {
            // Act
            var scraper = _scraperFactory.CreateScraper("JavBus");

            // Assert
            Assert.NotNull(scraper);
            Assert.Equal("JavBus", scraper.Name);
            Assert.IsType<JavBusScraperV2>(scraper);
        }

        /// <summary>
        /// 测试创建不存在的刮削器应抛出异常
        /// </summary>
        [Fact]
        public void CreateScraper_WithInvalidName_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _scraperFactory.CreateScraper("NonExistentScraper"));
        }

        /// <summary>
        /// 测试 TryCreateScraper 方法
        /// </summary>
        [Fact]
        public void TryCreateScraper_ShouldReturnTrueForValidName()
        {
            // Act
            var result = _scraperFactory.TryCreateScraper("JavBus", out var scraper);

            // Assert
            Assert.True(result);
            Assert.NotNull(scraper);
            Assert.Equal("JavBus", scraper.Name);
        }

        [Fact]
        public void TryCreateScraper_ShouldReturnFalseForInvalidName()
        {
            // Act
            var result = _scraperFactory.TryCreateScraper("NonExistentScraper", out var scraper);

            // Assert
            Assert.False(result);
            Assert.Null(scraper);
        }

        /// <summary>
        /// 测试检查刮削器是否已注册
        /// </summary>
        [Fact]
        public void IsRegistered_ShouldReturnTrueForRegisteredScraper()
        {
            // Act
            var isRegistered = _scraperFactory.IsRegistered("JavBus");

            // Assert
            Assert.True(isRegistered);
        }

        [Fact]
        public void IsRegistered_ShouldReturnFalseForUnregisteredScraper()
        {
            // Act
            var isRegistered = _scraperFactory.IsRegistered("NonExistentScraper");

            // Assert
            Assert.False(isRegistered);
        }

        /// <summary>
        /// 测试获取工厂数量
        /// </summary>
        [Fact]
        public void Count_ShouldReturnCorrectNumberOfScrapers()
        {
            // Act
            var count = _scraperFactory.Count;

            // Assert
            Assert.InRange(count, 1, int.MaxValue);
            Assert.Equal(_scraperFactory.GetAvailableScrapers().Count(), count);
        }

        /// <summary>
        /// 测试手动注册自定义工厂
        /// </summary>
        [Fact]
        public void RegisterFactory_ShouldAddNewScraper()
        {
            // Arrange
            var initialCount = _scraperFactory.Count;

            // Act
            _scraperFactory.RegisterFactory("TestScraper", () =>
            {
                return new TestScraper();
            });

            // Assert
            Assert.Equal(initialCount + 1, _scraperFactory.Count);
            Assert.True(_scraperFactory.IsRegistered("TestScraper"));
            
            var testScraper = _scraperFactory.CreateScraper("TestScraper");
            Assert.NotNull(testScraper);
            Assert.Equal("TestScraper", testScraper.Name);
        }

        /// <summary>
        /// 测试 ForEachFactory 遍历所有工厂
        /// </summary>
        [Fact]
        public void ForEachFactory_ShouldIterateAllScrapers()
        {
            // Arrange
            var visitedScrapers = new List<string>();

            // Act
            _scraperFactory.ForEachFactory((name, factoryFunc) =>
            {
                visitedScrapers.Add(name);
            });

            // Assert
            Assert.Equal(_scraperFactory.Count, visitedScrapers.Count);
            Assert.Contains("JavBus", visitedScrapers);
        }

        /// <summary>
        /// 测试查找可以处理关键字的刮削器
        /// </summary>
        [Fact]
        public async Task FindScrapers_ShouldReturnMatchingScrapers()
        {
            // Act
            var scrapers = _scraperFactory.FindScrapers("ABW-001").ToList();

            // Assert
            Assert.NotEmpty(scrapers);
            foreach (var scraper in scrapers)
            {
                Assert.True(scraper.CanHandle("ABW-001"));
            }
        }

        /// <summary>
        /// 测试多次创建同一刮削器返回不同实例
        /// </summary>
        [Fact]
        public void CreateScraper_ShouldReturnNewInstanceEachTime()
        {
            // Act
            var scraper1 = _scraperFactory.CreateScraper("JavBus");
            var scraper2 = _scraperFactory.CreateScraper("JavBus");

            // Assert
            Assert.NotNull(scraper1);
            Assert.NotNull(scraper2);
            Assert.NotSame(scraper1, scraper2);
        }

        /// <summary>
        /// 测试线程安全性 - 并发创建多个刮削器实例
        /// </summary>
        [Fact]
        public async Task ConcurrentCreateScraper_ShouldNotThrowExceptions()
        {
            // Arrange
            var tasks = Enumerable.Range(0, 10).Select(i =>
                Task.Run(() =>
                {
                    var scraper = _scraperFactory.CreateScraper("JavBus");
                    return scraper != null;
                }));

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result));
        }

        /// <summary>
        /// 测试空名称处理
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateScraper_WithNullOrEmptyName_ShouldThrowException(string name)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _scraperFactory.CreateScraper(name));
        }

        /// <summary>
        /// 测试 RegisterFactory 参数验证
        /// </summary>
        [Fact]
        public void RegisterFactory_WithNullParameters_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _scraperFactory.RegisterFactory(null, () => new TestScraper()));
            
            Assert.Throws<ArgumentNullException>(() => 
                _scraperFactory.RegisterFactory("Test", null));
        }
    }

    /// <summary>
    /// 用于测试的简单刮削器实现
    /// </summary>
    public class TestScraper : IScraperV2
    {
        public string Name => "TestScraper";

        public bool CanHandle(string keyword) => true;

        public Task<JavPerson> GetPersonAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task<JavVideo> GetDetailsAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            throw new NotImplementedException();
        }
    }
}
