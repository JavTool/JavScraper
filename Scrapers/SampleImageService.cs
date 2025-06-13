using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 示例图片提取服务
    /// </summary>
    public class SampleImageService
    {
        private readonly ILogger<SampleImageService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ScraperCacheService _cacheService;

        public SampleImageService(
            ILogger<SampleImageService> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
        {
            _logger = logger;
            _httpClient = clientFactory.CreateClient("SampleImage");
            _cacheService = cacheService;
        }

        /// <summary>
        /// 提取视频示例图片
        /// </summary>
        /// <param name="url">视频页面地址</param>
        /// <returns>示例图片地址列表</returns>
        public async Task<List<string>> ExtractSampleImagesAsync(string url)
        {
            var cacheKey = _cacheService.GenerateKey("SampleImage", "extract", url);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // 提取示例图片
                    var images = new List<string>();

                    // 通用图片选择器
                    var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'sample-box')]//img") ??
                                    doc.DocumentNode.SelectNodes("//div[contains(@class, 'sample-image')]//img") ??
                                    doc.DocumentNode.SelectNodes("//div[contains(@class, 'preview-images')]//img");

                    if (imageNodes != null)
                    {
                        foreach (var node in imageNodes)
                        {
                            var imageUrl = node.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                // 处理相对路径
                                if (imageUrl.StartsWith("/"))
                                {
                                    var baseUri = new Uri(url);
                                    imageUrl = $"{baseUri.Scheme}://{baseUri.Host}{imageUrl}";
                                }

                                // 处理缩略图到原图的转换
                                imageUrl = imageUrl.Replace("thumbnail", "cover")
                                                 .Replace("thumb", "image")
                                                 .Replace("-s.", ".");

                                images.Add(imageUrl);
                            }
                        }
                    }

                    return images;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"提取示例图片失败: {url}");
                    return new List<string>();
                }
            });
        }

        /// <summary>
        /// 提取演员写真图片
        /// </summary>
        /// <param name="url">演员页面地址</param>
        /// <returns>写真图片地址列表</returns>
        public async Task<List<string>> ExtractPersonImagesAsync(string url)
        {
            var cacheKey = _cacheService.GenerateKey("SampleImage", "person", url);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var html = await response.Content.ReadAsStringAsync();

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // 提取写真图片
                    var images = new List<string>();

                    // 通用图片选择器
                    var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'gallery')]//img") ??
                                    doc.DocumentNode.SelectNodes("//div[contains(@class, 'photos')]//img");

                    if (imageNodes != null)
                    {
                        foreach (var node in imageNodes)
                        {
                            var imageUrl = node.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(imageUrl))
                            {
                                // 处理相对路径
                                if (imageUrl.StartsWith("/"))
                                {
                                    var baseUri = new Uri(url);
                                    imageUrl = $"{baseUri.Scheme}://{baseUri.Host}{imageUrl}";
                                }

                                // 处理缩略图到原图的转换
                                imageUrl = imageUrl.Replace("thumbnail", "cover")
                                                 .Replace("thumb", "image")
                                                 .Replace("-s.", ".");

                                images.Add(imageUrl);
                            }
                        }
                    }

                    return images;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"提取写真图片失败: {url}");
                    return new List<string>();
                }
            });
        }
    }
}