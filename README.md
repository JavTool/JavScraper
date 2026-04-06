# JavScraper

## 项目说明
本项目是一个影片刮削工具，主要针对 [JavHelper](https://javhelper.blogspot.com/2015/07/javhelper-v1.html)整理后的文件进行二次整理功能进行优化和扩展。

## 项目结构

本项目包含以下两个部分：
### JavScraper.App
JavScraper.App 是一个 Windows 桌面应用程序，使用 C# 编写。
目前只实现了以下功能：
- 影片信息刮削
- 封面裁切
- 文件整理
- 日志记录
### JavScraper.Tools
JavScraper.Tools 是一个命令行工具，使用 C# 编写。
目前只实现了以下功能：
- NFO 文件修复和优化
- 图片处理（封面、演员照片）
- 批量处理支持

## 核心组件

### ScraperFactory（刮削器工厂）
参考 [metatube-sdk-go provider](https://github.com/metatube-community/metatube-sdk-go/tree/main/provider) 设计模式实现的刮削器工厂类，用于按名称实例化各种 Scraper。

**主要特性：**
- ✅ 自动发现并注册所有实现了 `IScraperV2` 接口的刮削器
- ✅ 支持手动注册自定义刮削器工厂
- ✅ 线程安全（使用 ReaderWriterLockSlim）
- ✅ 按需创建实例，避免状态污染
- ✅ 支持遍历所有已注册的工厂

**使用示例：**
```csharp
// 获取 ScraperFactory 实例
var scraperFactory = serviceProvider.GetRequiredService<ScraperFactory>();

// 查看所有已注册的刮削器
var scrapers = scraperFactory.GetAvailableScrapers();

// 创建指定名称的刮削器实例
var javBus = scraperFactory.CreateScraper("JavBus");

// 查找可以处理特定关键字的刮削器
var suitableScrapers = scraperFactory.FindScrapers("HEYZO-1234");

// 手动注册自定义刮削器
scraperFactory.RegisterFactory("CustomScraper", () => new CustomScraper());
```

详细文档请参阅：[Scrapers/README_ScraperFactory.md](Scrapers/README_ScraperFactory.md)

## 项目依赖

本项目依赖以下第三方库：

部分功能使用了 [Emby.Plugins.JavScraper](https://github.com/JavScraper/Emby.Plugins.JavScraper) 中的代码。

## 主要功能

1. 影片信息刮削
   - 支持从多个数据源获取影片信息
   - 自动下载封面图片和演员图片
   - 生成 NFO 元数据文件

2. 文件整理
   - 自动规范化文件夹和文件命名
   - 支持无码/有码影片分类
   - 支持字幕标记处理

3. 工具集
   - NFO 文件修复和优化
   - 图片处理（封面、演员照片）
   - 批量处理支持

## 使用说明

1. 基础功能
   - 运行程序后按照菜单提示选择功能
   - 输入需要处理的文件夹路径
   - 程序会自动处理文件名、下载图片和元数据

2. 文件命名规则
   - 支持自动识别影片番号
   - 自动添加字幕标记（[中字]等）
   - 支持无码流出特殊标记

3. 日志
   - 程序运行日志保存在 Logs 目录
   - 支持错误追踪和处理进度查看

## 注意事项

1. 运行环境要求
   - 需要 .NET 运行环境
   - 需要网络连接以下载元数据

2. 文件处理
   - 建议先备份重要文件
   - 确保有足够的磁盘空间
   - 注意文件命名的特殊字符处理

