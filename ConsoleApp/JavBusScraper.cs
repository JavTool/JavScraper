using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Text.RegularExpressions;


namespace ConsoleApp
{
    public class JavBusScraper
    {
        private readonly IPage _page;

        public JavBusScraper(IPage page)
        {
            _page = page;
        }

        // 👉 搜索番号
        public async Task<string> SearchAsync(string number)
        {
            await _page.GotoAsync($"https://www.javbus.com/search/{number}");

            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 第一条结果
            var link = await _page.GetAttributeAsync(".movie-box", "href");

            if (string.IsNullOrEmpty(link))
                return null;

            return link;
        }

        // 👉 解析详情页
        public async Task<VideoInfo> ParseDetailAsync(string url)
        {
            await _page.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var html = await _page.ContentAsync();

            var title = await _page.InnerTextAsync("h3");

            var cover = await _page.GetAttributeAsync(".bigImage img", "src");
            
            var actors = await _page.EvalOnSelectorAllAsync<string[]>(".star-name a", "elements => elements.map(e => e.textContent)");

            var numberMatch = Regex.Match(html, @"[A-Z0-9]{2,}-\d+");
            var number = numberMatch.Success ? numberMatch.Value : "";

            return new VideoInfo
            {
                Number = number,
                Title = title,
                CoverUrl = cover,
                Actors = [.. actors],
                SourceUrl = url
            };
        }
    }
}
