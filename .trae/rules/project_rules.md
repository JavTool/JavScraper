
📘 JavScraper 项目开发规范（project_rules.md）

📂 一、项目结构说明

JavScraper.sln
├── Emby/                           # Emby 相关逻辑封装
├── Emby.Common/                   # Emby 公共通用模块
├── Emby.Plugins.JavScraper/       # 插件集成（如 DLL）
├── JavScraper/                    # 主程序入口（逻辑 + CLI/WPF）
├── JavScraper.App/                # 桌面端 UI 项目（如 WPF）
├── JavScraper.Common/             # 公共代码（模型、扩展等）
├── JavScraper.Tools/              # 实用工具脚本或命令
├── JavScraper.Test/               # 单元测试项目
├── Scrapers/                      # 各站点爬虫适配器
├── tools/                         # 第三方工具或依赖（如 ffmpeg）
├── README.md
└── LICENSE


⸻

✅ 二、开发通用原则

规范项	说明
框架版本	.NET 8（跨平台）
项目依赖控制	除 HtmlAgilityPack、HttpClient 外尽量不依赖第三方库
命名规范	PascalCase（类/属性），camelCase（变量/参数）
注释规范	注释内容中英文和中文用空格分隔，所有 public 方法/类必须使用三段式 XML 注释（含参数说明），方法内部注释可使用 //
日志记录	使用 Microsoft.Extensions.Logging 接口标准封装
异常处理	所有逻辑必须包裹 try-catch，错误应记录详细信息


⸻

🧱 三、目录模块职责说明

文件夹/模块	功能说明
JavScraper	CLI / 通用主入口程序，负责参数解析与启动流程
JavScraper.App	桌面端 UI（建议 WPF 实现）
JavScraper.Common	公共实体类（如 JavItem, JavActor），通用逻辑
JavScraper.Tools	工具脚本，如图片重命名、视频校验等
JavScraper.Test	单元测试项目
Scrapers	每个站点的适配器，策略模式实现不同站点解析
Emby.*	Emby 插件或元数据同步实现


⸻

🔁 四、设计模式推荐应用

模式名称	应用场景
策略模式	多个网站（如 heyzo, caribbean）爬虫接口分离
工厂模式	Scraper 实例动态创建
单例模式	日志器、配置器等只需初始化一次的组件
命令模式	控制台参数封装为命令类
观察者模式	用于桌面端响应爬取状态更新


⸻

🧪 五、测试约定
	•	所有核心逻辑必须位于 JavScraper.Common 或 Scrapers 中，确保可被 JavScraper.Test 调用。
	•	测试使用 xUnit，测试项目必须包含覆盖率分析。
	•	尽量使用 mock 网络响应，避免真实请求。

⸻

🧰 六、注释范例（推荐模板）

/// <summary>
/// 下载并解析 JAV 视频信息。
/// </summary>
/// <param name="code">视频编号，例如：ABP-123</param>
/// <param name="site">目标资源站（如 caribbean、heyzo）</param>
/// <returns>返回 JavItem 实例，包含标题、演员、封面等信息</returns>
public async Task<JavItem> FetchMetadataAsync(string code, string site)


⸻

📦 七、Scraper 模块接口标准（核心）

/// <summary>
/// 所有资源站爬虫适配器必须实现该接口。
/// </summary>
public interface IScraper
{
    /// <summary>
    /// 根据番号解析并返回信息。
    /// </summary>
    /// <param name="code">番号</param>
    /// <returns>影片数据</returns>
    Task<JavItem?> FetchAsync(string code);
}

示例：

public class HeyzoScraper : IScraper
{
    public async Task<JavItem?> FetchAsync(string code)
    {
        // 使用 HttpClient 下载 HTML 并解析
    }
}


⸻

🖥 八、跨平台建议

建议项	示例/说明
文件路径统一使用	Path.Combine 而不是 C:\xxx
避免使用 Windows API	核心逻辑不要引用 System.Windows.Forms.* 等
配置读取推荐使用	IConfiguration 支持 json/env/ini 自动解析
图像处理推荐使用	System.Drawing.Common（注意 Linux 兼容）


⸻

🔐 九、安全性建议
	•	网络请求应配置 User-Agent，避免被屏蔽。
	•	下载资源前先判断内容类型、大小限制。
	•	统一封装 HttpHelper 工具类，限制重试次数和超时。

⸻

📄 十、通用代码模板建议

Program.cs（控制台）

static async Task Main(string[] args)
{
    var scraper = ScraperFactory.Create("heyzo");
    var item = await scraper.FetchAsync("1234");
    Console.WriteLine(item.Title);
}

ScraperFactory.cs

public static class ScraperFactory
{
    public static IScraper Create(string site)
    {
        return site.ToLower() switch
        {
            "heyzo" => new HeyzoScraper(),
            "caribbean" => new CaribbeanScraper(),
            _ => throw new NotSupportedException($"不支持站点：{site}")
        };
    }
}

