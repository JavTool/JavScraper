using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    public class JavdbScraper : BaseScraper
    {
        private readonly HttpClient http;

        public JavdbScraper(HttpClient httpClient) : base("javdb", "https://javdb.com")
        {
            http = httpClient;
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // 策略1：先尝试直接访问详情页
            var video = await TryParseDirectUrl(code);
            if (video != null)
                return video;

            // 策略2：如果直接访问失败，使用搜索方式
            video = await SearchAndParse(code);
            return video;
        }

        /// <summary>
        /// 尝试直接通过 URL 解析
        /// </summary>
        private async Task<JavVideo> TryParseDirectUrl(string code)
        {
            try
            {
                // JavDB 的 URL 格式可能是 /v/{id} 或 /videos/{id}
                var url = $"https://javdb.com/v/{code}";
                var html = await http.GetStringAsync(url);
                
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                // 检查是否是有效的详情页（不是搜索页或404）
                if (html.Contains("影片详情") || html.Contains("movie-panel"))
                {
                    return ParseVideoPage(html, url);
                }
            }
            catch
            {
                // 忽略错误，尝试其他方式
            }

            return null;
        }

        /// <summary>
        /// 通过搜索获取视频信息（参考 JavBus 的实现）
        /// </summary>
        private async Task<JavVideo> SearchAndParse(string code)
        {
            try
            {
                // 搜索 URL
                var searchUrl = $"https://javdb.com/search?q={Uri.EscapeDataString(code)}&f=all";
                var html = await http.GetStringAsync(searchUrl);
                
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 从搜索结果中获取第一个视频链接
                // JavDB 搜索结果通常是 div.movie-list 或 .grid-item
                var videoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'movie-list')]//a | //div[contains(@class,'grid-item')]//a");
                
                if (videoNode == null)
                    return null;

                var videoUrl = videoNode.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(videoUrl))
                    return null;

                // 构建完整 URL
                if (!videoUrl.StartsWith("http"))
                {
                    videoUrl = $"https://javdb.com{videoUrl}";
                }

                // 获取详情页 HTML
                var detailHtml = await http.GetStringAsync(videoUrl);
                if (string.IsNullOrWhiteSpace(detailHtml))
                    return null;

                return ParseVideoPage(detailHtml, videoUrl);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析视频详情页
        /// </summary>
        private JavVideo ParseVideoPage(string html, string url)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 标题 - 优先从 h1 或 title 标签获取
            var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim()
                        ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

            // 移除标题中的番号前缀
            if (!string.IsNullOrWhiteSpace(title))
            {
                var code = ExtractCodeFromUrl(url);
                if (!string.IsNullOrEmpty(code) && title.StartsWith(code, StringComparison.OrdinalIgnoreCase))
                {
                    title = title.Substring(code.Length).Trim();
                }
            }

            // 描述/简介
            var desc = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'description') or contains(@class,'plot')]")?.InnerText?.Trim()
                       ?? doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null);

            // 封面/海报
            var poster = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null)
                         ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'video-cover') or contains(@class,'cover')]")?.GetAttributeValue("src", null)
                         ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'poster')]")?.GetAttributeValue("src", null);

            // 演员
            var actorNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/actors/') or contains(@href,'/star/') or contains(@class,'actor')]");
            var actors = actorNodes?.Select(n => n.InnerText?.Trim())
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .Distinct()
                                    .ToList() ?? new List<string>();

            // 类型/标签
            var genreNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/genres/') or contains(@href,'/category/') or contains(@class,'genre')]");
            var genres = genreNodes?.Select(n => n.InnerText?.Trim())
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .Distinct()
                                    .ToList() ?? new List<string>();

            // 制作商/发行商
            var makerNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href,'/studios/') or contains(@href,'/maker/') or contains(@href,'/publisher/')]");
            var maker = makerNode?.InnerText?.Trim();

            // 导演
            var directorNode = doc.DocumentNode.SelectSingleNode("//span[text()='導演']/following-sibling::a | //span[text()='Director']/following-sibling::a");
            var director = directorNode?.InnerText?.Trim();

            // 日期
            var dateNode = doc.DocumentNode.SelectSingleNode("//span[text()='日期']/following-sibling::span | //span[text()='Date']/following-sibling::span | //time");
            var date = dateNode?.InnerText?.Trim();

            // 时长
            var runtimeNode = doc.DocumentNode.SelectSingleNode("//span[text()='時長']/following-sibling::span | //span[text()='Duration']/following-sibling::span");
            var runtime = runtimeNode?.InnerText?.Trim();

            // 预览图/样品图
            var sampleNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'sample-images')]//img | //div[contains(@class,'gallery')]//img | //div[contains(@class,'preview')]//img");
            var samples = sampleNodes?.Select(n => n.GetAttributeValue("src", null) ?? n.GetAttributeValue("data-src", null) ?? n.GetAttributeValue("data-original", null))
                                     .Where(u => !string.IsNullOrEmpty(u))
                                     .Distinct()
                                     .ToList() ?? new List<string>();

            // 番号 - 从 URL 或页面中提取
            var number = ExtractCodeFromUrl(url) 
                         ?? doc.DocumentNode.SelectSingleNode("//span[text()='番號']/following-sibling::span | //strong[text()='ID:']/following-sibling::span")?.InnerText?.Trim();

            var video = new JavVideo
            {
                Url = url,
                Number = number ?? ExtractCodeFromUrl(url),
                Title = title,
                Plot = desc,
                Cover = poster,
                Director = director,
                Date = date,
                Runtime = runtime,
                Maker = maker,
                Actors = actors,
                Genres = genres,
                Samples = samples
            };

            return video;
        }

        /// <summary>
        /// 从 URL 中提取番号
        /// </summary>
        private string ExtractCodeFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // 匹配 /v/{code} 或 /videos/{code} 格式
            var match = System.Text.RegularExpressions.Regex.Match(url, @"/(?:v|videos)/([^/?#]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count >= 2)
            {
                return match.Groups[1].Value.ToUpper();
            }

            return null;
        }
    }
}
