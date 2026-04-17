using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    public class BackupConfig
    {
        /// <summary>
        /// 是否备份 NFO（元数据）
        /// </summary>
        public bool BackupNfo { get; set; } = true;

        /// <summary>
        /// 是否备份图片（封面/示例图）
        /// </summary>
        public bool BackupImages { get; set; } = true;

        public static BackupConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                    return new BackupConfig();

                var json = File.ReadAllText(configFilePath);
                var root = JsonSerializer.Deserialize<ConfigRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return root?.BackupSettings ?? new BackupConfig();
            }
            catch
            {
                return new BackupConfig();
            }
        }

        internal class ConfigRoot
        {
            public BackupConfig BackupSettings { get; set; }
        }
    }
}
