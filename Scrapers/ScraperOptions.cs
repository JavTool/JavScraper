using System.Collections.Generic;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 刮削器配置选项
    /// </summary>
    public class ScraperOptions
    {
        /// <summary>
        /// 默认超时时间（秒）
        /// </summary>
        public int DefaultTimeout { get; set; } = 30;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// 重试延迟（毫秒）
        /// </summary>
        public int RetryDelay { get; set; } = 1000;

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpiration { get; set; } = 60;

        /// <summary>
        /// 代理服务器设置
        /// </summary>
        public ProxySettings Proxy { get; set; } = new ProxySettings();

        /// <summary>
        /// 站点特定配置
        /// </summary>
        public Dictionary<string, SiteSpecificSettings> SiteSettings { get; set; } = new Dictionary<string, SiteSpecificSettings>();
    }

    /// <summary>
    /// 代理服务器设置
    /// </summary>
    public class ProxySettings
    {
        /// <summary>
        /// 是否启用代理
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 代理服务器地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 代理服务器端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 代理服务器用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 代理服务器密码
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// 站点特定设置
    /// </summary>
    public class SiteSpecificSettings
    {
        /// <summary>
        /// 站点基础URL
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// 自定义请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 站点特定的超时时间（秒）
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// 是否使用特定的代理设置
        /// </summary>
        public ProxySettings SiteProxy { get; set; }

        /// <summary>
        /// 站点特定的重试设置
        /// </summary>
        public int? MaxRetries { get; set; }

        /// <summary>
        /// 站点特定的重试延迟（毫秒）
        /// </summary>
        public int? RetryDelay { get; set; }
    }
}