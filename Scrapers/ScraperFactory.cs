using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器工厂类
    /// </summary>
    public class ScraperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScraperFactory> _logger;
        private readonly Dictionary<string, Type> _scraperTypes;

        /// <summary>
        /// 初始化刮削器工厂
        /// </summary>
        public ScraperFactory(IServiceProvider serviceProvider, ILogger<ScraperFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _scraperTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            RegisterScrapers();
        }

        /// <summary>
        /// 注册所有刮削器类型
        /// </summary>
        private void RegisterScrapers()
        {
            // 通过反射获取所有实现了 IScraperV2 的类型
            var scraperTypes = typeof(IScraperV2).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IScraperV2).IsAssignableFrom(t));

            foreach (var type in scraperTypes)
            {
                try
                {
                    // 创建临时实例以获取名称
                    var scraper = (IScraperV2)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                    _scraperTypes.Add(scraper.Name, type);
                    _logger.LogInformation($"注册刮削器: {scraper.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"注册刮削器失败: {type.Name}");
                }
            }
        }

        /// <summary>
        /// 获取所有已注册的刮削器名称
        /// </summary>
        public IEnumerable<string> GetAvailableScrapers()
        {
            return _scraperTypes.Keys;
        }

        /// <summary>
        /// 创建指定名称的刮削器实例
        /// </summary>
        /// <param name="name">刮削器名称</param>
        /// <returns>刮削器实例</returns>
        public IScraperV2 CreateScraper(string name)
        {
            if (!_scraperTypes.TryGetValue(name, out var type))
            {
                throw new ArgumentException($"未找到名称为 {name} 的刮削器");
            }

            try
            {
                return (IScraperV2)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建刮削器实例失败: {name}");
                throw;
            }
        }

        /// <summary>
        /// 查找可以处理指定关键字的刮削器
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <returns>可以处理的刮削器列表</returns>
        public IEnumerable<IScraperV2> FindScrapers(string keyword)
        {
            foreach (var name in _scraperTypes.Keys)
            {
                var scraper = CreateScraper(name);
                if (scraper.CanHandle(keyword))
                {
                    yield return scraper;
                }
            }
        }
    }
}