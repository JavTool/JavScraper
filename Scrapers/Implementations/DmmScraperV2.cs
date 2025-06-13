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
    /// DMM 网站刮削器实现
    /// </summary>
    public class DmmScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "DMM";

        public DmmScraperV2(
            ILogger<DmmScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
            : base("https://www.dmm.co.jp", logger, clientFactory.CreateClient())
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   (keyword.StartsWith("DMM", StringComparison.OrdinalIgnoreCase) ||
                    keyword.Contains("-", StringComparison.OrdinalIgnoreCase));
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var url = $"{_baseUrl}/search/=/searchstr={Uri.EscapeDataString(keyword)}";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return new List<JavVideoIndex>();

            var results = new List<JavVideoIndex>();
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='tmb']");
            
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var linkNode = node.SelectSingleNode(".//a");
                    if (linkNode == null) continue;

                    var detailUrl = BuildUrl(linkNode.GetAttributeValue("href", ""));
                    var imgNode = node.SelectSingleNode(".//img");
                    
                    var video = new JavVideoIndex
                    {
                        Provider = Name,
                        Url = detailUrl,
                        Title = imgNode?.GetAttributeValue("alt", ""),
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
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@id='title']");
            video.Title = CleanHtmlText(titleNode?.InnerText);

            // 提取封面
            var coverNode = doc.DocumentNode.SelectSingleNode("//div[@id='sample-video']//img");
            video.Cover = coverNode?.GetAttributeValue("src", "");

            // 提取番号
            video.Num = ExtractVideoNumber(url);

            // 提取发行日期
            var dateNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '発売日')]/following-sibling::td");
            video.Date = CleanHtmlText(dateNode?.InnerText);

            // 提取时长
            var runtimeNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '収録時間')]/following-sibling::td");
            video.Runtime = CleanHtmlText(runtimeNode?.InnerText);

            // 提取制作商
            var studioNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'メーカー')]/following-sibling::td/a");
            video.Studio = CleanHtmlText(studioNode?.InnerText);

            // 提取发行商
            var makerNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'レーベル')]/following-sibling::td/a");
            video.Maker = CleanHtmlText(makerNode?.InnerText);

            // 提取系列
            var seriesNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), 'シリーズ')]/following-sibling::td/a");
            video.Set = CleanHtmlText(seriesNode?.InnerText);

            // 提取导演
            var directorNode = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '監督')]/following-sibling::td/a");
            video.Director = CleanHtmlText(directorNode?.InnerText);

            // 提取类别
            var genreNodes = doc.DocumentNode.SelectNodes("//td[contains(text(), 'ジャンル')]/following-sibling::td//a");
            if (genreNodes != null)
            {
                video.Genres = genreNodes.Select(n => CleanHtmlText(n.InnerText)).ToList();
            }

            // 提取演员
            var actorNodes = doc.DocumentNode.SelectNodes("//td[contains(text(), '出演者')]/following-sibling::td//a");
            if (actorNodes != null)
            {
                video.Actors = actorNodes.Select(n => new JavPerson
                {
                    Name = CleanHtmlText(n.InnerText),
                    Url = BuildUrl(n.GetAttributeValue("href", ""))
                }).ToList();
            }

            // 提取剧情介绍
            var plotNode = doc.DocumentNode.SelectSingleNode("//div[@class='mg-b20 lh4']");
            video.Plot = CleanHtmlText(plotNode?.InnerText);

            // 提取样品图片
            var sampleNodes = doc.DocumentNode.SelectNodes("//div[@id='sample-image-block']//img");
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
            var nameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='ttl-actor']");
            person.Name = CleanHtmlText(nameNode?.InnerText);

            // 提取头像
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='actor-image']//img");
            person.ImageUrl = imageNode?.GetAttributeValue("src", "");

            // 提取个人信息
            var infoNodes = doc.DocumentNode.SelectNodes("//table[@class='actor-info']//tr");
            if (infoNodes != null)
            {
                foreach (var node in infoNodes)
                {
                    var label = CleanHtmlText(node.SelectSingleNode("th")?.InnerText);
                    var value = CleanHtmlText(node.SelectSingleNode("td")?.InnerText);

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
            var match = System.Text.RegularExpressions.Regex.Match(url, @"cid=([^/&]+)");
            return match.Success ? match.Groups[1].Value.ToUpper() : null;
        }
    }
}