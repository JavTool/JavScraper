# ScraperFactory 快速开始指南

## 5 分钟上手

### 1. 获取 ScraperFactory 实例

如果你使用依赖注入（推荐）：

```csharp
public class MyService
{
    private readonly ScraperFactory _scraperFactory;
    
    public MyService(ScraperFactory scraperFactory)
    {
        _scraperFactory = scraperFactory;
    }
}
```

或者手动创建：

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<ScraperHttpClientFactory>();
services.AddSingleton<ScraperCacheService>();
services.AddSingleton<SampleImageService>();
services.AddSingleton(new ScraperOptions { /* 配置 */ });
services.AddSingleton<ScraperFactory>();

var serviceProvider = services.BuildServiceProvider();
var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();
```

### 2. 查看所有可用刮削器

```csharp
var scrapers = scraperFactory.GetAvailableScrapers();
foreach (var name in scrapers)
{
    Console.WriteLine($"可用刮削器：{name}");
}
// 输出示例：
// 可用刮削器：JavBus
// 可用刮削器：JavDB
// 可用刮削器：Dmm
// ...
```

### 3. 创建并使用刮削器

```csharp
// 创建 JavBus 刮削器
var javBus = scraperFactory.CreateScraper("JavBus");

// 搜索影片
var searchResults = await javBus.SearchAsync("ABW-001");

// 获取第一个结果的详情
if (searchResults.Any())
{
    var video = await javBus.GetDetailsAsync(searchResults.First().Url);
    Console.WriteLine($"标题：{video.Title}");
    Console.WriteLine($"番号：{video.Code}");
}
```

### 4. 自动匹配合适的刮削器

```csharp
string keyword = "HEYZO-1234";

// 查找可以处理该关键字的所有刮削器
var suitableScrapers = scraperFactory.FindScrapers(keyword);

foreach (var scraper in suitableScrapers)
{
    try
    {
        var results = await scraper.SearchAsync(keyword);
        if (results.Any())
        {
            Console.WriteLine($"{scraper.Name} 找到了数据");
            // 处理结果...
            break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{scraper.Name} 出错：{ex.Message}");
    }
}
```

### 5. 安全的创建方式（Try 模式）

```csharp
if (scraperFactory.TryCreateScraper("JavDB", out var javDb))
{
    // 成功创建，可以使用 javDb
    var results = await javDb.SearchAsync("IPX-123");
}
else
{
    // 创建失败，不会抛出异常
    Console.WriteLine("JavDB 刮削器不可用");
}
```

### 6. 注册自定义刮削器

```csharp
// 假设你有一个自己的刮削器实现
scraperFactory.RegisterFactory("MyCustomScraper", () =>
{
    return new MyCustomScraper(
        logger: logger,
        httpClient: httpClient,
        // 其他依赖...
    );
});

// 现在可以使用了
var custom = scraperFactory.CreateScraper("MyCustomScraper");
```

## 常用场景模板

### 场景 1：单个刮削器快速使用

```csharp
var scraper = scraperFactory.CreateScraper("JavBus");
var results = await scraper.SearchAsync("你的关键字");
// 处理结果...
```

### 场景 2：尝试多个刮削器

```csharp
var keyword = "你的关键字";
var scrapers = scraperFactory.FindScrapers(keyword);

foreach (var scraper in scrapers)
{
    var results = await scraper.SearchAsync(keyword);
    if (results.Any())
    {
        // 找到数据就停止
        var details = await scraper.GetDetailsAsync(results.First().Url);
        return details;
    }
}

throw new Exception("所有刮削器都未找到数据");
```

### 场景 3：检查后使用

```csharp
if (scraperFactory.IsRegistered("Dmm"))
{
    var dmm = scraperFactory.CreateScraper("Dmm");
    // 使用 DMM 刮削器...
}
```

### 场景 4：遍历所有刮削器

```csharp
scraperFactory.ForEachFactory((name, factoryFunc) =>
{
    Console.WriteLine($"发现刮削器：{name}");
    // 可选：创建实例
    // var scraper = factoryFunc();
});
```

## 完整示例

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using JavScraper.Scrapers;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 配置服务
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ScraperHttpClientFactory>();
        services.AddSingleton<ScraperCacheService>();
        services.AddSingleton<SampleImageService>();
        services.AddSingleton(new ScraperOptions());
        services.AddSingleton<ScraperFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();
        
        // 显示所有可用刮削器
        Console.WriteLine("可用刮削器列表:");
        foreach (var name in scraperFactory.GetAvailableScrapers())
        {
            Console.WriteLine($"  - {name}");
        }
        
        // 创建并使用刮削器
        Console.WriteLine("\n开始刮削 HEYZO-1234:");
        var scrapers = scraperFactory.FindScrapers("HEYZO-1234");
        
        foreach (var scraper in scrapers)
        {
            try
            {
                Console.WriteLine($"尝试使用 {scraper.Name}...");
                var results = await scraper.SearchAsync("HEYZO-1234");
                
                if (results.Any())
                {
                    Console.WriteLine($"✓ {scraper.Name} 找到了 {results.Count} 条结果");
                    var firstResult = results.First();
                    var details = await scraper.GetDetailsAsync(firstResult.Url);
                    
                    Console.WriteLine($"标题：{details.Title}");
                    Console.WriteLine($"番号：{details.Code}");
                    break;
                }
                else
                {
                    Console.WriteLine($"✗ {scraper.Name} 未找到结果");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {scraper.Name} 出错：{ex.Message}");
            }
        }
    }
}
```

## 注意事项

1. **每次创建新实例**：`CreateScraper()` 每次都会返回新实例，适合并发使用
2. **异常处理**：不存在的名称会抛出异常，建议使用 `TryCreateScraper` 或 `IsRegistered`
3. **线程安全**：所有方法都是线程安全的，可以在多线程环境中使用
4. **资源管理**：刮削器创建的 HTTP 客户端等资源会自动管理，无需手动释放

## 下一步

- 📖 查看完整文档：[README_ScraperFactory.md](README_ScraperFactory.md)
- 🧪 运行测试用例：`ScraperFactoryTests.cs`
- 💡 查看演示程序：`Tools/ScraperFactoryDemo.cs`
- 📝 阅读实现总结：[IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md)
