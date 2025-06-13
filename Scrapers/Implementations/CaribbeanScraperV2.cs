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
    /// Caribbean 网站刮削器实现
    /// </summary>
    public class CaribbeanScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "Caribbean";

        public CaribbeanScraperV2(
            ILogger<CaribbeanScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService,
            ScraperOptions options)
            : base(options.SiteSettings["Caribbean"].BaseUrl, logger, clientFactory.CreateClient("Caribbean"))
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            
            // 检查是否符合 Caribbean 编号格式（6位数字-3位数字）
            return System.Text.RegularExpressions.Regex.IsMatch(
                keyword,
                @"^\d{6}-\d{3}$");
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "search", keyword);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                // Caribbean 没有搜索功能，直接访问详情页
                var detailUrl = BuildUrl($"/moviepages/{keyword}/index.html");
                var doc = await GetHtmlDocumentAsync(detailUrl);
                if (doc == null) return new List<JavVideoIndex>();

                var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-info')]//h1");
                var imageNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-thumb')]//img");

                if (titleNode != null)
                {
                    return new List<JavVideoIndex>
                    {
                        new JavVideoIndex
                        {
                            Title = CleanHtmlText(titleNode.InnerText),
                            Url = detailUrl,
                            ImageUrl = imageNode?.GetAttributeValue("src", ""),
                            Source = Name
                        }
                    };
                }

                return new List<JavVideoIndex>();
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
                var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-info')]//h1");
                video.Title = CleanHtmlText(titleNode?.InnerText);

                // 提取番号（从URL中提取）
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/moviepages/(\d{6}-\d{3})/");
                if (match.Success)
                {
                    video.Code = match.Groups[1].Value;
                }

                // 提取发行日期
                var dateNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-info')]//span[contains(@class, 'date')]");
                if (dateNode != null && DateTime.TryParse(dateNode.InnerText, out var releaseDate))
                {
                    video.ReleaseDate = releaseDate;
                }

                // 提取演员信息
                var actorNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'movie-info')]//span[contains(@class, 'actor')]//a");
                if (actorNodes != null)
                {
                    video.Actors = actorNodes.Select(node => new JavPerson
                    {
                        Name = CleanHtmlText(node.InnerText),
                        Url = BuildUrl(node.GetAttributeValue("href", "")),
                        Source = Name
                    }).ToList();
                }

                // 提取封面图
                var coverNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie-thumb')]//img");
                if (coverNode != null)
                {
                    video.CoverUrl = BuildUrl(coverNode.GetAttributeValue("src", ""));
                }

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
                var nameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'actor-info')]//h1");
                person.Name = CleanHtmlText(nameNode?.InnerText);

                // 提取演员照片
                var photoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'actor-photo')]//img");
                if (photoNode != null)
                {
                    person.ImageUrl = BuildUrl(photoNode.GetAttributeValue("src", ""));
                }

                return person;
            });
        }
    }
}