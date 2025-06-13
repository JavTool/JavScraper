using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using JavScraper.Tools.Tools;
using JavScraper.Tools.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// 元数据服务类，负责处理JAV视频的元数据修正、标签处理、封面下载等功能
    /// </summary>
    public class MetadataService
    {
        #region Constants
        /// <summary>备份文件扩展名</summary>
        private const string BACKUP_EXTENSION = ".bak";
        /// <summary>相似度阈值</summary>
        private const double SIMILARITY_THRESHOLD = 0.5;
        /// <summary>中文字幕标签</summary>
        private const string CHINESE_SUBTITLE_TAG = "中文字幕";
        /// <summary>无码破解标签</summary>
        private const string UNCENSORED_TAG = "無碼破解";
        /// <summary>中字无码前缀</summary>
        private const string CHINESE_UNCENSORED_PREFIX = "[中字无码] ";
        /// <summary>中字前缀</summary>
        private const string CHINESE_PREFIX = "[中字] ";
        /// <summary>无码前缀</summary>
        private const string UNCENSORED_PREFIX = "[无码] ";
        /// <summary>无码后缀</summary>
        private const string UC_SUFFIX = "-UC";
        /// <summary>中字后缀</summary>
        private const string C_SUFFIX = "-C";
        /// <summary>无码后缀</summary>
        private const string U_SUFFIX = "-U";
        #endregion

        #region Fields
        /// <summary>日志工厂</summary>
        private readonly ILoggerFactory _loggerFactory;
        /// <summary>日志记录器</summary>
        private readonly ILogger<MetadataService> _logger;
        /// <summary>样本图片配置</summary>
        private readonly SampleImageConfig _sampleImageConfig;
        
        /// <summary>标签映射字典，用于将简化标签映射为完整标签</summary>
        private static readonly Dictionary<string, string> TagMappings = new()
        {
            { "中字", CHINESE_SUBTITLE_TAG }
        };
        #endregion

        /// <summary>
        /// 初始化元数据服务实例
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        public MetadataService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<MetadataService>();
            _sampleImageConfig = SampleImageConfig.LoadFromFile();
        }

        /// <summary>
    /// 修正指定路径下所有NFO文件的元数据，并执行标签修复
    /// </summary>
    /// <param name="path">要处理的目录路径</param>
    /// <returns>异步任务</returns>
    public async Task FixMetadataAsync(string path)
    {
        Dictionary<string, string> javFiles = new Dictionary<string, string>();
        DirectoryHelper.GetAllFiles(path, javFiles);

        // 第一步：处理所有NFO文件的基本元数据
        foreach (var javFile in javFiles)
        {
            var fileExt = Path.GetExtension(javFile.Key);
            if (fileExt.ToLower().Contains(".nfo") && !javFile.Key.Contains(".bak.nfo"))
            {
                await ProcessNfoFile(javFile.Key);
            }
        }
        
        // 第二步：执行标签修复和重命名操作
        await FixNfoTagsAsync(path);
        }

        /// <summary>
        /// 修正指定路径下NFO文件的标签
        /// </summary>
        /// <param name="path">要处理的目录路径</param>
        /// <returns>异步任务</returns>
        public async Task FixNfoTagsAsync(string path)
        {
            await NfoDataFIx.FixNfoTagsAsync(_loggerFactory, path);
        }

        /// <summary>
        /// 快速裁切封面图片，将图片裁切为海报格式
        /// </summary>
        /// <param name="path">图片文件路径</param>
        /// <returns>异步任务</returns>
        public async Task QuickCutCover(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(path);
            var fileExt = Path.GetExtension(fileInfo.FullName).ToLower();

            if (fileExt.Contains(".jpg") || fileExt.Contains(".jpeg"))
            {
                string destName = $"{Path.GetFileNameWithoutExtension(fileInfo.FullName)}-poster";
                ImageUtils.CutImage(fileInfo.FullName, fileInfo.DirectoryName, "poster");
                Console.WriteLine("裁切完成！");
            }
        }

        /// <summary>
        /// 处理单个NFO文件
        /// </summary>
        /// <param name="filePath">NFO文件路径</param>
        /// <returns>异步任务</returns>
        private async Task ProcessNfoFile(string filePath)
        {
            try
            {
                if (!ValidateNfoFile(filePath))
                    return;

                await ProcessValidNfoFile(filePath);
            }
            catch (Exception ex)
            {
                LogAndDisplayError($"处理文件 {filePath} 时发生错误", ex);
            }
        }

        /// <summary>
        /// 验证 NFO 文件是否可以处理
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否可以处理</returns>
        private bool ValidateNfoFile(string filePath)
        {
            var attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                LogAndDisplay("当前 nfo 文件已隐藏，跳过！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 处理有效的 NFO 文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private async Task ProcessValidNfoFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            
            // 备份 nfo 文件
            CreateBackupIfNotExists(fileInfo);

            var nfoManager = new NfoFileManager(filePath);
            if (string.IsNullOrEmpty(nfoManager.ToString()))
            {
                Console.WriteLine($"获取 「{filePath}」 番号异常，跳过执行。");
                return;
            }

            var metadata = await GetMetadataFromNfo(nfoManager);
            if (metadata == null)
                return;

            await ProcessMetadata(metadata, nfoManager, fileInfo);
            PrintUpdatedMetadata(metadata);
        }

        /// <summary>
        /// 创建备份文件（如果不存在）
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        private void CreateBackupIfNotExists(FileInfo fileInfo)
        {
            var destFileName = Path.Combine(fileInfo.DirectoryName,
                $"{Path.GetFileNameWithoutExtension(fileInfo.FullName)}{BACKUP_EXTENSION}{fileInfo.Extension}");
            
            if (!File.Exists(destFileName))
            {
                fileInfo.CopyTo(destFileName);
            }
        }

        /// <summary>
        /// 从NFO文件获取元数据信息
        /// </summary>
        /// <param name="nfoManager">NFO文件管理器</param>
        /// <returns>元数据信息，如果获取失败则返回null</returns>
        private async Task<MetadataInfo> GetMetadataFromNfo(NfoFileManager nfoManager)
        {
            var title = nfoManager.GetTitle();
            var sortTitle = nfoManager.GetSortTitle();
            var originalTitle = nfoManager.GetOriginalTitle();
            var genres = nfoManager.GetGenres();
            var tags = nfoManager.GetTags();

            var javId = JavRecognizer.Parse(sortTitle) ?? 
                       JavRecognizer.Parse(originalTitle) ?? 
                       JavRecognizer.Parse(title);

            if (javId == null)
            {
                _logger.LogInformation($"获取番号异常，跳过执行");
                Console.WriteLine($"获取番号异常，跳过执行。");
                return null;
            }

            Jav123Scraper jav123 = new Jav123Scraper(_loggerFactory);
            var javVideo = await jav123.SearchAndParseJavVideo(javId.Id) ?? 
                          await new JavCaptain(_loggerFactory).ParsePage($"https://javcaptain.com/zh/{javId}");

            if (javVideo == null)
            {
                Console.WriteLine($"获取番号信息异常，跳过执行。");
                return null;
            }

            return new MetadataInfo
            {
                JavId = javId,
                Title = title,
                OriginalTitle = originalTitle,
                Genres = genres,
                Tags = tags,
                JavVideo = javVideo
            };
        }

        /// <summary>
    /// 处理元数据，包括标题、标签、封面图片等
    /// </summary>
    /// <param name="metadata">元数据信息</param>
    /// <param name="nfoManager">NFO文件管理器</param>
    /// <param name="fileInfo">文件信息</param>
    /// <returns>异步任务</returns>
    private async Task ProcessMetadata(MetadataInfo metadata, NfoFileManager nfoManager, FileInfo fileInfo)
    {
        var videoInfo = CreateVideoInfo(metadata, nfoManager, fileInfo);
        
        // 处理标题和标签
        await ProcessTitleAndTags(metadata, videoInfo);

        // 处理封面图片
        await ProcessCoverImage(metadata, videoInfo);

        // 保存更新后的 nfo 文件
        // 注意：此处只进行基本的元数据保存，后续 FixNfoTagsAsync 会进一步处理标签和文件名
        nfoManager.SaveMetadata(videoInfo.VideoTitle, videoInfo.VideoOriginalTitle, videoInfo.VideoSortTitle, videoInfo.VideoId, "", 
            videoInfo.VideoActors, metadata.Genres, metadata.Tags);
        }

        /// <summary>
        /// 创建视频信息对象
        /// </summary>
        /// <param name="metadata">元数据信息</param>
        /// <param name="nfoManager">NFO 管理器</param>
        /// <param name="fileInfo">文件信息</param>
        /// <returns>视频信息</returns>
        private VideoInfo CreateVideoInfo(MetadataInfo metadata, NfoFileManager nfoManager, FileInfo fileInfo)
        {
            var videoId = metadata.JavId.Id.ToUpper();
            var videoTitle = metadata.JavVideo.Title.Trim();
            var videoOriginalTitle = videoTitle;
            var videoSortTitle = videoId;
            var videoActors = metadata.JavVideo.Actors ?? new List<string>();
            
            var titleInfo = ProcessVideoTitle(videoTitle, videoId);
            var genres = ProcessVideoGenres(metadata.JavVideo.Genres, titleInfo.HasChineseSubtitle, titleInfo.HasUncensored);
            
            metadata.Genres = genres;

            return new VideoInfo
            {
                VideoId = videoId,
                VideoTitle = titleInfo.ProcessedTitle,
                VideoOriginalTitle = videoOriginalTitle,
                VideoSortTitle = titleInfo.SortTitle,
                VideoActors = videoActors,
                DirectoryName = fileInfo.DirectoryName,
                BaseFileName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                HasChineseSubtitle = titleInfo.HasChineseSubtitle,
                HasUncensored = titleInfo.HasUncensored
            };
        }

        /// <summary>
    /// 处理视频标题
    /// </summary>
    /// <param name="originalTitle">原始标题</param>
    /// <param name="videoId">视频ID</param>
    /// <returns>处理后的标题信息</returns>
    private ProcessedTitleInfo ProcessVideoTitle(string originalTitle, string videoId)
    {
        // 清理标题中的特殊字符
        originalTitle = originalTitle.Replace("無碼 ", "").Replace("無修正 カリビアンコム ", "").Trim();
        
        // 检测中文字幕和无码特征
        var hasChineseSubtitle = originalTitle.Contains("中字") || originalTitle.Contains("中文");
        var hasUncensored = videoId.EndsWith(UC_SUFFIX) || 
                           originalTitle.Contains("無碼") || 
                           originalTitle.Contains("无码") || 
                           Directory.GetParent(Path.GetDirectoryName(videoId))?.Name.Contains("Un Censored") == true;
        
        // 检查外挂字幕
        string[] subtitleExtensions = { "*.srt", "*.ssa", "*.ass", "*.vtt", "*.sub" };
        bool hasSubtitles = subtitleExtensions.Any(ext =>
            Directory.GetFiles(Path.GetDirectoryName(videoId), ext, SearchOption.AllDirectories).Length > 0);
        
        if (hasSubtitles)
        {
            hasChineseSubtitle = true;
        }
        
        var processedTitle = originalTitle;
        var sortTitle = videoId;

        // 根据标签组合设置标题前缀
        if (hasChineseSubtitle && hasUncensored)
        {
            // 如果标题已经包含前缀，则替换而不是添加
            if (processedTitle.Contains("[中字]"))
            {
                processedTitle = processedTitle.Replace("[中字]", CHINESE_UNCENSORED_PREFIX);
            }
            else if (processedTitle.Contains("[无码]"))
            {
                processedTitle = processedTitle.Replace("[无码]", CHINESE_UNCENSORED_PREFIX);
            }
            else if (!processedTitle.Contains(CHINESE_UNCENSORED_PREFIX))
            {
                processedTitle = $"{CHINESE_UNCENSORED_PREFIX}{originalTitle}";
            }
            sortTitle = $"{videoId}{UC_SUFFIX}";
        }
        else if (hasChineseSubtitle)
        {
            if (!processedTitle.Contains(CHINESE_PREFIX))
            {
                processedTitle = $"{CHINESE_PREFIX}{originalTitle}";
            }
            sortTitle = $"{videoId}{C_SUFFIX}";
        }
        else if (hasUncensored)
        {
            if (!processedTitle.Contains(UNCENSORED_PREFIX))
            {
                processedTitle = $"{UNCENSORED_PREFIX}{originalTitle}";
            }
            sortTitle = $"{videoId}{U_SUFFIX}";
        }

        return new ProcessedTitleInfo
        {
            ProcessedTitle = processedTitle,
            SortTitle = sortTitle,
            HasChineseSubtitle = hasChineseSubtitle,
            HasUncensored = hasUncensored
        };
        }

        /// <summary>
        /// 处理视频类型
        /// </summary>
        /// <param name="originalGenres">原始类型列表</param>
        /// <param name="hasChineseSubtitle">是否有中文字幕</param>
        /// <param name="hasUncensored">是否为无码</param>
        /// <returns>处理后的类型列表</returns>
        private List<string> ProcessVideoGenres(List<string> originalGenres, bool hasChineseSubtitle, bool hasUncensored)
        {
            var genres = originalGenres?.ToList() ?? new List<string>();
            
            if (hasChineseSubtitle && !genres.Contains(CHINESE_SUBTITLE_TAG))
            {
                genres.Add(CHINESE_SUBTITLE_TAG);
            }
            
            if (hasUncensored && !genres.Contains(UNCENSORED_TAG))
            {
                genres.Add(UNCENSORED_TAG);
            }
            
            return genres;
        }

        /// <summary>
        /// 处理标题和标签
        /// </summary>
        /// <param name="metadata">元数据</param>
        /// <param name="videoInfo">视频信息</param>
        private async Task ProcessTitleAndTags(MetadataInfo metadata, VideoInfo videoInfo)
        {
            // 处理标签
            ProcessVideoTags(metadata, videoInfo);
            
            // 下载样本图片
            await DownloadSampleImages(metadata.JavVideo.Samples, videoInfo.DirectoryName);
        }

        /// <summary>
    /// 处理视频标签
    /// </summary>
    /// <param name="metadata">元数据</param>
    /// <param name="videoInfo">视频信息</param>
    private void ProcessVideoTags(MetadataInfo metadata, VideoInfo videoInfo)
    {
        var tags = new List<string>(metadata.Tags ?? new List<string>());
        
        // 添加中文字幕标签
        if (videoInfo.HasChineseSubtitle && !tags.Contains(CHINESE_SUBTITLE_TAG))
        {
            tags.Add(CHINESE_SUBTITLE_TAG);
        }
        
        // 添加无码标签
        if (videoInfo.HasUncensored && !tags.Contains(UNCENSORED_TAG))
        {
            tags.Add(UNCENSORED_TAG);
        }
        
        // 检查外挂字幕
        string[] subtitleExtensions = { "*.srt", "*.ssa", "*.ass", "*.vtt", "*.sub" };
        bool hasSubtitles = subtitleExtensions.Any(ext =>
            Directory.GetFiles(videoInfo.DirectoryName, ext, SearchOption.AllDirectories).Length > 0);

        if (hasSubtitles && !tags.Contains("外挂字幕"))
        {
            tags.Add("外挂字幕");
        }
        
        // 应用标签映射
        ApplyTagMappings(tags);
        
        metadata.Tags = tags;
    }

        /// <summary>
        /// 应用标签映射
        /// </summary>
        /// <param name="tags">标签列表</param>
        private void ApplyTagMappings(List<string> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (TagMappings.TryGetValue(tags[i], out var mappedTag))
                {
                    tags[i] = mappedTag;
                }
            }
        }

        /// <summary>
        /// 处理封面图片
        /// </summary>
        /// <param name="metadata">元数据</param>
        /// <param name="videoInfo">视频信息</param>
        private async Task ProcessCoverImage(MetadataInfo metadata, VideoInfo videoInfo)
        {
            if (string.IsNullOrEmpty(metadata.JavVideo.Cover))
                return;

            var coverPath = Path.Combine(videoInfo.DirectoryName, $"{videoInfo.BaseFileName}-poster.jpg");
            
            if (File.Exists(coverPath))
                return;

            try
            {
                await Downloader.DownloadJpegAsync(metadata.JavVideo.Cover, videoInfo.DirectoryName, $"{videoInfo.BaseFileName}-poster");
                LogAndDisplay($"下载封面图片: {coverPath}");
            }
            catch (Exception ex)
            {
                LogAndDisplayError("下载封面图片失败", ex);
            }
        }





        /// <summary>
        /// 下载样本图片
        /// </summary>
        /// <param name="samples">样本图片URL列表</param>
        /// <param name="directoryName">目录名称</param>
        private async Task DownloadSampleImages(List<string> samples, string directoryName)
        {
            if (samples == null || !samples.Any())
                return;

            var targetDir = GetSampleDownloadDirectory(directoryName);
            var downloadTasks = CreateSampleDownloadTasks(samples, targetDir);
            
            await Task.WhenAll(downloadTasks);
        }

        /// <summary>
        /// 获取样本图片下载目录
        /// </summary>
        /// <param name="directoryName">基础目录名称</param>
        /// <returns>样本图片下载目录路径</returns>
        private string GetSampleDownloadDirectory(string directoryName)
        {
            string targetDir;
            
            if (_sampleImageConfig.UseSeparateDirectory)
            {
                // 使用单独目录
                targetDir = Path.Combine(directoryName, _sampleImageConfig.DirectoryName);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
            }
            else
            {
                // 直接下载到当前目录
                targetDir = directoryName;
            }
            
            return targetDir;
        }

        /// <summary>
        /// 创建样本图片下载任务
        /// </summary>
        /// <param name="samples">样本图片URL列表</param>
        /// <param name="targetDir">目标目录</param>
        /// <returns>下载任务列表</returns>
        private List<Task> CreateSampleDownloadTasks(List<string> samples, string targetDir)
        {
            return samples.Select(async (sample, index) =>
            {
                try
                {
                    string fileName;
                    
                    if (_sampleImageConfig.UseSeparateDirectory)
                    {
                        // 使用单独目录时，保持原有命名方式
                        fileName = $"sample-{index + 1:D2}";
                    }
                    else
                    {
                        // 直接下载到当前目录时，使用backdrop命名方式
                        fileName = $"backdrop{index + 1}";
                    }
                    
                    await Downloader.DownloadJpegAsync(sample, targetDir, fileName);
                    LogAndDisplay($"下载样本图片: {fileName}.jpg");
                }
                catch (Exception ex)
                {
                    string fileName = _sampleImageConfig.UseSeparateDirectory ? $"sample-{index + 1:D2}" : $"backdrop{index + 1}";
                    LogAndDisplayError($"下载样本图片 {fileName}.jpg 失败", ex);
                }
            }).ToList();
        }

        /// <summary>
        /// 打印更新后的元数据信息到控制台
        /// </summary>
        /// <param name="metadata">元数据信息</param>
        private void PrintUpdatedMetadata(MetadataInfo metadata)
        {
            Console.WriteLine($"-----------------修正后的元数据-----------------");
            Console.WriteLine($"videoId -> {metadata.JavId.Id.ToUpper()}");
            Console.WriteLine($"videoTitle -> {metadata.Title}");
            Console.WriteLine($"videoOriginalTitle -> {metadata.OriginalTitle}");
            Console.WriteLine($"videoSortTitle -> {metadata.JavId.Id.ToUpper()}");
            Console.WriteLine($"tags -> {String.Join(",", metadata.Tags)}");
            Console.WriteLine($"genres -> {String.Join(",", metadata.Genres)}");
            Console.WriteLine($"==============================================");
        }

        /// <summary>
        /// 元数据信息类，包含视频的基本信息和从网站抓取的详细信息
        /// </summary>
        private class MetadataInfo
        {
            /// <summary>JAV视频ID</summary>
            public JavId JavId { get; set; }
            /// <summary>标题</summary>
            public string Title { get; set; }
            /// <summary>原始标题</summary>
            public string OriginalTitle { get; set; }
            /// <summary>类型列表</summary>
            public List<string> Genres { get; set; }
            /// <summary>标签列表</summary>
            public List<string> Tags { get; set; }
            /// <summary>JAV视频详细信息</summary>
            public JavVideo JavVideo { get; set; }
        }

        /// <summary>
        /// 视频信息类，包含处理后的视频元数据
        /// </summary>
        private class VideoInfo
        {
            /// <summary>视频ID</summary>
            public string VideoId { get; set; }
            /// <summary>视频标题</summary>
            public string VideoTitle { get; set; }
            /// <summary>视频原始标题</summary>
            public string VideoOriginalTitle { get; set; }
            /// <summary>视频排序标题</summary>
            public string VideoSortTitle { get; set; }
            /// <summary>视频演员列表</summary>
            public List<string> VideoActors { get; set; }
            /// <summary>目录名称</summary>
            public string DirectoryName { get; set; }
            /// <summary>基础文件名</summary>
            public string BaseFileName { get; set; }
            /// <summary>是否有中文字幕</summary>
            public bool HasChineseSubtitle { get; set; }
            /// <summary>是否为无码</summary>
            public bool HasUncensored { get; set; }
        }

        /// <summary>
        /// 处理后的标题信息类
        /// </summary>
        private class ProcessedTitleInfo
        {
            /// <summary>处理后的标题</summary>
            public string ProcessedTitle { get; set; }
            /// <summary>排序标题</summary>
            public string SortTitle { get; set; }
            /// <summary>是否有中文字幕</summary>
            public bool HasChineseSubtitle { get; set; }
            /// <summary>是否为无码</summary>
            public bool HasUncensored { get; set; }
        }

        /// <summary>
        /// 记录日志并显示信息到控制台
        /// </summary>
        /// <param name="message">要记录的消息</param>
        private void LogAndDisplay(string message)
        {
            _logger.LogInformation(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// 记录错误日志并显示错误信息到控制台
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="ex">异常对象</param>
        private void LogAndDisplayError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
            Console.WriteLine($"{message}: {ex.Message}");
        }
    }
}