using System.IO;
using System.Text.Json;

namespace JavScraper.Tools.Configuration
{
    /// <summary>
    /// 样本图片下载配置
    /// </summary>
    public class SampleImageConfig
    {
        /// <summary>
        /// 是否使用单独目录下载样本图片
        /// </summary>
        public bool UseSeparateDirectory { get; set; } = true;

        /// <summary>
        /// 单独目录名称
        /// </summary>
        public string DirectoryName { get; set; } = "sample";

        /// <summary>
        /// 从配置文件加载配置
        /// </summary>
        /// <param name="configFilePath">配置文件路径</param>
        /// <returns>配置实例</returns>
        public static SampleImageConfig LoadFromFile(string configFilePath = "config.json")
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    // 如果配置文件不存在，返回默认配置
                    return new SampleImageConfig();
                }

                var jsonContent = File.ReadAllText(configFilePath);
                var configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return configRoot?.SampleImageSettings ?? new SampleImageConfig();
            }
            catch
            {
                // 如果读取失败，返回默认配置
                return new SampleImageConfig();
            }
        }
    }

    /// <summary>
    /// 配置文件根对象
    /// </summary>
    internal class ConfigRoot
    {
        public SampleImageConfig SampleImageSettings { get; set; }
    }
}