using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器缓存服务
    /// </summary>
    public class ScraperCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ScraperCacheService> _logger;
        private readonly ScraperOptions _options;

        public ScraperCacheService(IMemoryCache cache, ILogger<ScraperCacheService> logger, ScraperOptions options)
        {
            _cache = cache;
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// 获取或添加缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存键</param>
        /// <param name="factory">数据获取函数</param>
        /// <returns>缓存的数据</returns>
        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory)
        {
            if (!_options.EnableCache)
            {
                return await factory();
            }

            try
            {
                return await _cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpiration);
                    var value = await factory();
                    _logger.LogDebug($"添加缓存: {key}");
                    return value;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"缓存操作失败: {key}");
                return await factory();
            }
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        public string GenerateKey(string scraperName, string operation, params object[] parameters)
        {
            var key = $"{scraperName}:{operation}";
            if (parameters != null && parameters.Length > 0)
            {
                key += $":{string.Join(":", parameters)}";
            }
            return key;
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
                _logger.LogDebug($"移除缓存: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"移除缓存失败: {key}");
            }
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void Clear()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0);
                    _logger.LogInformation("清除所有缓存");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存失败");
            }
        }
    }
}