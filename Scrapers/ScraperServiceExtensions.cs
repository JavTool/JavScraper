using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JavScraper.Scrapers.Implementations;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器服务扩展类
    /// </summary>
    public static class ScraperServiceExtensions
    {
        /// <summary>
        /// 添加刮削器服务
        /// </summary>
        public static IServiceCollection AddScraperServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册配置
            var options = new ScraperOptions();
            configuration.GetSection("Scraper").Bind(options);
            services.AddSingleton(options);

            // 注册缓存服务
            services.AddMemoryCache();
            services.AddSingleton<ScraperCacheService>();

            // 注册HTTP客户端工厂
            services.AddSingleton<ScraperHttpClientFactory>();

            // 注册刮削器工厂
            services.AddSingleton<ScraperFactory>();

            // 注册具体的刮削器实现
            services.AddTransient<JavDBScraperV2>();
            // 在这里添加其他刮削器的注册

            return services;
        }

        /// <summary>
        /// 使用刮削器服务
        /// </summary>
        public static IServiceProvider UseScraperServices(this IServiceProvider serviceProvider)
        {
            // 初始化刮削器工厂
            var factory = serviceProvider.GetRequiredService<ScraperFactory>();

            // 验证所有刮削器是否可用
            foreach (var scraperName in factory.GetAvailableScrapers())
            {
                try
                {
                    factory.CreateScraper(scraperName);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"初始化刮削器失败: {scraperName}", ex);
                }
            }

            return serviceProvider;
        }
    }
}