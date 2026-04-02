using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using JavScraper.App.Models;

namespace JavScraper.App.Services
{
    /// <summary>
    /// 配置读写服务，封装 config.json 的加载与保存逻辑。
    /// </summary>
    public static class ConfigService
    {
        private static string GetConfigPath()
        {
            return Path.Combine(Application.StartupPath, "config.json");
        }

        /// <summary>
        /// 从配置文件加载 <see cref="AppConfig"/>。
        /// 如果文件不存在或解析失败，返回一个具有默认值的新实例。
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path))
                    return new AppConfig();

                string json = File.ReadAllText(path, Encoding.UTF8);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, options);
                return cfg ?? new AppConfig();
            }
            catch
            {
                // 读取配置失败时返回默认配置
                return new AppConfig();
            }
        }

        /// <summary>
        /// 将配置保存到文件（覆盖现有文件）。
        /// </summary>
        public static void Save(AppConfig cfg)
        {
            try
            {
                string path = GetConfigPath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(cfg, options);
                File.WriteAllText(path, json, Encoding.UTF8);
            }
            catch
            {
                // 忽略保存错误（上层可按需捕获）
            }
        }
    }
}
