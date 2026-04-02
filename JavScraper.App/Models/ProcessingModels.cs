using System;
using System.Collections.Generic;
using System.Linq;

namespace JavScraper.Tools.Models
{
    /// <summary>
    /// NFO 处理结果。
    /// </summary>
    public class NfoProcessResult
    {
        /// <summary>
        /// 文件路径。
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 是否处理成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息。
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// JAV ID。
        /// </summary>
        public string JavId { get; set; } = string.Empty;

        /// <summary>
        /// 是否有字幕。
        /// </summary>
        public bool HasSubtitles { get; set; }

        /// <summary>
        /// 处理后的标题。
        /// </summary>
        public string ProcessedTitle { get; set; } = string.Empty;

        /// <summary>
        /// 处理后的类型列表。
        /// </summary>
        public List<string> ProcessedGenres { get; set; } = new();

        /// <summary>
        /// 处理后的标签列表。
        /// </summary>
        public List<string> ProcessedTags { get; set; } = new();

        /// <summary>
        /// 处理开始时间。
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 处理结束时间。
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 处理耗时。
        /// </summary>
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    }

    /// <summary>
    /// 标签处理结果。
    /// </summary>
    public class TagProcessResult
    {
        /// <summary>
        /// 处理后的类型列表。
        /// </summary>
        public List<string> ProcessedGenres { get; set; } = new();

        /// <summary>
        /// 处理后的标签列表。
        /// </summary>
        public List<string> ProcessedTags { get; set; } = new();

        /// <summary>
        /// 原始标签数量。
        /// </summary>
        public int OriginalCount { get; set; }

        /// <summary>
        /// 处理后标签数量。
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 移除的标签数量。
        /// </summary>
        public int RemovedCount => OriginalCount - ProcessedCount;

        /// <summary>
        /// 移除率。
        /// </summary>
        public double RemovalRate => OriginalCount > 0 ? (double)RemovedCount / OriginalCount : 0;
    }

    /// <summary>
    /// 标题处理结果。
    /// </summary>
    public class TitleProcessResult
    {
        /// <summary>
        /// 处理后的标题。
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 处理后的原始标题。
        /// </summary>
        public string OriginalTitle { get; set; } = string.Empty;

        /// <summary>
        /// 是否添加了无码标识。
        /// </summary>
        public bool AddedUncensoredTag { get; set; }

        /// <summary>
        /// 是否添加了字幕标识。
        /// </summary>
        public bool AddedSubtitleTag { get; set; }
    }

    /// <summary>
    /// 标签处理统计信息。
    /// </summary>
    public class TagProcessStatistics
    {
        /// <summary>
        /// 处理的总数量。
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// 原始标签总数。
        /// </summary>
        public int TotalOriginalTags { get; set; }

        /// <summary>
        /// 处理后标签总数。
        /// </summary>
        public int TotalProcessedTags { get; set; }

        /// <summary>
        /// 类型总数。
        /// </summary>
        public int TotalGenres { get; set; }

        /// <summary>
        /// 标签总数。
        /// </summary>
        public int TotalTags { get; set; }

        /// <summary>
        /// 平均减少率。
        /// </summary>
        public double AverageReductionRate { get; set; }

        /// <summary>
        /// 总减少数量。
        /// </summary>
        public int TotalReduced => TotalOriginalTags - TotalProcessedTags;

        /// <summary>
        /// 总减少率。
        /// </summary>
        public double TotalReductionRate => TotalOriginalTags > 0 ? (double)TotalReduced / TotalOriginalTags : 0;
    }

    /// <summary>
    /// 批量处理结果。
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>
        /// 处理的文件列表。
        /// </summary>
        public List<NfoProcessResult> Results { get; set; } = new();

        /// <summary>
        /// 成功处理的数量。
        /// </summary>
        public int SuccessCount => Results.Count(r => r.IsSuccess);

        /// <summary>
        /// 失败处理的数量。
        /// </summary>
        public int FailureCount => Results.Count(r => !r.IsSuccess);

        /// <summary>
        /// 总处理数量。
        /// </summary>
        public int TotalCount => Results.Count;

        /// <summary>
        /// 成功率。
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;

        /// <summary>
        /// 处理开始时间。
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 处理结束时间。
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 总耗时。
        /// </summary>
        public TimeSpan? TotalDuration => EndTime?.Subtract(StartTime);

        /// <summary>
        /// 平均耗时。
        /// </summary>
        public TimeSpan? AverageDuration => TotalDuration.HasValue && TotalCount > 0 
            ? TimeSpan.FromMilliseconds(TotalDuration.Value.TotalMilliseconds / TotalCount) 
            : null;
    }

    /// <summary>
    /// 文件分组信息。
    /// </summary>
    public class NfoFileGroup
    {
        /// <summary>
        /// 目录名称。
        /// </summary>
        public string DirectoryName { get; set; } = string.Empty;

        /// <summary>
        /// 目录路径。
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// NFO 文件列表。
        /// </summary>
        public List<string> NfoFiles { get; set; } = new();

        /// <summary>
        /// 主要的 NFO 文件（通常是 movie.nfo）。
        /// </summary>
        public string PrimaryNfoFile { get; set; } = string.Empty;

        /// <summary>
        /// 是否有主要 NFO 文件。
        /// </summary>
        public bool HasPrimaryNfo => !string.IsNullOrEmpty(PrimaryNfoFile);

        /// <summary>
        /// NFO 文件数量。
        /// </summary>
        public int FileCount => NfoFiles.Count;
    }

    /// <summary>
    /// 处理配置。
    /// </summary>
    public class ProcessingConfig
    {
        /// <summary>
        /// 是否备份原始文件。
        /// </summary>
        public bool BackupOriginalFiles { get; set; } = true;

        /// <summary>
        /// 备份文件后缀。
        /// </summary>
        public string BackupSuffix { get; set; } = ".bak";

        /// <summary>
        /// 是否处理文件名。
        /// </summary>
        public bool ProcessFileNames { get; set; } = true;

        /// <summary>
        /// 是否处理目录名。
        /// </summary>
        public bool ProcessDirectoryNames { get; set; } = true;

        /// <summary>
        /// 是否跳过已处理的文件。
        /// </summary>
        public bool SkipProcessedFiles { get; set; } = false;

        /// <summary>
        /// 最大并发数。
        /// </summary>
        public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 处理超时时间（秒）。
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 是否详细日志。
        /// </summary>
        public bool VerboseLogging { get; set; } = false;
    }
}