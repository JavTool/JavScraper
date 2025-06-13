using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// 多刮削器服务，支持从多个数据源获取视频信息并选择最佳结果
    /// </summary>
    public class MultiScraperService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<MultiScraperService> _logger;
        private readonly Dictionary<string, IUncensoredScraper> _scrapers;

        public MultiScraperService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MultiScraperService>();
            _scrapers = new Dictionary<string, IUncensoredScraper>();
            InitializeScrapers();
        }

        /// <summary>
        /// 初始化所有支持的刮削器
        /// </summary>
        private void InitializeScrapers()
        {
            var javDBScraper = new JavUncensoredScraper(_loggerFactory);

            _scrapers.Add("Caribbeancom", new CaribbeancomScraper(javDBScraper));
            _scrapers.Add("CaribbeancomPR", new CaribbeancomPRScraper(javDBScraper));
            _scrapers.Add("1Pondo", new OnePondoScraper(javDBScraper));
            _scrapers.Add("Pacopacomama", new PacopacomamaScraper(javDBScraper));
            _scrapers.Add("Heyzo", new HeyzoScraper(javDBScraper));
            _scrapers.Add("AVE", new AVEScraper(javDBScraper));
            _scrapers.Add("FC2", new FC2Scraper(javDBScraper));
        }

        /// <summary>
        /// 从多个数据源获取视频元数据并返回最佳结果
        /// </summary>
        /// <param name="javId">JAV ID</param>
        /// <param name="makers">制作商列表</param>
        /// <returns>最佳的视频信息</returns>
        public async Task<JavVideo> GetBestMetadataAsync(JavId javId, List<string> makers = null)
        {
            var results = new List<ScraperResult>();

            // 如果提供了制作商列表，优先使用这些制作商
            if (makers != null && makers.Count > 0)
            {
                foreach (var maker in makers)
                {
                    if (_scrapers.TryGetValue(maker, out var scraper))
                    {
                        var result = await TryGetMetadataFromScraper(scraper, maker, javId);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }
            }
            else
            {
                // 如果没有提供制作商列表，尝试所有刮削器
                foreach (var kvp in _scrapers)
                {
                    var result = await TryGetMetadataFromScraper(kvp.Value, kvp.Key, javId);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }

            // 选择最佳结果
            return SelectBestResult(results, javId);
        }

        /// <summary>
        /// 尝试从指定刮削器获取元数据
        /// </summary>
        /// <param name="scraper">刮削器实例</param>
        /// <param name="scraperName">刮削器名称</param>
        /// <param name="javId">JAV ID</param>
        /// <returns>刮削结果</returns>
        private async Task<ScraperResult> TryGetMetadataFromScraper(IUncensoredScraper scraper, string scraperName, JavId javId)
        {
            try
            {
                var video = await scraper.GetMetadataAsync(javId);
                if (video != null)
                {
                    _logger.LogInformation($"成功从 {scraperName} 获取番号 {javId.Id} 的数据");
                    return new ScraperResult
                    {
                        Video = video,
                        ScraperName = scraperName,
                        Quality = CalculateQuality(video)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"从 {scraperName} 获取数据失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 计算视频信息的质量分数
        /// </summary>
        /// <param name="video">视频信息</param>
        /// <returns>质量分数（越高越好）</returns>
        private int CalculateQuality(JavVideo video)
        {
            int score = 0;

            // 基础信息完整性
            if (!string.IsNullOrWhiteSpace(video.Title)) score += 10;
            if (!string.IsNullOrWhiteSpace(video.Plot)) score += 5;
            if (video.GetDate().HasValue) score += 5;
            if (!string.IsNullOrWhiteSpace(video.Studio)) score += 3;
            if (!string.IsNullOrWhiteSpace(video.Director)) score += 2;

            // 演员信息
            if (video.Actors != null && video.Actors.Count > 0) score += 8;

            // 标签和类型
            if (video.Genres != null && video.Genres.Count > 0) score += 5;
            if (video.Tags != null && video.Tags.Count > 0) score += 3;

            // 图片信息
            if (!string.IsNullOrWhiteSpace(video.Cover)) score += 5;
            if (video.Samples != null && video.Samples.Count > 0) score += 5;

            return score;
        }

        /// <summary>
        /// 计算视频信息与目标番号的匹配度
        /// </summary>
        /// <param name="video">视频信息</param>
        /// <param name="javId">目标JAV ID</param>
        /// <returns>匹配度分数（越高越匹配）</returns>
        private int CalculateMatchScore(JavVideo video, JavId javId)
        {
            int matchScore = 0;

            // 番号匹配度（最重要）
            if (!string.IsNullOrWhiteSpace(video.Number))
            {
                if (string.Equals(video.Number, javId.Id, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 100; // 完全匹配
                }
                else if (video.Number.Contains(javId.Id, StringComparison.OrdinalIgnoreCase) ||
                         javId.Id.Contains(video.Number, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 80; // 部分匹配
                }
                else if (NormalizeJavId(video.Number) == NormalizeJavId(javId.Id))
                {
                    matchScore += 90; // 标准化后匹配
                }
            }

            // 制作商匹配度
            if (!string.IsNullOrWhiteSpace(video.Studio))
            {
                matchScore += 10; // 有制作商信息就加分
            }

            // 演员匹配度
            if (video.Actors != null && video.Actors.Count > 0)
            {
                foreach (var actor in video.Actors)
                {
                    if (!string.IsNullOrWhiteSpace(actor))
                    {
                        matchScore += 5; // 有演员信息就加分
                        break;
                    }
                }
            }

            // 发行日期匹配度
            if (video.GetDate().HasValue)
            {
                matchScore += 10;
            }

            return matchScore;
        }

        /// <summary>
        /// 标准化 JAV ID，去除特殊字符和空格
        /// </summary>
        /// <param name="javId">原始JAV ID</param>
        /// <returns>标准化后的 JAV ID</returns>
        private string NormalizeJavId(string javId)
        {
            if (string.IsNullOrWhiteSpace(javId))
                return string.Empty;

            return javId.Replace("-", "").Replace("_", "").Replace(" ", "").ToUpperInvariant();
        }

        /// <summary>
        /// 从多个结果中选择最佳的一个
        /// </summary>
        /// <param name="results">刮削结果列表</param>
        /// <param name="javId">JAV ID</param>
        /// <returns>最佳的视频信息</returns>
        private JavVideo SelectBestResult(List<ScraperResult> results, JavId javId)
        {
            if (results == null || results.Count == 0)
            {
                _logger.LogWarning($"所有来源都无法获取番号 {javId.Id} 的元数据");
                return null;
            }

            // 如果只有一个结果，直接返回
            if (results.Count == 1)
            {
                var singleResult = results[0];
                _logger.LogInformation($"只有一个来源 {singleResult.ScraperName} 返回结果，直接使用");
                return singleResult.Video;
            }

            // 计算每个结果与目标番号的匹配度
            var scoredResults = results.Select(r => new
            {
                Result = r,
                MatchScore = CalculateMatchScore(r.Video, javId),
                QualityScore = r.Quality
            }).ToList();

            // 按匹配度优先，质量分数次之进行排序
            var bestScoredResult = scoredResults
                .OrderByDescending(sr => sr.MatchScore)
                .ThenByDescending(sr => sr.QualityScore)
                .First();

            var bestResult = bestScoredResult.Result;

            _logger.LogInformation($"选择来自 {bestResult.ScraperName} 的结果作为最佳结果（匹配度：{bestScoredResult.MatchScore}，质量分数：{bestResult.Quality}）");

            // 如果有多个结果，可以考虑合并信息
            return MergeResults(results, bestResult);
        }

        /// <summary>
        /// 合并多个刮削结果的信息
        /// </summary>
        /// <param name="results">所有结果</param>
        /// <param name="bestResult">最佳结果</param>
        /// <returns>合并后的视频信息</returns>
        private JavVideo MergeResults(List<ScraperResult> results, ScraperResult bestResult)
        {
            var mergedVideo = bestResult.Video;

            foreach (var result in results.Where(r => r != bestResult))
            {
                var video = result.Video;

                // 补充缺失的信息
                if (string.IsNullOrWhiteSpace(mergedVideo.Plot) && !string.IsNullOrWhiteSpace(video.Plot))
                    mergedVideo.Plot = video.Plot;

                if (string.IsNullOrWhiteSpace(mergedVideo.Director) && !string.IsNullOrWhiteSpace(video.Director))
                    mergedVideo.Director = video.Director;

                if (string.IsNullOrWhiteSpace(mergedVideo.Date) && !string.IsNullOrWhiteSpace(video.Date))
                    mergedVideo.Date = video.Date;

                // 合并演员信息
                if (video.Actors != null && video.Actors.Count > 0)
                {
                    mergedVideo.Actors = mergedVideo.Actors ?? new List<string>();
                    foreach (var actor in video.Actors)
                    {
                        if (!mergedVideo.Actors.Contains(actor))
                            mergedVideo.Actors.Add(actor);
                    }
                }

                // 合并标签
                if (video.Genres != null && video.Genres.Count > 0)
                {
                    mergedVideo.Genres = mergedVideo.Genres ?? new List<string>();
                    foreach (var genre in video.Genres)
                    {
                        if (!mergedVideo.Genres.Contains(genre))
                            mergedVideo.Genres.Add(genre);
                    }
                }

                // 合并样本图片
                if (video.Samples != null && video.Samples.Count > 0)
                {
                    mergedVideo.Samples = mergedVideo.Samples ?? new List<string>();
                    foreach (var image in video.Samples)
                    {
                        if (!mergedVideo.Samples.Contains(image))
                            mergedVideo.Samples.Add(image);
                    }
                }
            }

            return mergedVideo;
        }
    }

    /// <summary>
    /// 刮削结果包装类
    /// </summary>
    internal class ScraperResult
    {
        public JavVideo Video { get; set; }
        public string ScraperName { get; set; }
        public int Quality { get; set; }
    }
}