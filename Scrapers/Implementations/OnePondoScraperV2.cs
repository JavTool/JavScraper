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
    /// 1Pondo 网站刮削器实现
    /// </summary>
    public class OnePondoScraperV2 : ScraperBase
    {
        private readonly ScraperCacheService _cacheService;

        public override string Name => "1Pondo";

        public OnePondoScraperV2(
            ILogger<OnePondoScraperV2> logger,
            ScraperHttpClientFactory clientFactory,
            ScraperCacheService cacheService)
            : base("https://www.1pondo.tv", logger, clientFactory.CreateClient())
        {
            _cacheService = cacheService;
        }

        public override bool CanHandle(string keyword)
        {
            return !string.IsNullOrWhiteSpace(keyword) &&
                   System.Text.RegularExpressions.Regex.IsMatch(keyword, @"\d{6}_\d{3}");
        }

        public override async Task<List<JavVideoIndex>> SearchAsync(string keyword)
        {
            var url = $"{_baseUrl}/movies/{keyword}/";
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return new List<JavVideoIndex>();

            var video = new JavVideoIndex
            {
                Provider = Name,
                Url = url,
                Title = CleanHtmlText(doc.DocumentNode.SelectSingleNode("//h1[@class='h1--dense']")?.InnerText),
                Cover = $"{_baseUrl}/assets/sample/{keyword}/str.jpg",
                Num = keyword.ToUpper()
            };

            return new List<JavVideoIndex> { video };
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

            // 提取番号
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/movies/([^/]+)/");
            if (match.Success)
            {
                video.Num = match.Groups[1].Value.ToUpper();
            }

            // 提取标题
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='h1--dense']");
            video.Title = CleanHtmlText(titleNode?.InnerText);

            // 提取封面
            if (!string.IsNullOrEmpty(video.Num))
            {
                video.Cover = $"{_baseUrl}/assets/sample/{video.Num}/str.jpg";
            }

            // 提取发行日期
            var dateNode = doc.DocumentNode.SelectSingleNode("//li[contains(text(), '配信日')]/span");
            video.Date = CleanHtmlText(dateNode?.InnerText);

            // 提取时长
            var lengthNode = doc.DocumentNode.SelectSingleNode("//li[contains(text(), '再生時間')]/span");
            video.Runtime = CleanHtmlText(lengthNode?.InnerText);

            // 提取演员
            var actorNodes = doc.DocumentNode.SelectNodes("//li[contains(text(), '出演')]/span/a");
            if (actorNodes != null)
            {
                video.Actors = actorNodes.Select(n => new JavPerson
                {
                    Name = CleanHtmlText(n.InnerText),
                    Url = BuildUrl(n.GetAttributeValue("href", ""))
                }).ToList();
            }

            // 提取类别
            var genreNodes = doc.DocumentNode.SelectNodes("//li[contains(text(), 'タグ')]/span/a");
            if (genreNodes != null)
            {
                video.Genres = genreNodes.Select(n => CleanHtmlText(n.InnerText)).ToList();
            }

            // 提取制作商和发行商
            video.Maker = "1Pondo";
            video.Studio = "1Pondo";

            // 提取剧情介绍
            var plotNode = doc.DocumentNode.SelectSingleNode("//div[@class='movie-detail']/p");
            video.Plot = CleanHtmlText(plotNode?.InnerText);

            // 提取样品图片
            if (!string.IsNullOrEmpty(video.Num))
            {
                var samples = new List<string>();
                for (int i = 1; i <= 10; i++)
                {
                    samples.Add($"{_baseUrl}/assets/sample/{video.Num}/popu{i}.jpg");
                }
                video.Samples = samples;
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
            var nameNode = doc.DocumentNode.SelectSingleNode("//div[@class='actress-head']//h1");
            person.Name = CleanHtmlText(nameNode?.InnerText);

            // 提取头像
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='actress-image']//img");
            person.ImageUrl = imageNode?.GetAttributeValue("src", "");

            // 提取个人信息
            var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='actress-info']//dl");
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
    }
}