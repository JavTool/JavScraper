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
    /// Arzon 网站刮削器实现
    /// </summary>
    public class ArzonScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "Arzon";

        public ArzonScraperV2(
            ILogger<ArzonScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
            : base("https://www.arzon.jp", logger, clientFactory.CreateClient())
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   (keyword.StartsWith("ARZON", StringComparison.OrdinalIgnoreCase) ||
                    keyword.Contains("-", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var url = $"{_baseUrl}/itemlist.html?search_str={Uri.EscapeDataString(keyword)}";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return new List<JavVideoIndex>();

            var results = new List<JavVideoIndex>();
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='pictlist']/dl");
            
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var linkNode = node.SelectSingleNode(".//a");
                    if (linkNode == null) continue;

                    var detailUrl = BuildUrl(linkNode.GetAttributeValue("href", ""));
                    var imgNode = node.SelectSingleNode(".//img");
                    var titleNode = node.SelectSingleNode(".//p[@class='title']");
                    
                    var video = new JavVideoIndex
                    {
                        Provider = Name,
                        Url = detailUrl,
                        Title = CleanHtmlText(titleNode?.InnerText),
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
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1");
            video.Title = CleanHtmlText(titleNode?.InnerText);

            // 提取封面
            var coverNode = doc.DocumentNode.SelectSingleNode("//div[@id='item_sample']//img");
            video.Cover = coverNode?.GetAttributeValue("src", "");

            // 提取番号
            video.Num = ExtractVideoNumber(url);

            // 提取详细信息
            var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='item_detail']//dl");
            if (infoNodes != null)
            {
                foreach (var node in infoNodes)
                {
                    var label = CleanHtmlText(node.SelectSingleNode("dt")?.InnerText);
                    var value = CleanHtmlText(node.SelectSingleNode("dd")?.InnerText);

                    switch (label)
                    {
                        case "発売日":
                            video.Date = value;
                            break;
                        case "収録時間":
                            video.Runtime = value;
                            break;
                        case "メーカー":
                            video.Studio = value;
                            break;
                        case "レーベル":
                            video.Maker = value;
                            break;
                        case "シリーズ":
                            video.Set = value;
                            break;
                        case "監督":
                            video.Director = value;
                            break;
                    }
                }
            }

            // 提取类别
            var genreNodes = doc.DocumentNode.SelectNodes("//div[@class='item_genre']//a");
            if (genreNodes != null)
            {
                video.Genres = genreNodes.Select(n => CleanHtmlText(n.InnerText)).ToList();
            }

            // 提取演员
            var actorNodes = doc.DocumentNode.SelectNodes("//div[@class='item_actor']//a");
            if (actorNodes != null)
            {
                video.Actors = actorNodes.Select(n => new JavPerson
                {
                    Name = CleanHtmlText(n.InnerText),
                    Url = BuildUrl(n.GetAttributeValue("href", ""))
                }).ToList();
            }

            // 提取剧情介绍
            var plotNode = doc.DocumentNode.SelectSingleNode("//div[@class='item_text']");
            video.Plot = CleanHtmlText(plotNode?.InnerText);

            // 提取样品图片
            var sampleNodes = doc.DocumentNode.SelectNodes("//div[@class='sample_image']//img");
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
            var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='actor_name']/h1");
            person.Name = CleanHtmlText(nameNode?.InnerText);

            // 提取头像
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='actor_image']//img");
            person.ImageUrl = imageNode?.GetAttributeValue("src", "");

            // 提取个人信息
            var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='actor_info']//dl");
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
            var match = System.Text.RegularExpressions.Regex.Match(url, @"id=([^/&]+)");
            return match.Success ? match.Groups[1].Value.ToUpper() : null;
        }
    }
}