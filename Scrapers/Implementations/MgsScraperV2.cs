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
    /// MGS 网站刮削器实现
    /// </summary>
    public class MgsScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "MGS";

        public MgsScraperV2(
            ILogger<MgsScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
            : base("https://www.mgstage.com", logger, clientFactory.CreateClient())
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   (keyword.StartsWith("MGS", StringComparison.OrdinalIgnoreCase) ||
                    keyword.StartsWith("SIRO", StringComparison.OrdinalIgnoreCase) ||
                    keyword.StartsWith("ARA", StringComparison.OrdinalIgnoreCase) ||
                    keyword.StartsWith("LUXU", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var url = $"{_baseUrl}/search/cSearch.php?search_word={Uri.EscapeDataString(keyword)}";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return new List<JavVideoIndex>();

            var results = new List<JavVideoIndex>();
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='rank_list']/ul/li");
            
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var linkNode = node.SelectSingleNode(".//h5/a");
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
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='tag']");
            video.Title = CleanHtmlText(titleNode?.InnerText);

            // 提取封面
            var coverNode = doc.DocumentNode.SelectSingleNode("//div[@class='detail_photo']//img");
            video.Cover = coverNode?.GetAttributeValue("src", "");

            // 提取番号
            var idNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), '品番')]/td");
            video.Num = CleanHtmlText(idNode?.InnerText);

            // 提取发行日期
            var dateNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), '配信開始日')]/td");
            video.Date = CleanHtmlText(dateNode?.InnerText);

            // 提取时长
            var lengthNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), '収録時間')]/td");
            video.Runtime = CleanHtmlText(lengthNode?.InnerText);

            // 提取导演
            var directorNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), '監督')]/td");
            video.Director = CleanHtmlText(directorNode?.InnerText);

            // 提取制作商
            var makerNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), 'メーカー')]/td");
            video.Maker = CleanHtmlText(makerNode?.InnerText);

            // 提取发行商
            var labelNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), 'レーベル')]/td");
            video.Studio = CleanHtmlText(labelNode?.InnerText);

            // 提取系列
            var seriesNode = doc.DocumentNode.SelectSingleNode("//tr[contains(th/text(), 'シリーズ')]/td");
            video.Set = CleanHtmlText(seriesNode?.InnerText);

            // 提取类别
            var genreNodes = doc.DocumentNode.SelectNodes("//tr[contains(th/text(), 'ジャンル')]/td/a");
            if (genreNodes != null)
            {
                video.Genres = genreNodes.Select(n => CleanHtmlText(n.InnerText)).ToList();
            }

            // 提取演员
            var actorNodes = doc.DocumentNode.SelectNodes("//tr[contains(th/text(), '出演')]/td/a");
            if (actorNodes != null)
            {
                video.Actors = actorNodes.Select(n => new JavPerson
                {
                    Name = CleanHtmlText(n.InnerText),
                    Url = BuildUrl(n.GetAttributeValue("href", ""))
                }).ToList();
            }

            // 提取剧情介绍
            var plotNode = doc.DocumentNode.SelectSingleNode("//p[@class='txt introduction']");
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
            var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='actress_name']/h1");
            person.Name = CleanHtmlText(nameNode?.InnerText);

            // 提取头像
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='actress_image']//img");
            person.ImageUrl = imageNode?.GetAttributeValue("src", "");

            // 提取个人信息
            var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='actress_info']//dl");
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
                        case "趣味":
                            person.Hobbies = value;
                            break;
                    }
                }
            }

            return person;
        }

        private string ExtractVideoNumber(string url)
        {
            // 从URL中提取番号
            var match = System.Text.RegularExpressions.Regex.Match(url, @"product_id=([^/&]+)");
            return match.Success ? match.Groups[1].Value.ToUpper() : null;
        }
    }
}