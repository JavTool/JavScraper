using System.Threading.Tasks;
using JavScraper.Domain;
using System.Collections.Generic;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 新版本刮削器接口标准
    /// </summary>
    public interface IScraperV2
    {
        /// <summary>
        /// 获取刮削器名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 检查是否可以处理指定的关键字
        /// </summary>
        /// <param name="keyword">视频编号或关键字</param>
        /// <returns>如果可以处理返回 true，否则返回 false</returns>
        bool CanHandle(string keyword);

        /// <summary>
        /// 搜索视频信息
        /// </summary>
        /// <param name="keyword">搜索关键字</param>
        /// <returns>搜索结果列表</returns>
        Task<List<JavVideoIndex>> SearchAsync(string keyword);

        /// <summary>
        /// 获取视频详细信息
        /// </summary>
        /// <param name="url">视频详情页面地址</param>
        /// <returns>视频详细信息</returns>
        Task<JavVideo> GetDetailsAsync(string url);

        /// <summary>
        /// 获取演员信息
        /// </summary>
        /// <param name="url">演员页面地址</param>
        /// <returns>演员信息</returns>
        Task<JavPerson> GetPersonAsync(string url);
    }
}