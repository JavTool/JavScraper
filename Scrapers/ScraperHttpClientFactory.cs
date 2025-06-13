using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器HTTP客户端工厂
    /// </summary>
    public class ScraperHttpClientFactory
    {
        private readonly ILogger<ScraperHttpClientFactory> _logger;
        private readonly ScraperOptions _options;

        public ScraperHttpClientFactory(ILogger<ScraperHttpClientFactory> logger, ScraperOptions options)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// 创建HTTP客户端
        /// </summary>
        /// <param name="scraperName">刮削器名称</param>
        /// <returns>配置好的HTTP客户端</returns>
        public HttpClient CreateClient(string scraperName)
        {
            var handler = CreateHandler(scraperName);
            var client = new HttpClient(handler);

            // 配置超时
            var timeout = GetTimeout(scraperName);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            // 配置默认请求头
            ConfigureHeaders(client, scraperName);

            return client;
        }

        /// <summary>
        /// 创建带有重试策略的Handler
        /// </summary>
        private HttpMessageHandler CreateHandler(string scraperName)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            // 配置代理
            ConfigureProxy(handler, scraperName);

            // 创建重试策略
            var retryPolicy = CreateRetryPolicy(scraperName);

            return new PolicyHttpMessageHandler(retryPolicy)
            {
                InnerHandler = handler
            };
        }

        /// <summary>
        /// 创建重试策略
        /// </summary>
        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(string scraperName)
        {
            var siteSettings = GetSiteSettings(scraperName);
            var maxRetries = siteSettings?.MaxRetries ?? _options.MaxRetries;
            var retryDelay = siteSettings?.RetryDelay ?? _options.RetryDelay;

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    maxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(retryDelay * retryAttempt),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception.Exception,
                            $"重试请求 {scraperName} (尝试 {retryCount} of {maxRetries})");
                    });
        }

        /// <summary>
        /// 配置代理服务器
        /// </summary>
        private void ConfigureProxy(HttpClientHandler handler, string scraperName)
        {
            var proxySettings = GetProxySettings(scraperName);
            if (proxySettings?.Enabled == true && !string.IsNullOrEmpty(proxySettings.Host))
            {
                var proxyUri = new Uri($"http://{proxySettings.Host}:{proxySettings.Port}");
                handler.Proxy = new WebProxy(proxyUri);

                if (!string.IsNullOrEmpty(proxySettings.Username))
                {
                    handler.Proxy.Credentials = new NetworkCredential(
                        proxySettings.Username,
                        proxySettings.Password);
                }
            }
        }

        /// <summary>
        /// 配置请求头
        /// </summary>
        private void ConfigureHeaders(HttpClient client, string scraperName)
        {
            var siteSettings = GetSiteSettings(scraperName);
            if (siteSettings?.Headers != null)
            {
                foreach (var header in siteSettings.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// 获取站点特定设置
        /// </summary>
        private SiteSpecificSettings GetSiteSettings(string scraperName)
        {
            return _options.SiteSettings.TryGetValue(scraperName, out var settings) ? settings : null;
        }

        /// <summary>
        /// 获取代理设置
        /// </summary>
        private ProxySettings GetProxySettings(string scraperName)
        {
            var siteSettings = GetSiteSettings(scraperName);
            return siteSettings?.SiteProxy ?? _options.Proxy;
        }

        /// <summary>
        /// 获取超时设置
        /// </summary>
        private int GetTimeout(string scraperName)
        {
            var siteSettings = GetSiteSettings(scraperName);
            return siteSettings?.Timeout ?? _options.DefaultTimeout;
        }
    }
}