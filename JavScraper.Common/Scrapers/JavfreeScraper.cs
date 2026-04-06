using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;
using Microsoft.Extensions.Logging;

namespace JavScraper.Common.Scrapers
{
    /// <summary>
    /// JAVFREE 刮削器
    /// 参考: https://github.com/metatube-community/metatube-sdk-go/tree/main/provider/javfree
    /// </summary>
    public class JavfreeScraper : BaseScraper
    {
        private readonly HttpClient http;

        // URL 模板
        private const string MovieUrlTemplate = "https://javfree.me/{0}/fc2-ppv-{1}";
        private const string SearchUrlTemplate = "https://javfree.me/?s={0}";

        public JavfreeScraper(HttpClient httpClient) 
            : base("javfree", "https://javfree.me/")
        {
            http = httpClient;
            
            // 配置请求头
            http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", DefaultBaseUrl);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // 解析 FC2 番号格式 (例如: FC2-123456 或 123456)
            var fc2Id = ParseFc2Number(code);
            if (string.IsNullOrEmpty(fc2Id))
                return null;

            // 构建 URL: https://javfree.me/{provider_id}/fc2-ppv-{fc2_id}
            // 注意：Go 版本需要 provider_id，但我们先尝试直接使用 fc2_id
            var url = $"https://javfree.me/fc2-ppv-{fc2Id}";
            
            try
            {
                var html = await http.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var video = new JavVideo
                {
                    Number = $"FC2-{fc2Id}"
                };

                // Title - 从 h1 标签提取，移除 FC2-PPV 前缀
                var titleNode = doc.DocumentNode.SelectSingleNode("//header[@class='entry-header']/h1");
                if (titleNode != null)
                {
                    var titleText = titleNode.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(titleText))
                    {
                        // 移除 FC2-PPV 前缀
                        video.Title = Regex.Replace(titleText, @"(?i)(?:FC2(?:[-_]?PPV)?[-_]?)\d+", "").Trim();
                    }
                }

                // Director & Date - 从 span.post-author 提取
                var authorNode = doc.DocumentNode.SelectSingleNode("//span[@class='post-author']/strong");
                if (authorNode != null)
                {
                    video.Director = authorNode.InnerText?.Trim();
                    
                    // 获取下一个兄弟节点作为发布日期
                    var nextNode = authorNode.NextSibling;
                    if (nextNode != null && !string.IsNullOrEmpty(nextNode.InnerText))
                    {
                        var dateText = nextNode.InnerText.Trim();
                        if (DateTime.TryParse(dateText, out var releaseDate))
                        {
                            video.Date = releaseDate.ToString("yyyy-MM-dd");
                        }
                    }
                }

                // Samples - 从 entry-content 中的图片提取
                var sampleNodes = doc.DocumentNode.SelectNodes("//div[@class='entry-content']/p/img");
                if (sampleNodes != null)
                {
                    foreach (var imgNode in sampleNodes)
                    {
                        var src = imgNode.GetAttributeValue("src", null);
                        if (!string.IsNullOrEmpty(src))
                        {
                            video.Samples.Add(MakeAbsoluteUrl(url, src));
                        }
                    }
                }

                // Cover - 使用第一张预览图作为封面
                if (video.Samples.Count > 0)
                {
                    video.Cover = video.Samples[0];
                    // 移除第一张，剩余的作为预览图
                    video.Samples.RemoveAt(0);
                }

                return video;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JavFree 抓取失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 搜索电影
        /// </summary>
        public async Task<List<JavVideo>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<JavVideo>();

            // 提取 FC2 ID
            var fc2Id = ParseFc2Number(keyword);
            if (string.IsNullOrEmpty(fc2Id))
                return new List<JavVideo>();

            var results = new List<JavVideo>();
            var searchUrl = string.Format(SearchUrlTemplate, Uri.EscapeDataString(fc2Id));

            try
            {
                var html = await http.GetStringAsync(searchUrl);
                if (string.IsNullOrWhiteSpace(html))
                    return results;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 搜索文章列表
                var articles = doc.DocumentNode.SelectNodes("//article[@class='hentry clear']");
                if (articles == null)
                    return results;

                foreach (var article in articles)
                {
                    try
                    {
                        var video = ParseSearchResult(article, fc2Id, searchUrl);
                        if (video != null)
                        {
                            results.Add(video);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析搜索结果项失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JavFree 搜索失败: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// 解析单个搜索结果
        /// </summary>
        private JavVideo ParseSearchResult(HtmlNode article, string fc2Id, string baseUrl)
        {
            // 获取链接
            var linkNode = article.SelectSingleNode(".//h2/a");
            if (linkNode == null)
                return null;

            var homepage = MakeAbsoluteUrl(baseUrl, linkNode.GetAttributeValue("href", null));
            var title = linkNode.InnerText?.Trim();

            // 获取缩略图
            var thumbNode = article.SelectSingleNode(".//a/div/img");
            if (thumbNode == null)
                return null;

            var thumbSrc = thumbNode.GetAttributeValue("src", null);
            if (string.IsNullOrEmpty(thumbSrc))
                return null;

            var thumb = MakeAbsoluteUrl(baseUrl, thumbSrc);
            
            // 生成封面 URL (使用 CDN)
            var fileName = thumb.Substring(thumb.LastIndexOf('/') + 1);
            var cover = $"https://cf.javfree.me/HLIC/{fileName}";

            return new JavVideo
            {
                Number = $"FC2-{fc2Id}",
                Title = title,
                Cover = cover
            };
        }

        /// <summary>
        /// 从 URL 中解析电影 ID
        /// </summary>
        private string ParseMovieIdFromUrl(string url)
        {
            // 匹配模式: /{provider_id}/fc2-ppv-{fc2_id}
            var match = Regex.Match(url, @"/(\d+)/fc2-ppv-(\d+)");
            if (match.Success && match.Groups.Count >= 3)
            {
                return $"{match.Groups[1].Value}-{match.Groups[2].Value}";
            }
            return null;
        }

        /// <summary>
        /// 解析 FC2 番号
        /// 支持格式: FC2-123456, FC2PPV-123456, 123456
        /// </summary>
        private string ParseFc2Number(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return null;

            // 移除所有空格和特殊字符
            keyword = keyword.Trim();

            // 匹配 FC2-XXXXXX、FC2PPV-XXXXXX、FC2_PPV_XXXXXX 等格式
            var match = Regex.Match(keyword, @"FC2(?:[-_]?PPV?)?[-_]?(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count >= 2)
            {
                return match.Groups[1].Value;
            }

            // 如果只是纯数字，直接返回
            if (Regex.IsMatch(keyword, @"^\d+$"))
            {
                return keyword;
            }

            return null;
        }

        /// <summary>
        /// 将相对 URL 转换为绝对 URL
        /// </summary>
        private string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return null;

            if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
                return relativeUrl;

            try
            {
                var baseUri = new Uri(baseUrl);
                var absoluteUri = new Uri(baseUri, relativeUrl);
                return absoluteUri.ToString();
            }
            catch
            {
                return relativeUrl;
            }
        }

        public override bool CanHandle(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return false;

            // 检查是否是 FC2 格式
            var fc2Id = ParseFc2Number(keyword);
            return !string.IsNullOrEmpty(fc2Id);
        }

        public override bool CheckKeyword(string keyword)
        {
            return CanHandle(keyword);
        }
    }
}
