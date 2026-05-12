using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    /// <summary>
    /// Tag/Genre 替换与清理配置
    /// </summary>
    public class TagReplaceConfig
    {
        /// <summary>
        /// 是否启用 Tag 替换/清理功能
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 替换映射：键为目标（统一）名称，值为该目标名称的所有别名列表。
        /// 例如: { "白虎": ["剃毛・パイパン", "パイパン"] }
        /// </summary>
        public Dictionary<string, List<string>> Replacements { get; set; } = new();

        /// <summary>
        /// 需要从 Tag/Genre 列表中移除的词条。
        /// </summary>
        public List<string> RemoveTerms { get; set; } = new();

        public static TagReplaceConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return new TagReplaceConfig();

                var json = File.ReadAllText(configFilePath);
                var root = JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.TagReplaceSettings ?? new TagReplaceConfig();
            }
            catch
            {
                return new TagReplaceConfig();
            }
        }

        internal class ConfigRoot
        {
            public TagReplaceConfig TagReplaceSettings { get; set; }
        }
    }
}
