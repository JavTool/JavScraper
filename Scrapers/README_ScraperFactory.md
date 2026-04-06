# ScraperFactory 使用指南

## 概述

`ScraperFactory` 是参考 [metatube-sdk-go provider](https://github.com/metatube-community/metatube-sdk-go/tree/main/provider) 设计模式实现的刮削器工厂类，用于按名称实例化各种 Scraper。

## 核心特性

### 1. 自动注册
启动时自动发现并注册所有实现了 `IScraperV2` 接口的非抽象类。

### 2. 手动注册
支持手动注册自定义的刮削器工厂函数。

### 3. 线程安全
使用 `ReaderWriterLockSlim` 保护工厂集合，确保多线程访问时的安全性。

### 4. 按需实例化
每次调用 `CreateScraper` 都会创建新的实例，避免状态污染。

## 使用示例

### 基础用法

```csharp
// 获取 ScraperFactory 实例（通过 DI 容器）
var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();

// 查看所有已注册的刮削器
var availableScrapers = scraperFactory.GetAvailableScrapers();
foreach (var name in availableScrapers)
{
    Console.WriteLine($"可用刮削器：{name}");
}

// 创建指定名称的刮削器实例
var javBus = scraperFactory.CreateScraper("JavBus");
Console.WriteLine($"创建刮削器：{javBus.Name}");

// 搜索视频
var searchResults = await javBus.SearchAsync("ABW-001");
foreach (var video in searchResults)
{
    Console.WriteLine($"{video.Title} - {video.Url}");
}

// 获取视频详情
var videoDetails = await javBus.GetDetailsAsync(searchResults.First().Url);
Console.WriteLine($"番号：{videoDetails.Code}");
Console.WriteLine($"标题：{videoDetails.Title}");
```

### 条件创建（Try 模式）

```csharp
// 尝试创建刮削器，失败时不会抛出异常
if (scraperFactory.TryCreateScraper("JavDB", out var javDb))
{
    var results = await javDb.SearchAsync("IPX-123");
    // 处理结果...
}
else
{
    Console.WriteLine("JavDB 刮削器不可用");
}
```

### 查找合适的刮削器

```csharp
// 根据关键字自动查找可以处理的刮削器
var keyword = "HEYZO-1234";
var suitableScrapers = scraperFactory.FindScrapers(keyword);

foreach (var scraper in suitableScrapers)
{
    Console.WriteLine($"刮削器 {scraper.Name} 可以处理该关键字");
    var results = await scraper.SearchAsync(keyword);
    // 处理结果...
}
```

### 遍历所有工厂

```csharp
// 使用 ForEachFactory 遍历所有注册的工厂（类似 Go 的 Range 模式）
scraperFactory.ForEachFactory((name, factoryFunc) =>
{
    Console.WriteLine($"注册工厂：{name}");
    
    // 可以选择不创建实例，或者按需创建
    if (name == "JavBus")
    {
        var scraper = factoryFunc();
        // 使用 scraper...
    }
});
```

### 手动注册自定义刮削器

```csharp
// 注册一个简单的自定义刮削器
scraperFactory.RegisterFactory("CustomScraper", () =>
{
    return new CustomScraper(
        logger: loggerFactory.CreateLogger<CustomScraper>(),
        clientFactory: httpClientFactory,
        cacheService: cacheService,
        options: scraperOptions
    );
});

// 现在可以使用自定义刮削器
var custom = scraperFactory.CreateScraper("CustomScraper");
```

### 检查刮削器是否已注册

```csharp
if (scraperFactory.IsRegistered("Dmm"))
{
    var dmm = scraperFactory.CreateScraper("Dmm");
    // 使用 DMM 刮削器...
}
else
{
    Console.WriteLine("DMM 刮削器未注册");
}
```

### 获取工厂数量

```csharp
var count = scraperFactory.Count;
Console.WriteLine($"已注册 {count} 个刮削器");
```

## 完整的刮削流程示例

```csharp
public class JavScraperService
{
    private readonly ScraperFactory _scraperFactory;
    private readonly ILogger<JavScraperService> _logger;

    public JavScraperService(
        ScraperFactory scraperFactory,
        ILogger<JavScraperService> logger)
    {
        _scraperFactory = scraperFactory;
        _logger = logger;
    }

    public async Task<JavVideo> ScrapeMovie(string keyword)
    {
        _logger.LogInformation($"开始刮削：{keyword}");
        
        // 查找可以处理该关键字的刮削器
        var scrapers = _scraperFactory.FindScrapers(keyword);
        var scraperList = scrapers.ToList();
        
        if (!scraperList.Any())
        {
            throw new InvalidOperationException($"没有找到可以处理 {keyword} 的刮削器");
        }

        _logger.LogInformation($"找到 {scraperList.Count} 个候选刮削器");

        // 依次尝试每个刮削器
        foreach (var scraper in scraperList)
        {
            try
            {
                _logger.LogDebug($"尝试使用 {scraper.Name} 刮削");
                
                // 搜索
                var searchResults = await scraper.SearchAsync(keyword);
                if (searchResults == null || !searchResults.Any())
                {
                    _logger.LogDebug($"{scraper.Name} 未找到结果");
                    continue;
                }

                // 获取第一个结果的详情
                var firstResult = searchResults.First();
                var details = await scraper.GetDetailsAsync(firstResult.Url);
                
                if (details != null)
                {
                    _logger.LogInformation($"成功从 {scraper.Name} 获取数据");
                    return details;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{scraper.Name} 刮削失败");
                // 继续尝试下一个刮削器
            }
        }

        throw new InvalidOperationException($"所有刮削器都无法获取 {keyword} 的数据");
    }
}
```

## 依赖注入配置

在 `Startup.cs` 或 `Program.cs` 中配置：

```csharp
services.AddSingleton<ScraperFactory>();
services.AddSingleton<ScraperCacheService>();
services.AddSingleton<ScraperHttpClientFactory>();
services.AddSingleton<SampleImageService>();

// 配置各个 Scraper 的 Options
services.Configure<ScraperOptions>(options =>
{
    options.SiteSettings["JavBus"] = new SiteSetting
    {
        BaseUrl = "https://www.javbus.com",
        Enabled = true
    };
    // 其他站点配置...
});
```

## 性能建议

1. **缓存实例**：虽然每次都会创建新实例，但对于频繁使用的刮削器，可以考虑缓存
2. **批量操作**：使用 `FindScrapers` 时注意会创建多个实例
3. **异常处理**：始终捕获可能的异常，特别是网络请求相关的异常

## 与 metatube-sdk-go 的对应关系

| Go SDK | C# 实现 |
|--------|---------|
| `provider.Register()` | `RegisterFactory()` |
| `RangeMovieFactory()` | `ForEachFactory()` |
| `MovieProvider` | `IScraperV2` |
| `sync.RWMutex` | `ReaderWriterLockSlim` |
| Factory Map | `_factories` Dictionary |

## 注意事项

1. 刮削器名称区分大小写（内部使用 `StringComparer.OrdinalIgnoreCase`）
2. 每次 `CreateScraper` 都会创建新实例，注意资源管理
3. 网络请求应该实现重试和超时机制
4. 建议使用缓存服务减少重复请求
