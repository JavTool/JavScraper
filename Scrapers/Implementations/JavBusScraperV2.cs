using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Domain;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers.Implementations
{
    /// <summary>
    /// JavBus 网站刮削器实现
    /// </summary>
    public class JavBusScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "JavBus";

        public JavBusScraperV2(
            ILogger<JavBusScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService,
            ScraperOptions options)
            : base(options.SiteSettings["JavBus"].BaseUrl, logger, clientFactory.CreateClient("JavBus"))
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            
            // 检查是否符合常见的 JavBus 编号格式
            return System.Text.RegularExpressions.Regex.IsMatch(
                keyword.ToUpper(),
                @"^[A-Z]{2,5}-\d{2,5}$");
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "search", keyword);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var searchUrl = BuildUrl($"/search/{Uri.EscapeDataString(keyword)}");
                var doc = await GetHtmlDocumentAsync(searchUrl);
                if (doc == null) return new List<JavVideoIndex>();

                var results = new List<JavVideoIndex>();
                var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'movie-box')]//div[contains(@class, 'photo-info')]");
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var titleNode = item.SelectSingleNode(".//span[contains(@class, 'title')]");
                        var linkNode = item.SelectSingleNode("ancestor::a[1]");
                        var imageNode = item.SelectSingleNode("ancestor::a[1]//img");

                        if (titleNode != null && linkNode != null)
                        {
                            results.Add(new JavVideoIndex
                            {
                                Title = CleanHtmlText(titleNode.InnerText),
                                Url = BuildUrl(linkNode.GetAttributeValue("href", "")),
                                ImageUrl = imageNode?.GetAttributeValue("src", ""),
                                Source = Name
                            });
                        }
                    }
                }

                return results;
            });
        }

        public override async Task<JavVideo> GetDetailsAsync(string url)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "details", url);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var doc = await GetHtmlDocumentAsync(url);
                if (doc == null) return null;

                var video = new JavVideo
                {
                    Url = url,
                    Source = Name
                };

                // 提取标题
                var titleNode = doc.DocumentNode.SelectSingleNode("//h3");
                video.Title = CleanHtmlText(titleNode?.InnerText);

                // 提取番号
                var codeNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'movie-code')]");
                video.Code = CleanHtmlText(codeNode?.InnerText);

                // 提取发行日期
                var dateNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'release-date')]");
                if (dateNode != null && DateTime.TryParse(dateNode.InnerText, out var releaseDate))
                {
                    video.ReleaseDate = releaseDate;
                }

                // 提取演员信息
                var actorNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'star-box')]//a");
                if (actorNodes != null)
                {
                    video.Actors = actorNodes.Select(node => new JavPerson
                    {
                        Name = CleanHtmlText(node.InnerText),
                        Url = BuildUrl(node.GetAttributeValue("href", ""))
                    }).ToList();
                }

                // 提取封面图
                var coverNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-cover')]//img");
                video.CoverUrl = coverNode?.GetAttributeValue("src", "");

                return video;
            });
        }

        public override async Task<JavPerson> GetPersonAsync(string url)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "person", url);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var doc = await GetHtmlDocumentAsync(url);
                if (doc == null) return null;

                var person = new JavPerson
                {
                    Url = url,
                    Source = Name
                };

                // 提取演员名称
                var nameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'avatar-box')]//h4");
                person.Name = CleanHtmlText(nameNode?.InnerText);

                // 提取演员照片
                var photoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'avatar-box')]//img");
                person.ImageUrl = photoNode?.GetAttributeValue("src", "");

                return person;
            });
        }
    }
}