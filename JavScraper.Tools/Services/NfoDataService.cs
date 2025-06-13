using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JavScraper.Tools.Configuration;
using JavScraper.Tools.Models;
using JavScraper.Tools.Utilities;
using JavScraper.Tools.Tools;
using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using JavScraper.Scrapers;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// NFO 数据处理服务，负责 NFO 文件的标签修复和数据更新。
    /// </summary>
    public class NfoDataService
    {
        private readonly ILogger<NfoDataService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private NfoFileManager _nfoManager;
        private readonly FileOperationService _fileOperationService;
        private readonly TagProcessingService _tagProcessingService;

        public NfoDataService(
            ILogger<NfoDataService> logger,
            ILoggerFactory loggerFactory,
            FileOperationService fileOperationService,
            TagProcessingService tagProcessingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _fileOperationService = fileOperationService ?? throw new ArgumentNullException(nameof(fileOperationService));
            _tagProcessingService = tagProcessingService ?? throw new ArgumentNullException(nameof(tagProcessingService));
        }

        /// <summary>
        /// 修复 NFO 文件的标签数据。
        /// </summary>
        /// <param name="nfoFilePath">NFO 文件路径</param>
        /// <returns>处理结果</returns>
        public async Task<NfoProcessResult> FixNfoTagsAsync(string nfoFilePath)
        {
            try
            {
                _logger.LogInformation("开始处理 NFO 文件: {FilePath}", nfoFilePath);

                var result = new NfoProcessResult { FilePath = nfoFilePath };

                // 读取 NFO 文件
                _nfoManager = new NfoFileManager(nfoFilePath);
                var nfoVideoInfo = _nfoManager.GetJavVideo();
                if (nfoVideoInfo == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "无法读取 NFO 文件";
                    return result;
                }

                // 解析 JAV ID
                var javId = ExtractJavId(nfoVideoInfo, nfoFilePath);
                if (string.IsNullOrEmpty(javId))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "无法解析 JAV ID";
                    return result;
                }

                result.JavId = javId;

                // 处理标签和类型
                var processedTags = _tagProcessingService.ProcessTags(nfoVideoInfo.Genres);
                nfoVideoInfo.Genres = processedTags.ProcessedGenres;
                nfoVideoInfo.Tags = processedTags.ProcessedTags;

                // 检查字幕
                var hasSubtitles = CheckForSubtitles(Path.GetDirectoryName(nfoFilePath));
                result.HasSubtitles = hasSubtitles;

                // 处理标题
                var videoInfo = new VideoInfo
                {
                    Title = nfoVideoInfo.Title,
                    OriginalTitle = nfoVideoInfo.OriginalTitle,
                    Number = nfoVideoInfo.Number
                };
                var titleInfo = ProcessTitle(videoInfo, hasSubtitles, Path.GetDirectoryName(nfoFilePath));
                nfoVideoInfo.Title = titleInfo.Title;
                nfoVideoInfo.OriginalTitle = titleInfo.OriginalTitle;

                // 保存修改后的 NFO
                _nfoManager.SaveMetadata(nfoVideoInfo.Title, nfoVideoInfo.OriginalTitle, nfoVideoInfo.SortTitle,
                nfoVideoInfo.Plot, nfoVideoInfo.Number, nfoVideoInfo.Actors,
                nfoVideoInfo.Genres, nfoVideoInfo.Tags, nfoVideoInfo.GetYear(), nfoVideoInfo.Date);

                // 复制到其他 NFO 文件
                await CopyToOtherNfoFiles(nfoFilePath, nfoVideoInfo);

                // 处理目录和文件名
                await ProcessDirectoryAndFileNames(nfoFilePath, hasSubtitles);

                result.IsSuccess = true;
                result.ProcessedTitle = nfoVideoInfo.Title;
                result.ProcessedGenres = nfoVideoInfo.Genres;
                result.ProcessedTags = nfoVideoInfo.Tags;

                _logger.LogInformation("NFO 文件处理完成: {FilePath}, JAV ID: {JavId}", nfoFilePath, javId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理 NFO 文件时发生错误: {FilePath}", nfoFilePath);
                return new NfoProcessResult
                {
                    FilePath = nfoFilePath,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 更新 NFO 文件数据。
        /// </summary>
        /// <param name="nfoFilePath">NFO 文件路径</param>
        /// <returns>处理结果</returns>
        public async Task<NfoProcessResult> FixNfoDataAsync(string nfoFilePath)
        {
            try
            {
                _logger.LogInformation("开始更新 NFO 数据: {FilePath}", nfoFilePath);

                var result = new NfoProcessResult { FilePath = nfoFilePath };

                // 检查文件属性
                var fileInfo = new FileInfo(nfoFilePath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }

                // 备份原始文件
                await _fileOperationService.BackupFileAsync(nfoFilePath);

                // 读取 NFO 内容
                var nfoContent = await File.ReadAllTextAsync(nfoFilePath);
                var javId = ExtractJavIdFromContent(nfoContent, nfoFilePath);

                if (string.IsNullOrEmpty(javId))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "无法解析 JAV ID";
                    return result;
                }

                result.JavId = javId;

                // 从网络获取视频信息
                JavVideo videoInfo = null;
                var videoInfoFetcher = new VideoInfoFetcher(_loggerFactory);
                videoInfo = await videoInfoFetcher.TryGetMetadataAsync(javId);
                if (videoInfo == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "无法获取视频信息";
                    return result;
                }

                // 获取完整的 JavVideo 对象以获取完整的元数据
                var javVideoForProcessing = await videoInfoFetcher.TryGetMetadataAsync(javId);
                
                // 创建标准视频元数据
                var standardMetadata = CreateStandardMetadata(videoInfo, javId);

                // 处理标签（使用 JavVideo 对象的 Genres）
                var processedTags = _tagProcessingService.ProcessTags(javVideoForProcessing?.Genres);
                var processedGenres = processedTags.ProcessedGenres;
                var processedTagsList = processedTags.ProcessedTags;

                // 检查字幕
                var hasSubtitles = CheckForSubtitles(Path.GetDirectoryName(nfoFilePath));
                result.HasSubtitles = hasSubtitles;

                // 处理标题
                var videoInfoForTitle = new VideoInfo
                {
                    Title = standardMetadata.Title,
                    OriginalTitle = standardMetadata.OriginalTitle,
                    Number = standardMetadata.Number
                };
                var titleInfo = ProcessTitle(videoInfoForTitle, hasSubtitles, Path.GetDirectoryName(nfoFilePath));
                standardMetadata.Title = titleInfo.Title;
                standardMetadata.OriginalTitle = titleInfo.OriginalTitle;

                // 保存更新后的 NFO
                var nfoManager = new NfoFileManager(nfoFilePath);
                
                nfoManager.SaveMetadata(standardMetadata.Title, standardMetadata.OriginalTitle, standardMetadata.SortTitle,
                     javVideoForProcessing?.Plot, standardMetadata.Number, javVideoForProcessing?.Actors,
                     processedGenres, processedTagsList, javVideoForProcessing?.GetYear(), standardMetadata.Date);

                result.IsSuccess = true;
                result.ProcessedTitle = standardMetadata.Title;
                result.ProcessedGenres = processedGenres;
                result.ProcessedTags = processedTagsList;

                _logger.LogInformation("NFO 数据更新完成: {FilePath}, JAV ID: {JavId}", nfoFilePath, javId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新 NFO 数据时发生错误: {FilePath}", nfoFilePath);
                return new NfoProcessResult
                {
                    FilePath = nfoFilePath,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 提取 JAV ID。
        /// </summary>
        private string ExtractJavId(JavVideo nfoVideoInfo, string nfoFilePath)
        {
            // 从 NFO 中提取
            if (!string.IsNullOrEmpty(nfoVideoInfo.Number))
            {
                return nfoVideoInfo.Number;
            }

            // 从目录名提取
            var directoryName = Path.GetFileName(Path.GetDirectoryName(nfoFilePath));
            return JavIdExtractor.ExtractJavId(directoryName);
        }

        /// <summary>
        /// 从内容中提取 JAV ID。
        /// </summary>
        private string ExtractJavIdFromContent(string nfoContent, string nfoFilePath)
        {
            // 从 NFO 内容中提取
            var javId = JavIdExtractor.ExtractJavIdFromNfoContent(nfoContent);
            if (!string.IsNullOrEmpty(javId))
            {
                return javId;
            }

            // 从目录名提取
            var directoryName = Path.GetFileName(Path.GetDirectoryName(nfoFilePath));
            return JavIdExtractor.ExtractJavId(directoryName);
        }

        /// <summary>
        /// 检查是否有字幕文件。
        /// </summary>
        private bool CheckForSubtitles(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            return TagMappingConfig.SubtitleExtensions
                .Any(ext => Directory.GetFiles(directoryPath, ext).Length > 0);
        }

        /// <summary>
        /// 处理标题。
        /// </summary>
        private TitleProcessResult ProcessTitle(VideoInfo videoInfo, bool hasSubtitles, string directoryPath)
        {
            var result = new TitleProcessResult
            {
                Title = videoInfo.Title,
                OriginalTitle = videoInfo.OriginalTitle
            };

            // 移除特定日文术语
            result.Title = TitleCleaner.RemoveJapaneseTerms(result.Title);
            result.OriginalTitle = TitleCleaner.RemoveJapaneseTerms(result.OriginalTitle);

            // 处理无码标识
            var directoryName = Path.GetFileName(directoryPath);
            if (directoryName?.Contains("Un Censored", StringComparison.OrdinalIgnoreCase) == true)
            {
                var uncensoredTag = hasSubtitles ? "[中字无码]" : "[无码]";
                if (!result.Title.Contains(uncensoredTag))
                {
                    result.Title = $"{uncensoredTag} {result.Title}";
                }
            }

            return result;
        }

        /// <summary>
        /// 复制到其他 NFO 文件。
        /// </summary>
        private async Task CopyToOtherNfoFiles(string sourceNfoPath, JavVideo videoInfo)
        {
            try
            {
                var directory = Path.GetDirectoryName(sourceNfoPath);
                var nfoFiles = Directory.GetFiles(directory, "*.nfo")
                    .Where(f => !f.Equals(sourceNfoPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var nfoFile in nfoFiles)
                {
                    var nfoManager = new NfoFileManager(nfoFile);
                    // VideoInfo类没有Plot、Actors、Genres、Tags、GetYear等属性，需要从JavVideo获取
                    var javVideo = await new VideoInfoFetcher(_loggerFactory).TryGetMetadataAsync(videoInfo.Number);
                    nfoManager.SaveMetadata(videoInfo.Title, videoInfo.OriginalTitle, videoInfo.SortTitle,
                         javVideo?.Plot ?? "", videoInfo.Number, javVideo?.Actors ?? new List<string>(),
                         javVideo?.Genres ?? new List<string>(), javVideo?.Tags ?? new List<string>(), javVideo?.GetYear(), javVideo?.Date ?? videoInfo.Date);
                    _logger.LogDebug("已复制 NFO 数据到: {FilePath}", nfoFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "复制 NFO 文件时发生错误: {SourcePath}", sourceNfoPath);
            }
        }

        /// <summary>
        /// 处理目录和文件名。
        /// </summary>
        private async Task ProcessDirectoryAndFileNames(string nfoFilePath, bool hasSubtitles)
        {
            try
            {
                var directoryPath = Path.GetDirectoryName(nfoFilePath);
                var directoryName = Path.GetFileName(directoryPath);

                // 处理目录名
                if (directoryName.Contains("Un Censored", StringComparison.OrdinalIgnoreCase))
                {
                    var newDirectoryName = TitleCleaner.RemoveJapaneseTerms(directoryName);
                    if (newDirectoryName != directoryName)
                    {
                        await _fileOperationService.RenameDirectoryAsync(directoryPath, newDirectoryName);
                    }
                }

                // 处理文件名
                var files = Directory.GetFiles(directoryPath)
                    .Where(f => !Path.GetFileName(f).EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var extension = Path.GetExtension(file);
                    var newFileName = TitleCleaner.RemoveJapaneseTerms(fileName);

                    if (newFileName != fileName)
                    {
                        var newFilePath = Path.Combine(directoryPath, $"{newFileName}{extension}");
                        await _fileOperationService.RenameFileAsync(file, newFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "处理目录和文件名时发生错误: {FilePath}", nfoFilePath);
            }
        }

        /// <summary>
        /// 创建标准元数据。
        /// </summary>
        private VideoInfo CreateStandardMetadata(JavVideo videoInfo, string javId)
        {
            return new VideoInfo
            {
                Number = javId,
                Title = videoInfo.Title ?? string.Empty,
                OriginalTitle = videoInfo.Title ?? string.Empty,
                Date = videoInfo.Date ?? string.Empty,
                HasChineseSubtitle = false,
                IsUncensored = false
            };
        }
    }
}