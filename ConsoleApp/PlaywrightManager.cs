using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;


namespace ConsoleApp
{

    public class PlaywrightManager : IAsyncDisposable
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IBrowserContext _context;
        private IPage _page;

        private const string StateFile = "playwright-state.json";

        public async Task InitAsync(bool headless = false)
        {
            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                Args = new[]
                {
                "--disable-blink-features=AutomationControlled",
                "--no-sandbox",
                "--disable-dev-shm-usage"
            }
            });

            // 👉 关键：复用登录 / Cookie 状态
            if (File.Exists(StateFile))
            {
                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    StorageStatePath = StateFile
                });
            }
            else
            {
                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    Locale = "zh-CN",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36"
                });
            }

            _page = await _context.NewPageAsync();

            // 👉 防止被检测为自动化
            await _context.AddInitScriptAsync(@"() => {
            Object.defineProperty(navigator, 'webdriver', {
                get: () => undefined
            });
        }");
        }

        public IPage Page => _page;

        public async Task SaveStateAsync()
        {
            await _context.StorageStateAsync(new BrowserContextStorageStateOptions
            {
                Path = StateFile
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            if (_playwright != null) _playwright.Dispose();
        }
    }
}
