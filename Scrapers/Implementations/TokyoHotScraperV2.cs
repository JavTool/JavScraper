using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Domain;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers.Implementations
{
    /// <summary>
    /// Tokyo Hot 网站刮削器实现
    /// </summary>
    public class TokyoHotScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "TokyoHot";

        public TokyoHotScraperV2(
            ILogger<TokyoHotScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
            : base("https://my.tokyo-hot.com", logger, clientFactory.CreateClient())
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   (keyword.StartsWith("N", StringComparison.OrdinalIgnoreCase) ||
                    keyword.StartsWith("K", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var url = $"{_baseUrl}/product?q={Uri.EscapeDataString(keyword)}";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return new List<JavVideoIndex>();

            var results = new List<JavVideoIndex>();
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='product']");
            
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var linkNode = node.SelectSingleNode(".//h3/a");
                    if (linkNode == null) continue;

                    var detailUrl = BuildUrl(linkNode.GetAttributeValue("href", ""));
                    var imgNode = node.SelectSingleNode(".//img");
                    
                    var video = new JavVideoIndex
                    {
                        Provider = Name,
                        Url = detailUrl,
                        Title = CleanHtmlText(linkNode.InnerText),
                        Cover = imgNode?.GetAttributeValue("src", ""),
                        Num = ExtractVideoNumber(detailUrl)
                    };

                    results.Add(video);
                }
            }

            return results;
        }

        public override async Task<JavVideo> GetDetailsAsync(string url)
        {
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return null;

            var video = new JavVideo
            {
                Provider = Name,
                Url = url
            };

            // 提取标题
            var titleNode = doc.DocumentNode.SelectSingleNode("//h2[@class='product-name']");
            video.Title = CleanHtmlText(titleNode?.InnerText);

            // 提取封面
            var coverNode = doc.DocumentNode.SelectSingleNode("//div[@class='product-image']//img");
            video.Cover = coverNode?.GetAttributeValue("src", "");

            // 提取番号
            var idNode = doc.DocumentNode.SelectSingleNode("//div[@class='product-code']");
            video.Num = CleanHtmlText(idNode?.InnerText);

            // 提取发行日期
            var dateNode = doc.DocumentNode.SelectSingleNode("//div[@class='product-date']");
            video.Date = CleanHtmlText(dateNode?.InnerText);

            // 提取时长
            var lengthNode = doc.DocumentNode.SelectSingleNode("//div[@class='product-duration']");
            video.Runtime = CleanHtmlText(lengthNode?.InnerText);

            // 提取制作商
            video.Maker = "Tokyo Hot";
            video.Studio = "Tokyo Hot";

            // 提取类别
            var genreNodes = doc.DocumentNode.SelectNodes("//div[@class='product-categories']//a");
            if (genreNodes != null)
            {
                video.Genres = genreNodes.Select(n => CleanHtmlText(n.InnerText)).ToList();
            }

            // 提取演员
            var actorNodes = doc.DocumentNode.SelectNodes("//div[@class='product-actors']//a");
            if (actorNodes != null)
            {
                video.Actors = actorNodes.Select(n => new JavPerson
                {
                    Name = CleanHtmlText(n.InnerText),
                    Url = BuildUrl(n.GetAttributeValue("href", ""))
                }).ToList();
            }

            // 提取剧情介绍
            var plotNode = doc.DocumentNode.SelectSingleNode("//div[@class='product-description']");
            video.Plot = CleanHtmlText(plotNode?.InnerText);

            // 提取样品图片
            var sampleNodes = doc.DocumentNode.SelectNodes("//div[@class='product-samples']//img");
            if (sampleNodes != null)
            {
                video.Samples = sampleNodes.Select(n => n.GetAttributeValue("src", ""))
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToList();
            }

            return video;
        }

        public override async Task<JavPerson> GetPersonAsync(string url)
        {
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return null;

            var person = new JavPerson
            {
                Url = url
            };

            // 提取演员名称
            var nameNode = doc.DocumentNode.SelectSingleNode("//h2[@class='actor-name']");
            person.Name = CleanHtmlText(nameNode?.InnerText);

            // 提取头像
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='actor-image']//img");
            person.ImageUrl = imageNode?.GetAttributeValue("src", "");

            // 提取个人信息
            var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='actor-info']//dl");
            if (infoNodes != null)
            {
                foreach (var node in infoNodes)
                {
                    var label = CleanHtmlText(node.SelectSingleNode("dt")?.InnerText);
                    var value = CleanHtmlText(node.SelectSingleNode("dd")?.InnerText);

                    switch (label)
                    {
                        case "生年月日":
                            person.Birthday = value;
                            break;
                        case "身長":
                            if (int.TryParse(value?.Replace("cm", ""), out int height))
                                person.Height = height;
                            break;
                        case "サイズ":
                            var sizes = value?.Split('/');
                            if (sizes?.Length >= 3)
                            {
                                if (int.TryParse(sizes[0], out int bust))
                                    person.Bust = bust;
                                if (int.TryParse(sizes[1], out int waist))
                                    person.Waist = waist;
                                if (int.TryParse(sizes[2], out int hip))
                                    person.Hip = hip;
                            }
                            break;
                        case "出身地":
                            person.Birthplace = value;
                            break;
                    }
                }
            }

            return person;
        }

        private string ExtractVideoNumber(string url)
        {
            // 从URL中提取番号
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/product/([^/]+)");
            return match.Success ? match.Groups[1].Value.ToUpper() : null;
        }
    }
}