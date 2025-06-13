using JavScraper.Tools.Entities;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// 无码视频刮削器接口
    /// </summary>
    public interface IUncensoredScraper
    {
        /// <summary>
        /// 获取刮削器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 检查是否支持指定的 JAV ID
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>如果支持返回true，否则返回 false</returns>
        bool CanHandle(JavId javId);

        /// <summary>
        /// 获取视频元数据
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <returns>视频信息</returns>
        Task<JavVideo> GetMetadataAsync(JavId javId);
    }
}