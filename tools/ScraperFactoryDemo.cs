using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JavScraper.Scrapers;

namespace JavScraper.Tools
{
    /// <summary>
    /// ScraperFactory 使用示例程序
    /// </summary>
    public class ScraperFactoryDemo
    {
        public static async Task RunDemoAsync()
        {
            Console.WriteLine("=== ScraperFactory 使用示例 ===\n");

            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // 获取 ScraperFactory
            var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();

            // 1. 查看所有已注册的刮削器
            Console.WriteLine("1. 查看所有已注册的刮削器:");
            Console.WriteLine(new string('-', 50));
            foreach (var name in scraperFactory.GetAvailableScrapers())
            {
                Console.WriteLine($"  - {name}");
            }
            Console.WriteLine($"总计：{scraperFactory.Count} 个刮削器\n");

            // 2. 检查特定刮削器是否可用
            Console.WriteLine("2. 检查刮削器是否已注册:");
            Console.WriteLine(new string('-', 50));
            CheckScraperAvailability(scraperFactory, "JavBus");
            CheckScraperAvailability(scraperFactory, "JavDB");
            CheckScraperAvailability(scraperFactory, "NonExistent");
            Console.WriteLine();

            // 3. 创建并使用刮削器
            Console.WriteLine("3. 创建 JavBus 刮削器并搜索:");
            Console.WriteLine(new string('-', 50));
            try
            {
                var javBus = scraperFactory.CreateScraper("JavBus");
                Console.WriteLine($"✓ 成功创建刮削器：{javBus.Name}");
                
                // 注意：实际使用时才真正调用 API
                // var results = await javBus.SearchAsync("ABW-001");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 创建失败：{ex.Message}");
            }
            Console.WriteLine();

            // 4. 使用 Try 模式创建刮削器
            Console.WriteLine("4. 使用 TryCreateScraper 方法:");
            Console.WriteLine(new string('-', 50));
            if (scraperFactory.TryCreateScraper("Dmm", out var dmm))
            {
                Console.WriteLine($"✓ 成功创建 DMM 刮削器：{dmm.Name}");
            }
            else
            {
                Console.WriteLine("✗ DMM 刮削器不可用");
            }
            Console.WriteLine();

            // 5. 查找可以处理特定关键字的刮削器
            Console.WriteLine("5. 查找可以处理 'HEYZO-1234' 的刮削器:");
            Console.WriteLine(new string('-', 50));
            var keyword = "HEYZO-1234";
            var suitableScrapers = scraperFactory.FindScrapers(keyword).ToList();
            
            if (suitableScrapers.Any())
            {
                Console.WriteLine($"找到 {suitableScrapers.Count} 个匹配的刮削器:");
                foreach (var scraper in suitableScrapers)
                {
                    Console.WriteLine($"  ✓ {scraper.Name}");
                }
            }
            else
            {
                Console.WriteLine("未找到可以处理该关键字的刮削器");
            }
            Console.WriteLine();

            // 6. 遍历所有工厂
            Console.WriteLine("6. 使用 ForEachFactory 遍历所有工厂:");
            Console.WriteLine(new string('-', 50));
            scraperFactory.ForEachFactory((name, factoryFunc) =>
            {
                Console.WriteLine($"  工厂：{name}");
                // 这里可以选择性地创建实例
                // var scraper = factoryFunc();
            });
            Console.WriteLine();

            // 7. 手动注册自定义刮削器
            Console.WriteLine("7. 注册自定义刮削器:");
            Console.WriteLine(new string('-', 50));
            scraperFactory.RegisterFactory("CustomScraper", () =>
            {
                return new CustomDemoScraper();
            });
            
            if (scraperFactory.IsRegistered("CustomScraper"))
            {
                var custom = scraperFactory.CreateScraper("CustomScraper");
                Console.WriteLine($"✓ 成功注册并创建自定义刮削器：{custom.Name}");
            }
            Console.WriteLine();

            // 8. 多次创建验证每次都是新实例
            Console.WriteLine("8. 验证每次创建返回不同实例:");
            Console.WriteLine(new string('-', 50));
            var scraper1 = scraperFactory.CreateScraper("JavBus");
            var scraper2 = scraperFactory.CreateScraper("JavBus");
            Console.WriteLine($"实例 1 HashCode: {scraper1.GetHashCode()}");
            Console.WriteLine($"实例 2 HashCode: {scraper2.GetHashCode()}");
            Console.WriteLine($"是否为不同实例：{!ReferenceEquals(scraper1, scraper2)}");
            Console.WriteLine();

            Console.WriteLine("=== 示例结束 ===");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 添加日志服务
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // 添加必要的服务
            services.AddSingleton<ScraperHttpClientFactory>();
            services.AddSingleton<ScraperCacheService>();
            services.AddSingleton<SampleImageService>();
            
            // 配置选项
            services.AddSingleton(new ScraperOptions
            {
                SiteSettings = new System.Collections.Generic.Dictionary<string, SiteSetting>
                {
                    ["JavBus"] = new SiteSetting { BaseUrl = "https://www.javbus.com", Enabled = true },
                    ["JavDB"] = new SiteSetting { BaseUrl = "https://javdb.com", Enabled = true },
                    ["Dmm"] = new SiteSetting { BaseUrl = "https://www.dmm.co.jp", Enabled = true },
                    ["Heyzo"] = new SiteSetting { BaseUrl = "https://www.heyzo.com", Enabled = true },
                }
            });

            // 添加 ScraperFactory
            services.AddSingleton<ScraperFactory>();
        }

        private static void CheckScraperAvailability(ScraperFactory factory, string name)
        {
            var isRegistered = factory.IsRegistered(name);
            var status = isRegistered ? "✓ 已注册" : "✗ 未注册";
            Console.WriteLine($"  {name,-20} {status}");
        }
    }

    /// <summary>
    /// 用于演示的自定义刮削器
    /// </summary>
    public class CustomDemoScraper : IScraperV2
    {
        public string Name => "CustomDemoScraper";

        public bool CanHandle(string keyword) => false;

        public Task<JavPerson> GetPersonAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task<JavVideo> GetDetailsAsync(string url)
        {
            throw new NotImplementedException();
        }

        public Task<System.Collections.Generic.List<JavVideoIndex>> SearchAsync(string keyword)
        {
            throw new NotImplementedException();
        }
    }
}
