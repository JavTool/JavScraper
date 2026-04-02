using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    /// <summary>
    /// 标题特殊字符/词语清理配置
    /// </summary>
    public class TitleSanitizerConfig
    {
        /// <summary>
        /// 需要从标题中完全移除的字符串列表（按项匹配）。
        /// </summary>
        public List<string> RemoveStrings { get; set; } = new List<string>
        {
            // 保持与历史逻辑一致的默认移除项
            "無碼 ",
            "無修正 カリビアンコム "
        };

        /// <summary>
        /// 需要替换的字符串映射，键为要被替换的内容，值为替换后的内容。
        /// </summary>
        public Dictionary<string, string> ReplaceMap { get; set; } = [];

        /// <summary>
        /// 从配置文件加载配置（默认从 config.json 中读取 TitleSanitizerSettings 节点）。
        /// </summary>
        /// <param name="configFilePath">配置文件路径，默认为 config.json</param>
        /// <returns>配置实例；如果读取失败返回默认配置。</returns>
        public static TitleSanitizerConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return new TitleSanitizerConfig();

                var json = File.ReadAllText(configFilePath);
                var root = JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.TitleSanitizerSettings ?? new TitleSanitizerConfig();
            }
            catch
            {
                return new TitleSanitizerConfig();
            }
        }

        internal class ConfigRoot
        {
            public TitleSanitizerConfig TitleSanitizerSettings { get; set; }
        }
    }
}

