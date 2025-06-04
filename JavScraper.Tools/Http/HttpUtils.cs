using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Tools.Http
{
    public class HttpUtils
    {
        private static readonly HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// 站点信息封装
        /// </summary>
        private static readonly Dictionary<string, Func<string, string>> SiteUrlBuilders = new()
        {
            { "1Pondo",         number => $"https://www.1pondo.tv/movies/{number}/" },
            { "CaribbeancomPR", number => $"https://www.caribbeancompr.com/moviepages/{number}/index.html" },
            { "Pacopacomama",   number => $"https://www.pacopacomama.com/moviepages/{number}/index.html" },
            { "Caribbeancom",   number => $"https://www.caribbeancom.com/moviepages/{number}/index.html" },
        };

        /// <summary>
        /// 检查番号属于哪个片商（根据是否返回 404）
        /// </summary>
        public static async Task<List<string>> CheckMakerAsync(string number)
        {
            var tasks = new List<Task<(string site, bool exists)>>();

            foreach (var kvp in SiteUrlBuilders)
            {
                string site = kvp.Key;
                string url = kvp.Value(number);

                tasks.Add(CheckUrlExistsAsync(site, url));
            }

            var results = await Task.WhenAll(tasks);
            var availableSites = new List<string>();

            foreach (var (site, exists) in results)
            {
                if (exists)
                    availableSites.Add(site);
            }

            return availableSites;
        }

        /// <summary>
        /// 使用 HEAD 请求验证链接是否有效
        /// </summary>
        private static async Task<(string site, bool exists)> CheckUrlExistsAsync(string site, string url)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await client.SendAsync(request);

                return (site, response.StatusCode != System.Net.HttpStatusCode.NotFound);
            }
            catch
            {
                return (site, false); // 网络异常视为无效
            }
        }

        /// <summary>
        /// 验证一个 HTTP 地址是否可用，是否返回状态码 200
        /// </summary>
        /// <param name="url">要检测的 URL</param>
        /// <returns>是否返回 200 状态码</returns>
        public static async Task<bool> IsUrlAvailableAsync(string url)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置超时

                using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                // 请求失败（无效链接、连接超时等）视为不可用
                return false;
            }
        }
    }
}
