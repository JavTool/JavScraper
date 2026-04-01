using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    /// <summary>
    /// 演员名称替换配置
    /// </summary>
    public class ActorReplaceConfig
    {
        /// <summary>
        /// 是否启用演员名称替换功能
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 替换映射：键为目标（统一）名称，值为该目标名称所有的别名列表
        /// 例如: { "坂道美琉": ["miru","坂道みる"] }
        /// </summary>
        public Dictionary<string, List<string>> Replacements { get; set; } = new Dictionary<string, List<string>>
        {
            { "坂道美琉", new List<string> { "miru", "坂道みる" } },
            { "田中柠檬", new List<string> { "枫カレン", "田中レモン" } }
        };

        public static ActorReplaceConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return new ActorReplaceConfig();

                var json = File.ReadAllText(configFilePath);
                var root = JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.ActorReplaceSettings ?? new ActorReplaceConfig();
            }
            catch
            {
                return new ActorReplaceConfig();
            }
        }

        internal class ConfigRoot
        {
            public ActorReplaceConfig ActorReplaceSettings { get; set; }
        }
    }
}
