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
    /// FC2 网站刮削器实现
    /// </summary>
    public class FC2ScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "FC2";

        public FC2ScraperV2(
            ILogger<FC2ScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService,
            ScraperOptions options)
            : base(options.SiteSettings["FC2"].BaseUrl, logger, clientFactory.CreateClient("FC2"))
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            
            // 检查是否符合 FC2 编号格式（纯数字）
            return System.Text.RegularExpressions.Regex.IsMatch(
                keyword,
                @"^\d{6,7}$");
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var cacheKey = _cacheService.GenerateKey(Name, "search", keyword);
            return await _cacheService.GetOrAddAsync(cacheKey, async () =>
            {
                var searchUrl = BuildUrl($"/search/search.php?keyword=FC2-PPV-{keyword}");
                var doc = await GetHtmlDocumentAsync(searchUrl);
                if (doc == null) return new List<JavVideoIndex>();

                var results = new List<JavVideoIndex>();
                var items = doc.DocumentNode.SelectNodes("//div[contains(@class, 'items_article')]//li");
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var titleNode = item.SelectSingleNode(".//div[contains(@class, 'title_wrapper')]");
                        var linkNode = item.SelectSingleNode(".//a");
                        var imageNode = item.SelectSingleNode(".//img");

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
                var titleNode = doc.DocumentNode.SelectSingleNode("//h3[contains(@class, 'title')]");
                video.Title = CleanHtmlText(titleNode?.InnerText);

                // 提取番号（从URL或标题中提取）
                var match = System.Text.RegularExpressions.Regex.Match(url, @"FC2-PPV-(\d+)");
                if (match.Success)
                {
                    video.Code = $"FC2-PPV-{match.Groups[1].Value}";
                }

                // 提取发行日期
                var dateNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'added')]");
                if (dateNode != null && DateTime.TryParse(dateNode.InnerText, out var releaseDate))
                {
                    video.ReleaseDate = releaseDate;
                }

                // 提取卖家信息（作为演员）
                var sellerNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'seller')]/a");
                if (sellerNode != null)
                {
                    video.Actors = new List<JavPerson>
                    {
                        new JavPerson
                        {
                            Name = CleanHtmlText(sellerNode.InnerText),
                            Url = BuildUrl(sellerNode.GetAttributeValue("href", "")),
                            Source = Name
                        }
                    };
                }

                // 提取封面图
                var coverNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'main_thumbnail')]//img");
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

                // 提取卖家名称
                var nameNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'seller_name')]");
                person.Name = CleanHtmlText(nameNode?.InnerText);

                // FC2 通常没有卖家头像，所以这里不设置 ImageUrl

                return person;
            });
        }
    }
}