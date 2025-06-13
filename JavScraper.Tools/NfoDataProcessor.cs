using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JavScraper.Tools.Configuration;
using JavScraper.Tools.Models;
using JavScraper.Tools.Services;
using JavScraper.Tools.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using JavScraper.Domain;
using JavScraper.Scrapers;

namespace JavScraper.Tools
{
    /// <summary>
    /// NFO 数据处理器，重构后的主要入口类。
    /// 提供 NFO 文件的标签修复和数据更新功能。
    /// </summary>
    public class NfoDataProcessor
    {
        private readonly ILogger<NfoDataProcessor> _logger;
        private readonly NfoDataService _nfoDataService;
        private readonly ProcessingConfig _config;
        private readonly IServiceProvider _serviceProvider;

        public NfoDataProcessor(
            ILogger<NfoDataProcessor> logger,
            NfoDataService nfoDataService,
            ProcessingConfig config,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nfoDataService = nfoDataService ?? throw new ArgumentNullException(nameof(nfoDataService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 修复单个 NFO 文件的标签。
        /// </summary>
        /// <param name="nfoFilePath">NFO 文件路径</param>
        /// <returns>处理结果</returns>
        public async Task<NfoProcessResult> FixNfoTagsAsync(string nfoFilePath)
        {
            try
            {
                _logger.LogInformation("开始修复 NFO 标签: {FilePath}", nfoFilePath);

                if (string.IsNullOrWhiteSpace(nfoFilePath))
                {
                    throw new ArgumentException("NFO 文件路径不能为空", nameof(nfoFilePath));
                }

                if (!File.Exists(nfoFilePath))
                {
                    throw new FileNotFoundException($"NFO 文件不存在: {nfoFilePath}");
                }

                // 检查是否跳过已处理的文件
                if (_config.SkipProcessedFiles && NfoFileUtilities.IsNfoFileProcessed(nfoFilePath))
                {
                    _logger.LogInformation("跳过已处理的文件: {FilePath}", nfoFilePath);
                    return new NfoProcessResult
                    {
                        FilePath = nfoFilePath,
                        IsSuccess = true,
                        ErrorMessage = "文件已被处理，跳过"
                    };
                }

                var result = await _nfoDataService.FixNfoTagsAsync(nfoFilePath);
                result.EndTime = DateTime.Now;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("NFO 标签修复完成: {FilePath}, 耗时: {Duration}ms", 
                        nfoFilePath, result.Duration?.TotalMilliseconds ?? 0);
                }
                else
                {
                    _logger.LogWarning("NFO 标签修复失败: {FilePath}, 错误: {Error}", 
                        nfoFilePath, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修复 NFO 标签时发生异常: {FilePath}", nfoFilePath);
                return new NfoProcessResult
                {
                    FilePath = nfoFilePath,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 更新单个 NFO 文件的数据。
        /// </summary>
        /// <param name="nfoFilePath">NFO 文件路径</param>
        /// <returns>处理结果</returns>
        public async Task<NfoProcessResult> FixNfoDataAsync(string nfoFilePath)
        {
            try
            {
                _logger.LogInformation("开始更新 NFO 数据: {FilePath}", nfoFilePath);

                if (string.IsNullOrWhiteSpace(nfoFilePath))
                {
                    throw new ArgumentException("NFO 文件路径不能为空", nameof(nfoFilePath));
                }

                if (!File.Exists(nfoFilePath))
                {
                    throw new FileNotFoundException($"NFO 文件不存在: {nfoFilePath}");
                }

                // 检查是否跳过已处理的文件
                if (_config.SkipProcessedFiles && NfoFileUtilities.IsNfoFileProcessed(nfoFilePath))
                {
                    _logger.LogInformation("跳过已处理的文件: {FilePath}", nfoFilePath);
                    return new NfoProcessResult
                    {
                        FilePath = nfoFilePath,
                        IsSuccess = true,
                        ErrorMessage = "文件已被处理，跳过"
                    };
                }

                var result = await _nfoDataService.FixNfoDataAsync(nfoFilePath);
                result.EndTime = DateTime.Now;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("NFO 数据更新完成: {FilePath}, 耗时: {Duration}ms", 
                        nfoFilePath, result.Duration?.TotalMilliseconds ?? 0);
                }
                else
                {
                    _logger.LogWarning("NFO 数据更新失败: {FilePath}, 错误: {Error}", 
                        nfoFilePath, result.ErrorMessage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 NFO 数据时发生异常: {FilePath}", nfoFilePath);
                return new NfoProcessResult
                {
                    FilePath = nfoFilePath,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    EndTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// 批量修复 NFO 文件标签。
        /// </summary>
        /// <param name="rootPath">根目录路径</param>
        /// <returns>批量处理结果</returns>
        public async Task<BatchProcessResult> FixNfoTagsBatchAsync(string rootPath)
        {
            var batchResult = new BatchProcessResult();

            try
            {
                _logger.LogInformation("开始批量修复 NFO 标签: {RootPath}", rootPath);

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new ArgumentException("根目录路径不能为空", nameof(rootPath));
                }

                if (!Directory.Exists(rootPath))
                {
                    throw new DirectoryNotFoundException($"根目录不存在: {rootPath}");
                }

                // 获取所有 NFO 文件分组
                var fileGroups = NfoFileUtilities.GetNfoFilesGroupedByDirectory(rootPath);
                _logger.LogInformation("找到 {GroupCount} 个目录，共 {FileCount} 个 NFO 文件", 
                    fileGroups.Count, fileGroups.Sum(g => g.FileCount));

                // 处理每个分组
                var semaphore = new System.Threading.SemaphoreSlim(_config.MaxConcurrency);
                var tasks = fileGroups.Select(async group =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await ProcessFileGroup(group, FixNfoTagsAsync);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                var groupResults = await Task.WhenAll(tasks);
                batchResult.Results.AddRange(groupResults.SelectMany(r => r));
                batchResult.EndTime = DateTime.Now;

                _logger.LogInformation("批量修复完成: 成功 {Success}/{Total}, 耗时: {Duration}ms",
                    batchResult.SuccessCount, batchResult.TotalCount, batchResult.TotalDuration?.TotalMilliseconds ?? 0);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量修复 NFO 标签时发生异常: {RootPath}", rootPath);
                batchResult.EndTime = DateTime.Now;
                return batchResult;
            }
        }

        /// <summary>
        /// 批量更新 NFO 文件数据。
        /// </summary>
        /// <param name="rootPath">根目录路径</param>
        /// <returns>批量处理结果</returns>
        public async Task<BatchProcessResult> FixNfoDataBatchAsync(string rootPath)
        {
            var batchResult = new BatchProcessResult();

            try
            {
                _logger.LogInformation("开始批量更新 NFO 数据: {RootPath}", rootPath);

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new ArgumentException("根目录路径不能为空", nameof(rootPath));
                }

                if (!Directory.Exists(rootPath))
                {
                    throw new DirectoryNotFoundException($"根目录不存在: {rootPath}");
                }

                // 获取所有 NFO 文件分组
                var fileGroups = NfoFileUtilities.GetNfoFilesGroupedByDirectory(rootPath);
                _logger.LogInformation("找到 {GroupCount} 个目录，共 {FileCount} 个 NFO 文件", 
                    fileGroups.Count, fileGroups.Sum(g => g.FileCount));

                // 处理每个分组
                var semaphore = new System.Threading.SemaphoreSlim(_config.MaxConcurrency);
                var tasks = fileGroups.Select(async group =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await ProcessFileGroup(group, FixNfoDataAsync);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                var groupResults = await Task.WhenAll(tasks);
                batchResult.Results.AddRange(groupResults.SelectMany(r => r));
                batchResult.EndTime = DateTime.Now;

                _logger.LogInformation("批量更新完成: 成功 {Success}/{Total}, 耗时: {Duration}ms",
                    batchResult.SuccessCount, batchResult.TotalCount, batchResult.TotalDuration?.TotalMilliseconds ?? 0);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新 NFO 数据时发生异常: {RootPath}", rootPath);
                batchResult.EndTime = DateTime.Now;
                return batchResult;
            }
        }

        /// <summary>
        /// 处理文件分组。
        /// </summary>
        /// <param name="group">文件分组</param>
        /// <param name="processFunc">处理函数</param>
        /// <returns>处理结果列表</returns>
        private async Task<List<NfoProcessResult>> ProcessFileGroup(
            NfoFileGroup group, 
            Func<string, Task<NfoProcessResult>> processFunc)
        {
            var results = new List<NfoProcessResult>();

            try
            {
                // 优先处理主要的 NFO 文件
                if (group.HasPrimaryNfo)
                {
                    var result = await processFunc(group.PrimaryNfoFile);
                    results.Add(result);

                    // 如果主要文件处理成功，处理其他文件
                    if (result.IsSuccess)
                    {
                        var otherFiles = group.NfoFiles
                            .Where(f => !f.Equals(group.PrimaryNfoFile, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var file in otherFiles)
                        {
                            var otherResult = await processFunc(file);
                            results.Add(otherResult);
                        }
                    }
                }
                else
                {
                    // 没有主要文件，处理所有文件
                    foreach (var file in group.NfoFiles)
                    {
                        var result = await processFunc(file);
                        results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件分组时发生异常: {DirectoryPath}", group.DirectoryPath);
                
                // 为未处理的文件添加错误结果
                foreach (var file in group.NfoFiles.Where(f => !results.Any(r => r.FilePath == f)))
                {
                    results.Add(new NfoProcessResult
                    {
                        FilePath = file,
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                        EndTime = DateTime.Now
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// 获取处理统计信息。
        /// </summary>
        /// <param name="batchResult">批量处理结果</param>
        /// <returns>统计信息字符串</returns>
        public string GetProcessingStatistics(BatchProcessResult batchResult)
        {
            if (batchResult == null)
            {
                return "无处理结果";
            }

            var stats = new System.Text.StringBuilder();
            stats.AppendLine($"处理统计信息:");
            stats.AppendLine($"  总文件数: {batchResult.TotalCount}");
            stats.AppendLine($"  成功数: {batchResult.SuccessCount}");
            stats.AppendLine($"  失败数: {batchResult.FailureCount}");
            stats.AppendLine($"  成功率: {batchResult.SuccessRate:P2}");
            stats.AppendLine($"  总耗时: {batchResult.TotalDuration?.TotalSeconds:F2} 秒");
            stats.AppendLine($"  平均耗时: {batchResult.AverageDuration?.TotalMilliseconds:F2} 毫秒/文件");

            // 错误统计
            var errorGroups = batchResult.Results
                .Where(r => !r.IsSuccess)
                .GroupBy(r => r.ErrorMessage)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            if (errorGroups.Any())
            {
                stats.AppendLine($"\n主要错误类型:");
                foreach (var errorGroup in errorGroups)
                {
                    stats.AppendLine($"  {errorGroup.Key}: {errorGroup.Count()} 次");
                }
            }

            return stats.ToString();
        }
    }

    /// <summary>
    /// NFO 数据处理器工厂。
    /// </summary>
    public static class NfoDataProcessorFactory
    {
        /// <summary>
        /// 创建 NFO 数据处理器实例。
        /// </summary>
        /// <param name="config">处理配置</param>
        /// <returns>NFO 数据处理器实例</returns>
        public static NfoDataProcessor Create(ProcessingConfig? config = null)
        {
            var services = new ServiceCollection();
            
            // 注册配置
            var processingConfig = config ?? new ProcessingConfig();
            services.AddSingleton(processingConfig);

            // 注册日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                if (processingConfig.VerboseLogging)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
            });

            // 注册服务
            services.AddTransient<TagProcessingService>();
            services.AddTransient<FileOperationService>();
            services.AddTransient<NfoDataService>();
            services.AddTransient<NfoDataProcessor>();

            // 注册外部依赖（需要根据实际情况调整）
            // NfoFileManager 不需要注册，因为它是按需创建的
            // 如果需要 JAV 爬虫服务，可以注册具体的爬虫实现
            // services.AddTransient<IJavScraper, YourScraperImplementation>();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<NfoDataProcessor>();
        }
    }
}