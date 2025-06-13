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
    /// Heyzo 网站刮削器实现
    /// </summary>
    public class HeyzoScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "Heyzo";

        public HeyzoScraperV2(
            ILogger<HeyzoScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService,
            ScraperOptions options)
            : base(options.SiteSettings["Heyzo"].BaseUrl, logger, clientFactory.CreateClient("Heyzo"))
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            
            // 检查是否符合 Heyzo 编号格式（HEYZO-数字）
            return System.Text.RegularExpressions.Regex.IsMatch(
                keyword.ToUpper(),
                @"^(?:HEYZO[-\s]?)?\d{4}$");
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "search", keyword);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                // 提取纯数字编号
                var match = System.Text.RegularExpressions.Regex.Match(keyword, @"\d{4}");
                if (!match.Success) return new List<JavVideoIndex>();

                var movieId = match.Value;
                var detailUrl = BuildUrl($"/moviepages/{movieId}/index.html");

                // Heyzo 没有搜索功能，直接访问详情页
                var doc = await GetHtmlDocumentAsync(detailUrl);
                if (doc == null) return new List<JavVideoIndex>();

                var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie')]//h1");
                var imageNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie')]//img");

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
                var titleNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie')]//h1");
                video.Title = CleanHtmlText(titleNode?.InnerText);

                // 提取番号（从URL中提取）
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/moviepages/(\d{4})/");
                if (match.Success)
                {
                    video.Code = $"HEYZO-{match.Groups[1].Value}";
                }

                // 提取发行日期
                var dateNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'release-day')]");
                if (dateNode != null && DateTime.TryParse(dateNode.InnerText, out var releaseDate))
                {
                    video.ReleaseDate = releaseDate;
                }

                // 提取演员信息
                var actorNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'actor')]//a");
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
                var coverNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'movie')]//img[contains(@class, 'thumb')]");
                video.CoverUrl = coverNode != null ? BuildUrl(coverNode.GetAttributeValue("src", "")) : null;

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
                var nameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'actor-name')]");
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