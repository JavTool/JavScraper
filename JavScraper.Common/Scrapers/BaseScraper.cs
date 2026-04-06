using System.Threading.Tasks;
using JavScraper.Common.Models;

namespace JavScraper.Common.Scrapers
{
    /// <summary>
    /// 公共刮片器基类，供各 provider 在 Common 项目中实现。
    /// 该类提供最小的契约：Name、DefaultBaseUrl 与异步抓取方法。
    /// </summary>
    public abstract class BaseScraper
    {
        /// <summary>
        /// 刮片器名称（例如 "caribbeancom"）。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 默认基础地址（可为空）。
        /// </summary>
        public string DefaultBaseUrl { get; }

        protected BaseScraper(string name, string defaultBaseUrl = null)
        {
            Name = name;
            DefaultBaseUrl = defaultBaseUrl;
        }

        /// <summary>
        /// 使用番号或标识抓取并返回 Movie 元数据。
        /// </summary>
        /// <param name="code">番号或其它唯一标识</param>
        /// <returns>抓取到的 Movie 实例或 null</returns>
        public abstract Task<JavVideo> ScrapeAsync(string code);

        /// <summary>
        /// 简单关键字检查，子类可重写以实现更复杂的校验。
        /// </summary>
        public virtual bool CheckKeyword(string keyword)
            => !string.IsNullOrWhiteSpace(keyword);

        /// <summary>
        /// 检查是否可以处理指定的关键字（用于 ScraperFactory 自动匹配）
        /// </summary>
        /// <param name="keyword">视频编号或关键字</param>
        /// <returns>如果可以处理返回 true，否则返回 false</returns>
        public virtual bool CanHandle(string keyword)
            => CheckKeyword(keyword);
    }
}
