using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using JavScraper.Common.Scrapers;

namespace JavScraper.Common.Test
{
    /// <summary>
    /// 测试基础类，提供通用的测试辅助方法
    /// </summary>
    public class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ScraperFactory ScraperFactory;
        protected readonly Mock<ILogger<ScraperFactory>> LoggerMock;

        public TestBase()
        {
            // 设置依赖注入容器
            var services = new ServiceCollection();
            
            // 添加日志服务
            services.AddLogging();
            
            // 添加 HttpClient
            services.AddSingleton(new HttpClient());
            
            // 添加其他必要的服务
            services.AddSingleton<ScraperFactory>();
            
            ServiceProvider = services.BuildServiceProvider();
            
            // 创建日志 Mock
            LoggerMock = new Mock<ILogger<ScraperFactory>>();
            
            // 创建 ScraperFactory 实例
            ScraperFactory = ServiceProvider.GetRequiredService<ScraperFactory>();
        }

        /// <summary>
        /// 获取所有可用的刮削器名称
        /// </summary>
        protected IEnumerable<string> GetAvailableScraperNames()
        {
            return ScraperFactory.GetAvailableScrapers();
        }

        /// <summary>
        /// 验证刮削器是否已注册
        /// </summary>
        protected void AssertScraperRegistered(string scraperName)
        {
            Assert.True(ScraperFactory.IsRegistered(scraperName), 
                $"刮削器 '{scraperName}' 应该已注册");
        }

        /// <summary>
        /// 验证刮削器是否未注册
        /// </summary>
        protected void AssertScraperNotRegistered(string scraperName)
        {
            Assert.False(ScraperFactory.IsRegistered(scraperName), 
                $"刮削器 '{scraperName}' 不应该已注册");
        }

        /// <summary>
        /// 创建刮削器实例
        /// </summary>
        protected BaseScraper CreateScraper(string name)
        {
            if (ScraperFactory.TryCreateScraper(name, out var scraper))
            {
                return scraper;
            }
            throw new ArgumentException($"无法创建刮削器：{name}");
        }

        public void Dispose()
        {
            ServiceProvider?.GetService<HttpClient>()?.Dispose();
        }
    }
}
