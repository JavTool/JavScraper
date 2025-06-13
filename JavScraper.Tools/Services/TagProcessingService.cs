using System;
using System.Collections.Generic;
using System.Linq;
using JavScraper.Tools.Configuration;
using JavScraper.Tools.Models;
using Microsoft.Extensions.Logging;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// 标签处理服务，负责处理和转换标签数据。
    /// </summary>
    public class TagProcessingService
    {
        private readonly ILogger<TagProcessingService> _logger;

        public TagProcessingService(ILogger<TagProcessingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理标签列表，进行翻译和过滤。
        /// </summary>
        /// <param name="genres">原始类型列表</param>
        /// <returns>处理结果</returns>
        public TagProcessResult ProcessTags(List<string> genres)
        {
            try
            {
                var result = new TagProcessResult();

                if (genres == null || !genres.Any())
                {
                    _logger.LogDebug("输入的类型列表为空");
                    return result;
                }

                _logger.LogDebug("开始处理 {Count} 个标签", genres.Count);

                var processedGenres = new List<string>();
                var processedTags = new List<string>();

                foreach (var genre in genres)
                {
                    if (string.IsNullOrWhiteSpace(genre))
                    {
                        continue;
                    }

                    var trimmedGenre = genre.Trim();

                    // 检查是否需要移除
                    if (ShouldRemoveTag(trimmedGenre))
                    {
                        _logger.LogDebug("移除标签: {Tag}", trimmedGenre);
                        continue;
                    }

                    // 翻译标签
                    var translatedTag = TranslateTag(trimmedGenre);

                    // 分类处理
                    if (IsGenreTag(translatedTag))
                    {
                        if (!processedGenres.Contains(translatedTag, StringComparer.OrdinalIgnoreCase))
                        {
                            processedGenres.Add(translatedTag);
                        }
                    }
                    else
                    {
                        if (!processedTags.Contains(translatedTag, StringComparer.OrdinalIgnoreCase))
                        {
                            processedTags.Add(translatedTag);
                        }
                    }
                }

                result.ProcessedGenres = processedGenres;
                result.ProcessedTags = processedTags;
                result.OriginalCount = genres.Count;
                result.ProcessedCount = processedGenres.Count + processedTags.Count;

                _logger.LogDebug("标签处理完成: 原始 {Original} 个，处理后 {Processed} 个（类型 {Genres} 个，标签 {Tags} 个）",
                    result.OriginalCount, result.ProcessedCount, processedGenres.Count, processedTags.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理标签时发生错误");
                return new TagProcessResult();
            }
        }

        /// <summary>
        /// 翻译单个标签。
        /// </summary>
        /// <param name="tag">原始标签</param>
        /// <returns>翻译后的标签</returns>
        public string TranslateTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            var trimmedTag = tag.Trim();

            // 直接匹配
            if (TagMappingConfig.AvTerms.TryGetValue(trimmedTag, out var directTranslation))
            {
                return directTranslation;
            }

            // 模糊匹配（包含关系）
            var fuzzyMatch = TagMappingConfig.AvTerms
                .FirstOrDefault(kvp => trimmedTag.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(fuzzyMatch.Key))
            {
                return fuzzyMatch.Value;
            }

            // 反向模糊匹配（标签包含在映射键中）
            var reverseMatch = TagMappingConfig.AvTerms
                .FirstOrDefault(kvp => kvp.Key.Contains(trimmedTag, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(reverseMatch.Key))
            {
                return reverseMatch.Value;
            }

            // 无匹配，返回原标签
            return trimmedTag;
        }

        /// <summary>
        /// 检查是否应该移除标签。
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否应该移除</returns>
        public bool ShouldRemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return true;
            }

            var trimmedTag = tag.Trim();

            // 检查移除列表
            if (TagMappingConfig.RemoveTermsList.Any(term => 
                trimmedTag.Equals(term, StringComparison.OrdinalIgnoreCase) ||
                trimmedTag.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 检查特殊模式
            return IsSpecialRemovePattern(trimmedTag);
        }

        /// <summary>
        /// 检查是否为特殊移除模式。
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否为特殊移除模式</returns>
        private bool IsSpecialRemovePattern(string tag)
        {
            // 移除纯数字标签
            if (int.TryParse(tag, out _))
            {
                return true;
            }

            // 移除过短的标签
            if (tag.Length <= 1)
            {
                return true;
            }

            // 移除特殊字符标签
            if (tag.All(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断是否为类型标签（而非普通标签）。
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>是否为类型标签</returns>
        private bool IsGenreTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            // 定义类型关键词
            var genreKeywords = new[]
            {
                "剧情", "无码", "中文字幕", "素人", "人妻", "熟女", "美少女", "巨乳", "美乳",
                "角色扮演", "制服", "护士", "女教师", "OL", "女仆", "和服", "泳装",
                "群交", "三人行", "双飞", "乱交", "轮奸", "3P", "多P",
                "口交", "肛交", "内射", "颜射", "口内射精", "深喉", "乳交",
                "自慰", "玩具", "振动棒", "电动按摩棒", "假阳具",
                "野外", "车震", "温泉", "浴室", "按摩", "POV",
                "SM", "捆绑", "调教", "拘束", "蒙眼", "口塞",
                "萝莉", "辣妹", "模特", "偶像", "清纯", "性感", "丰满", "苗条"
            };

            return genreKeywords.Any(keyword => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 批量处理标签列表。
        /// </summary>
        /// <param name="tagLists">多个标签列表</param>
        /// <returns>批量处理结果</returns>
        public List<TagProcessResult> ProcessTagsBatch(IEnumerable<List<string>> tagLists)
        {
            var results = new List<TagProcessResult>();

            foreach (var tagList in tagLists)
            {
                var result = ProcessTags(tagList);
                results.Add(result);
            }

            _logger.LogInformation("批量处理完成，共处理 {Count} 个标签列表", results.Count);
            return results;
        }

        /// <summary>
        /// 获取处理统计信息。
        /// </summary>
        /// <param name="results">处理结果列表</param>
        /// <returns>统计信息</returns>
        public TagProcessStatistics GetProcessingStatistics(IEnumerable<TagProcessResult> results)
        {
            var resultList = results.ToList();

            return new TagProcessStatistics
            {
                TotalProcessed = resultList.Count,
                TotalOriginalTags = resultList.Sum(r => r.OriginalCount),
                TotalProcessedTags = resultList.Sum(r => r.ProcessedCount),
                TotalGenres = resultList.Sum(r => r.ProcessedGenres.Count),
                TotalTags = resultList.Sum(r => r.ProcessedTags.Count),
                AverageReductionRate = resultList.Count > 0 
                    ? resultList.Average(r => r.OriginalCount > 0 
                        ? (double)(r.OriginalCount - r.ProcessedCount) / r.OriginalCount 
                        : 0) 
                    : 0
            };
        }
    }
}