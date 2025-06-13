using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using JavScraper.Domain;
using HtmlAgilityPack;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器基础抽象类，提供通用功能实现
    /// </summary>
    public abstract class ScraperBase : IScraperV2
    {
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;
        protected readonly string _baseUrl;
        protected readonly SampleImageService _sampleImageService;

        /// <summary>
        /// 获取刮削器名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 初始化刮削器
        /// </summary>
        /// <param name="baseUrl">基础URL</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="sampleImageService">示例图片服务</param>
        protected ScraperBase(
            string baseUrl,
            ILogger logger,
            HttpClient httpClient,
            SampleImageService sampleImageService = null)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _sampleImageService = sampleImageService;

            // 配置默认请求头
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// 检查是否可以处理指定的关键字
        /// </summary>
        public abstract bool CanHandle(string keyword);

        /// <summary>
        /// 搜索视频信息
        /// </summary>
        public abstract Task<List<JavVideoIndex>> SearchAsync(string keyword);

        /// <summary>
        /// 获取视频详细信息
        /// </summary>
        public abstract Task<JavVideo> GetDetailsAsync(string url);

        /// <summary>
        /// 获取演员信息
        /// </summary>
        public abstract Task<JavPerson> GetPersonAsync(string url);

        /// <summary>
        /// 获取视频示例图片
        /// </summary>
        /// <param name="url">视频页面地址</param>
        /// <returns>示例图片地址列表</returns>
        public virtual async Task<List<string>> GetSampleImagesAsync(string url)
        {
            if (_sampleImageService == null)
            {
                _logger.LogWarning($"示例图片服务未初始化: {Name}");
                return new List<string>();
            }

            try
            {
                return await _sampleImageService.ExtractSampleImagesAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取示例图片失败: {url}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取演员写真图片
        /// </summary>
        /// <param name="url">演员页面地址</param>
        /// <returns>写真图片地址列表</returns>
        public virtual async Task<List<string>> GetPersonImagesAsync(string url)
        {
            if (_sampleImageService == null)
            {
                _logger.LogWarning($"示例图片服务未初始化: {Name}");
                return new List<string>();
            }

            try
            {
                return await _sampleImageService.ExtractPersonImagesAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取写真图片失败: {url}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取HTML文档
        /// </summary>
        protected async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取页面失败: {url}");
                return null;
            }
        }

        /// <summary>
        /// 构建完整URL
        /// </summary>
        protected string BuildUrl(string path)
        {
            return new Uri(new Uri(_baseUrl), path).ToString();
        }

        /// <summary>
        /// 清理HTML文本
        /// </summary>
        protected string CleanHtmlText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Trim()
                      .Replace("\n", " ")
                      .Replace("\r", " ")
                      .Replace("\t", " ")
                      .Replace("&nbsp;", " ")
                      .Replace("  ", " ");
        }
    }
}