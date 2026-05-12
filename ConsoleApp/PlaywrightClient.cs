using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace ConsoleApp
{

    public class PlaywrightClient : IAsyncDisposable
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        public IPage Page { get; private set; }

        private const string StateFile = "state.json";

        public async Task InitAsync(bool headless = false)
        {
            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new()
            {
                Headless = headless,
                Args = new[]
                {
                "--disable-blink-features=AutomationControlled"
            }
            });

            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                StorageStatePath = File.Exists(StateFile) ? StateFile : null,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
            });

            await _context.AddInitScriptAsync(@"() => {
            Object.defineProperty(navigator, 'webdriver', {
                get: () => undefined
            });
        }");

            Page = await _context.NewPageAsync();
        }

        public async Task SaveStateAsync()
        {
            await _context.StorageStateAsync(new()
            {
                Path = StateFile
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _context.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
        }
    }
}
