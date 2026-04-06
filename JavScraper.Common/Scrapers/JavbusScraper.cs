using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JavScraper.Common.Models;
using System.Net;

namespace JavScraper.Common.Scrapers
{
    public class JavbusScraper : BaseScraper
    {
        private readonly HttpClient http;
        private readonly HttpClientHandler _handler;
        private static readonly Regex CoverRegex = new Regex(@"(?i)/cover/([a-z\d]+)(?:_b)?\.(jpg|png)", RegexOptions.Compiled);
        private static readonly Regex ThumbRegex = new Regex(@"(?i)/thumbs?/([a-z\d]+)(?:_b)?\.(jpg|png)", RegexOptions.Compiled);

        public JavbusScraper(HttpClient httpClient) : base("javbus", "https://www.javbus.com")
        {
            // 注意：为了正确管理 Cookie，我们需要访问底层的 HttpClientHandler
            // 如果传入的 HttpClient 没有使用可配置的 Handler，我们需要创建一个新的
            
            // 尝试从现有 client 获取 handler，或者创建新的
            _handler = CreateHandlerWithCookies();
            
            // 创建带有 Cookie 容器的新 HttpClient
            http = new HttpClient(_handler, disposeHandler: false);
            
            // 配置完整的浏览器请求头（模仿 Chrome 146）
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", 
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", 
                "zh-CN,zh;q=0.9,zh-TW;q=0.8");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", 
                "gzip, deflate, br, zstd");
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", DefaultBaseUrl);
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", 
                "\"Chromium\";v=\"146\", \"Not-A.Brand\";v=\"24\", \"Google Chrome\";v=\"146\"");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "document");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "navigate");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "same-origin");
            http.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-user", "?1");
            http.DefaultRequestHeaders.TryAddWithoutValidation("upgrade-insecure-requests", "1");
            http.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "max-age=0");
            
            // 设置 Cookie - 使用 CookieContainer 而不是直接设置 Header
            SetupCookies();
        }

        /// <summary>
        /// 创建带有 Cookie 容器的 Handler
        /// </summary>
        private HttpClientHandler CreateHandlerWithCookies()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer(),
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            return handler;
        }

        /// <summary>
        /// 设置必要的 Cookie 以绕过年龄验证
        /// </summary>
        private void SetupCookies()
        {
            try
            {
                var uri = new Uri(DefaultBaseUrl);
                
                // 清除旧的 Cookie
                var oldCookies = _handler.CookieContainer.GetCookies(uri);
                foreach (System.Net.Cookie cookie in oldCookies)
                {
                    cookie.Expired = true;
                }
                
                // 添加 PHPSESSID（会话 ID）- 使用随机生成的会话 ID
                var sessionId = GenerateSessionId();
                _handler.CookieContainer.Add(new System.Net.Cookie("PHPSESSID", sessionId)
                {
                    Domain = uri.Host,
                    Path = "/",
                    Expires = DateTime.Now.AddHours(2)
                });
                
                // 添加 existmag=mag Cookie（显示杂志内容）
                _handler.CookieContainer.Add(new System.Net.Cookie("existmag", "mag")
                {
                    Domain = uri.Host,
                    Path = "/",
                    Expires = DateTime.Now.AddYears(1)
                });
                
                System.Diagnostics.Debug.WriteLine("✅ Cookie 设置完成");
                System.Diagnostics.Debug.WriteLine($"   - PHPSESSID={sessionId}");
                System.Diagnostics.Debug.WriteLine($"   - existmag=mag");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 设置 Cookie 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成随机会话 ID
        /// </summary>
        private string GenerateSessionId()
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var result = new char[26];
            for (int i = 0; i < 26; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        /// <summary>
        /// 访问首页以建立完整的会话和 Cookie
        /// </summary>
        private async Task VisitHomePage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 访问首页建立会话...");
                
                // 访问首页
                var response = await http.GetAsync(DefaultBaseUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("✅ 首页访问成功");
                    
                    // 检查响应中的 Cookie
                    var cookies = _handler.CookieContainer.GetCookies(new Uri(DefaultBaseUrl));
                    System.Diagnostics.Debug.WriteLine($"📋 当前 Cookie 数量: {cookies.Count}");
                    foreach (System.Net.Cookie cookie in cookies)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {cookie.Name}={cookie.Value}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 首页访问失败: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 访问首页出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 模拟提交年龄验证表单
        /// </summary>
        private async Task SubmitAgeVerification()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 提交年龄验证表单...");
                
                // 构建表单数据（模拟表单提交）
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Submit", "確認"),
                    // 注意：实际的表单可能还需要其他字段，需要根据实际情况调整
                });
                
                // 设置正确的 Content-Type
                formData.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                
                // 发送 POST 请求到首页（表单 action 为空，表示提交到当前页面）
                var response = await http.PostAsync(DefaultBaseUrl, formData);
                
                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine("✅ 年龄验证表单提交成功");
                    
                    // 检查响应中的新 Cookie
                    var cookies = _handler.CookieContainer.GetCookies(new Uri(DefaultBaseUrl));
                    System.Diagnostics.Debug.WriteLine($"📋 提交后的 Cookie 数量: {cookies.Count}");
                    foreach (System.Net.Cookie cookie in cookies)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - {cookie.Name}={cookie.Value} (Expires: {cookie.Expires})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ 表单提交失败: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 提交年龄验证表单出错: {ex.Message}");
            }
        }

        public override async Task<JavVideo> ScrapeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // 使用与 Go 版本一致的 URL 格式（带 /ja/ 语言前缀）
            var url = $"https://www.javbus.com/ja/{code.ToUpper()}";
            
            try
            {
                var html = await http.GetStringAsync(url);
                
                // 调试：检查是否遇到年龄验证页面
                if (html.Contains("你是否已經成年") || html.Contains("form1") || html.Contains("age_check"))
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ 检测到年龄验证页面！");
                    System.Diagnostics.Debug.WriteLine($"HTML 长度: {html.Length}");
                    
                    // 提取年龄验证表单的关键信息
                    var hasCheckbox = html.Contains("我已經成年");
                    var hasSubmitButton = html.Contains("id=\"submit\"");
                    System.Diagnostics.Debug.WriteLine($"   - 包含复选框: {hasCheckbox}");
                    System.Diagnostics.Debug.WriteLine($"   - 包含提交按钮: {hasSubmitButton}");
                    
                    // 策略1：尝试提交年龄验证表单
                    await SubmitAgeVerification();
                    
                    // 重试请求
                    html = await http.GetStringAsync(url);
                    
                    if (html.Contains("你是否已經成年") || html.Contains("form1"))
                    {
                        System.Diagnostics.Debug.WriteLine("❌ 表单提交后仍然是年龄验证页面");
                        
                        // 策略2：尝试先访问首页建立会话
                        System.Diagnostics.Debug.WriteLine("🔄 尝试策略2：访问首页...");
                        await VisitHomePage();
                        
                        // 再次重试
                        html = await http.GetStringAsync(url);
                        
                        if (html.Contains("你是否已經成年") || html.Contains("form1"))
                        {
                            System.Diagnostics.Debug.WriteLine("❌ 所有策略都失败了");
                            System.Diagnostics.Debug.WriteLine("💡 提示：可能需要在浏览器中手动访问一次 https://www.javbus.com 并完成年龄验证");
                            return null;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("✅ 策略2成功！");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ 表单提交成功，已绕过年龄验证");
                    }
                }
                
                if (string.IsNullOrWhiteSpace(html))
                    return null;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var video = new JavVideo
                {
                    Url = url,
                    Number = code.ToUpper()
                };

                // Image + Title - 从 bigImage 提取封面和标题
                var coverNode = doc.DocumentNode.SelectSingleNode("//a[@class='bigImage']/img");
                if (coverNode != null)
                {
                    video.Title = coverNode.GetAttributeValue("title", null)?.Trim();
                    
                    // 如果 title 属性为空，尝试从页面标题或其他位置获取
                    if (string.IsNullOrEmpty(video.Title))
                    {
                        video.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(video.Title))
                        {
                            // 清理标题，移除网站名称等后缀
                            var separatorIndex = video.Title.IndexOf(" - ");
                            if (separatorIndex > 0)
                                video.Title = video.Title.Substring(0, separatorIndex).Trim();
                        }
                    }
                    
                    var coverSrc = coverNode.GetAttributeValue("src", null);
                    if (!string.IsNullOrEmpty(coverSrc))
                    {
                        video.Cover = MakeAbsoluteUrl(url, coverSrc);
                        
                        // 生成缩略图 URL（与 Go 版本逻辑一致）
                        // 注意：JavVideo 模型没有 Thumb 字段，所以这里不设置
                        // var thumb = GenerateThumbUrl(video.Cover);
                    }
                }
                
                // 如果仍然没有标题，尝试其他方法
                if (string.IsNullOrEmpty(video.Title))
                {
                    video.Title = doc.DocumentNode.SelectSingleNode("//h3")?.InnerText?.Trim()
                               ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)?.Trim();
                }

                // 描述信息
                video.Plot = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
                            ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", null);

                // Fields - 解析详细信息字段
                var infoNodes = doc.DocumentNode.SelectNodes("//div[@class='col-md-3 info']/p");
                if (infoNodes != null)
                {
                    foreach (var node in infoNodes)
                    {
                        var label = node.SelectSingleNode(".//span")?.InnerText?.Trim();
                        if (string.IsNullOrEmpty(label))
                            continue;

                        switch (label)
                        {
                            case "品番:":
                                // Number 已经从参数设置，这里可以再次确认
                                break;
                            case "発売日:":
                                var releaseDateText = ExtractLastField(node.InnerText);
                                if (!string.IsNullOrEmpty(releaseDateText))
                                    video.Date = ParseDateToString(releaseDateText);
                                break;
                            case "収録時間:":
                                var runtimeText = ExtractLastField(node.InnerText);
                                if (!string.IsNullOrEmpty(runtimeText))
                                    video.Runtime = runtimeText.Replace("分", "").Replace("min", "").Trim();
                                break;
                            case "監督:":
                                var directorNode = node.SelectSingleNode(".//a");
                                if (directorNode != null)
                                    video.Director = directorNode.InnerText?.Trim();
                                break;
                            case "メーカー:":
                                var makerNode = node.SelectSingleNode(".//a");
                                if (makerNode != null)
                                    video.Maker = makerNode.InnerText?.Trim();
                                break;
                            case "レーベル:":
                                var labelNode = node.SelectSingleNode(".//a");
                                if (labelNode != null)
                                    video.Studio = labelNode.InnerText?.Trim(); // 使用 Studio 字段存储 Label
                                break;
                            case "シリーズ:":
                                var seriesNode = node.SelectSingleNode(".//a");
                                if (seriesNode != null)
                                    video.Set = seriesNode.InnerText?.Trim(); // 使用 Set 字段存储 Series
                                break;
                        }
                    }
                }

                // Genres - 提取类型标签
                var genreNodes = doc.DocumentNode.SelectNodes("//span[@class='genre']");
                if (genreNodes != null)
                {
                    var genres = new List<string>();
                    foreach (var genreNode in genreNodes)
                    {
                        var tagNode = genreNode.SelectSingleNode(".//label/a");
                        if (tagNode != null)
                        {
                            var tag = tagNode.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(tag))
                                genres.Add(tag);
                        }
                    }
                    if (genres.Count > 0)
                        video.Genres = genres;
                }

                // Previews/Samples - 提取预览图片
                var sampleNodes = doc.DocumentNode.SelectNodes("//*[@id='sample-waterfall']/a");
                if (sampleNodes != null)
                {
                    var samples = new List<string>();
                    foreach (var sampleNode in sampleNodes)
                    {
                        var href = sampleNode.GetAttributeValue("href", null);
                        if (!string.IsNullOrEmpty(href))
                        {
                            samples.Add(MakeAbsoluteUrl(url, href));
                        }
                    }
                    if (samples.Count > 0)
                        video.Samples = samples;
                }

                // Actors - 提取演员
                var actorNodes = doc.DocumentNode.SelectNodes("//div[@class='star-name']");
                if (actorNodes != null)
                {
                    var actors = new List<string>();
                    foreach (var actorNode in actorNodes)
                    {
                        var actorLink = actorNode.SelectSingleNode(".//a");
                        if (actorLink != null)
                        {
                            var actorName = actorLink.GetAttributeValue("title", null)?.Trim();
                            if (!string.IsNullOrEmpty(actorName))
                                actors.Add(actorName);
                        }
                    }
                    if (actors.Count > 0)
                        video.Actors = actors;
                }

                return video;
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，返回 null
                System.Diagnostics.Debug.WriteLine($"JavBus scrape error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 生成缩略图 URL（与 Go 版本逻辑一致）
        /// </summary>
        private string GenerateThumbUrl(string coverUrl)
        {
            if (string.IsNullOrEmpty(coverUrl))
                return null;

            // 尝试从 cover URL 生成 thumb URL
            if (CoverRegex.IsMatch(coverUrl))
            {
                var thumb = CoverRegex.Replace(coverUrl, "/thumb/${1}.${2}");
                var thumbs = CoverRegex.Replace(coverUrl, "/thumbs/${1}.${2}");
                
                // 优先返回 thumbs 格式（复数形式）
                return thumbs;
            }

            return null;
        }

        /// <summary>
        /// 提取字段的最后一个部分（用于日期和时间）
        /// </summary>
        private string ExtractLastField(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var fields = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return fields.Length > 0 ? fields[fields.Length - 1] : null;
        }

        /// <summary>
        /// 解析日期字符串并格式化为 yyyy-MM-dd
        /// </summary>
        private string ParseDateToString(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date))
                return date.ToString("yyyy-MM-dd");
            
            if (DateTime.TryParse(dateStr, out date))
                return date.ToString("yyyy-MM-dd");

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
    }
}
