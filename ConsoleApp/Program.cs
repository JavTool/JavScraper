// See https://aka.ms/new-console-template for more information

using ConsoleApp;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var pw = new PlaywrightClient();
        await pw.InitAsync(headless: false);

        var scraper = new JavBusScraper(pw.Page);

        string number = "DVMM-362";

        var url = await scraper.SearchAsync(number);

        if (url != null)
        {
            var info = await scraper.ParseDetailAsync(url);
            Console.WriteLine($"演员: {string.Join(", ", info.Actors)}");
            Console.WriteLine($"番号: {info.Number}");
            Console.WriteLine($"标题: {info.Title}");
            Console.WriteLine($"封面: {info.CoverUrl}");
        }

        await pw.SaveStateAsync();
        await pw.DisposeAsync();
    }
}