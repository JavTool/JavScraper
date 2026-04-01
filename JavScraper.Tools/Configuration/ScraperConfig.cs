using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    /// <summary>
    /// 刮削器选择配置
    /// </summary>
    public class ScraperConfig
    {
        /// <summary>
        /// 优先使用的刮削器名称列表（按顺序尝试）。
        /// 可使用的名称示例："Jav123", "JavCaptain"。
        /// </summary>
        public List<string> PreferredScrapers { get; set; } = ["DMM", "Jav123", "JavCaptain"];

        /// <summary>
        /// 从配置文件加载 ScraperConfig（默认从 config.json 的 ScraperSettings 节点读取）。
        /// </summary>
        /// <param name="configFilePath">配置文件路径，默认为 config.json</param>
        /// <returns>配置实例；如果读取失败返回默认配置。</returns>
        public static ScraperConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return new ScraperConfig();

                var json = File.ReadAllText(configFilePath);
                var root = JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.ScraperSettings ?? new ScraperConfig();
            }
            catch
            {
                return new ScraperConfig();
            }
        }

        internal class ConfigRoot
        {
            public ScraperConfig ScraperSettings { get; set; }
        }
    }
}
