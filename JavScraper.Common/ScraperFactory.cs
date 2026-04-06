using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JavScraper.Common.Scrapers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JavScraper.Common
{
    /// <summary>
    /// 刮削器工厂委托
    /// </summary>
    /// <typeparam name="T">刮削器类型</typeparam>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="logger">日志记录器</param>
    /// <returns>刮削器实例</returns>
    public delegate T ScraperFactoryDelegate<out T>(IServiceProvider serviceProvider, ILogger logger) where T : BaseScraper;

    /// <summary>
    /// 刮削器工厂类
    /// 参考 metatube-sdk-go provider 设计模式
    /// </summary>
    public class ScraperFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScraperFactory> _logger;
        
        // 使用读写锁保护工厂集合，确保线程安全
        private readonly ReaderWriterLockSlim _factoryLock = new ReaderWriterLockSlim();
        
        // 刮削器工厂字典，key 为刮削器名称
        private readonly Dictionary<string, Func<BaseScraper>> _factories = new Dictionary<string, Func<BaseScraper>>(StringComparer.OrdinalIgnoreCase);
        
        // 已注册的刮削器类型（用于向后兼容）
        private readonly Dictionary<string, Type> _scraperTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化刮削器工厂
        /// </summary>
        public ScraperFactory(IServiceProvider serviceProvider, ILogger<ScraperFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            RegisterScrapers();
        }

        /// <summary>
        /// 注册所有刮削器类型（自动发现模式）
        /// </summary>
        private void RegisterScrapers()
        {
            // 通过反射获取所有继承了 BaseScraper 的非抽象类型
            var scraperTypes = typeof(BaseScraper).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(BaseScraper).IsAssignableFrom(t));

            foreach (var type in scraperTypes)
            {
                try
                {
                    RegisterScraperType(type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"注册刮削器失败：{type.Name}");
                }
            }
        }

        /// <summary>
        /// 注册单个刮削器类型
        /// </summary>
        /// <param name="scraperType">刮削器类型</param>
        private void RegisterScraperType(Type scraperType)
        {
            // 创建临时实例以获取名称
            var scraper = (BaseScraper)ActivatorUtilities.CreateInstance(_serviceProvider, scraperType);
            
            _factoryLock.EnterWriteLock();
            try
            {
                // 同时注册工厂和类型
                _factories[scraper.Name] = () => (BaseScraper)ActivatorUtilities.CreateInstance(_serviceProvider, scraperType);
                _scraperTypes[scraper.Name] = scraperType;
                
                _logger.LogInformation($"注册刮削器：{scraper.Name} (类型：{scraperType.Name})");
            }
            finally
            {
                _factoryLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 手动注册刮削器工厂函数
        /// </summary>
        /// <param name="name">刮削器名称</param>
        /// <param name="factory">工厂函数</param>
        public void RegisterFactory(string name, Func<BaseScraper> factory)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factoryLock.EnterWriteLock();
            try
            {
                _factories[name] = factory;
                _logger.LogInformation($"注册自定义刮削器工厂：{name}");
            }
            finally
            {
                _factoryLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取所有已注册的刮削器名称
        /// </summary>
        public IEnumerable<string> GetAvailableScrapers()
        {
            _factoryLock.EnterReadLock();
            try
            {
                return _factories.Keys.ToList();
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 检查指定名称的刮削器是否已注册
        /// </summary>
        public bool IsRegistered(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            _factoryLock.EnterReadLock();
            try
            {
                return _factories.ContainsKey(name);
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 创建指定名称的刮削器实例
        /// </summary>
        /// <param name="name">刮削器名称</param>
        /// <returns>刮削器实例</returns>
        /// <exception cref="ArgumentException">当刮削器未注册时抛出</exception>
        public BaseScraper CreateScraper(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("刮削器名称不能为空", nameof(name));

            _factoryLock.EnterReadLock();
            Func<BaseScraper> factory;
            try
            {
                if (!_factories.TryGetValue(name, out factory))
                {
                    _factoryLock.ExitReadLock();
                    throw new ArgumentException($"未找到名称为 {name} 的刮削器");
                }
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }

            try
            {
                return factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建刮削器实例失败：{name}");
                throw;
            }
        }

        /// <summary>
        /// 尝试创建指定名称的刮削器实例
        /// </summary>
        /// <param name="name">刮削器名称</param>
        /// <param name="scraper">如果成功则返回刮削器实例</param>
        /// <returns>是否成功创建</returns>
        public bool TryCreateScraper(string name, out BaseScraper scraper)
        {
            scraper = null;
            
            if (string.IsNullOrWhiteSpace(name))
                return false;

            _factoryLock.EnterReadLock();
            Func<BaseScraper> factory;
            try
            {
                if (!_factories.TryGetValue(name, out factory))
                {
                    return false;
                }
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }

            try
            {
                scraper = factory();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"尝试创建刮削器实例失败：{name}");
                return false;
            }
        }

        /// <summary>
        /// 查找可以处理指定关键字的刮削器
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <returns>可以处理的刮削器列表</returns>
        public IEnumerable<BaseScraper> FindScrapers(string keyword)
        {
            List<string> scraperNames;
            _factoryLock.EnterReadLock();
            try
            {
                scraperNames = _factories.Keys.ToList();
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }

            var result = new List<BaseScraper>();
            foreach (var name in scraperNames)
            {
                try
                {
                    var scraper = CreateScraper(name);
                    if (scraper.CanHandle(keyword))
                    {
                        result.Add(scraper);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"检查刮削器 {name} 时出错");
                }
            }

            return result;
        }

        /// <summary>
        /// 遍历所有已注册的工厂（类似 Go 的 Range 模式）
        /// </summary>
        /// <param name="action">对每个工厂执行的操作</param>
        public void ForEachFactory(Action<string, Func<BaseScraper>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _factoryLock.EnterReadLock();
            try
            {
                foreach (var kvp in _factories)
                {
                    action(kvp.Key, kvp.Value);
                }
            }
            finally
            {
                _factoryLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 获取已注册的工厂数量
        /// </summary>
        public int Count
        {
            get
            {
                _factoryLock.EnterReadLock();
                try
                {
                    return _factories.Count;
                }
                finally
                {
                    _factoryLock.ExitReadLock();
                }
            }
        }
    }
}