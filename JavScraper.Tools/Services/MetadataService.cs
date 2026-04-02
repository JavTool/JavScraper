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
using System.IO.Compression;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// 元数据服务类，负责处理 JAV 视频的元数据修正、标签处理以及封面 / 示例图片的下载。
    /// </summary>
    /// <remarks>
    /// 该服务封装了对单个或批量 NFO 文件的处理流程：
    /// - 扫描目录并定位 NFO 文件（跳过已备份或隐藏的文件）。
    /// - 为 NFO 创建备份，解析现有元数据并尝试通过番号抓取站点数据以补全信息。
    /// - 基于抓取数据与目录特征（例如外挂字幕文件、目录名）自动识别并添加标签/类型，如“中文字幕”“无码破解”，并在标题中加入相应前缀/后缀。
    /// - 下载封面与示例图片（支持将示例图片保存到单独目录，命名规则由配置控制）。
    /// - 将更新后的基本元数据写回 NFO，随后调用外部修复器（NfoDataFIx）进行更复杂的标签修正与重命名操作。
    /// 
    /// 异常与安全策略：
    /// - 对单个文件的处理包含异常捕获，确保批量任务不中断并记录错误日志。
    /// - 在覆盖原始 NFO 之前会创建一个以 ".bak" 为扩展名的备份文件以便回退。
    /// 
    /// 依赖项：
    /// - NfoFileManager, Jav123Scraper, JavCaptain, Downloader, DirectoryHelper, ImageUtils, SampleImageConfig 等工具类。
    /// 
    /// 注意事项：
    /// - 本类负责基础的元数据补全与图片下载，关于标签的细粒度修正与统一命名由 NfoDataFIx 实现。
    /// - 若需扩展标签映射或检测规则，可通过更新 TagMappings 或 ProcessVideoTitle / ProcessVideoTags 方法。
    /// </remarks>
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
        /// <summary>示例图片配置</summary>
        private readonly SampleImageConfig _sampleImageConfig;
        /// <summary>标题清理配置</summary>
        private readonly TitleSanitizerConfig _titleSanitizerConfig;
        private readonly ScraperConfig _scraperConfig;
        private readonly ActorReplaceConfig _actorReplaceConfig;
        private readonly BackupConfig _backupConfig;

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
            _titleSanitizerConfig = TitleSanitizerConfig.LoadFromFile();
            _scraperConfig = ScraperConfig.LoadFromFile();
            _actorReplaceConfig = JavScraper.Tools.Configuration.ActorReplaceConfig.LoadFromFile();
            _backupConfig = BackupConfig.LoadFromFile();
        }

        /// <summary>
        /// 扫描并修正指定路径及其子目录下的所有 NFO 文件的元数据。
        /// </summary>
        /// <param name="path">要处理的根目录路径。方法会递归扫描该目录下的文件。</param>
        /// <returns>一个表示异步操作的任务。完成后所有可处理的 NFO 将被修改并保存，随后会调用标签修复器进行二次处理。</returns>
        /// <remarks>
        /// 流程说明：
        /// 1. 通过 <see cref="DirectoryHelper.GetAllFiles"/> 收集目标目录下的文件。
        /// 2. 对于非备份且非隐藏的 NFO 文件逐个调用 <see cref="ProcessNfoFile"/> 进行处理。
        /// 3. 在完成所有 NFO 基础修正后，调用 <see cref="FixNfoTagsAsync"/> 执行统一的标签修正与重命名操作。
        /// </remarks>
        public async Task FixMetadataAsync(string path)
        {
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            DirectoryHelper.GetAllFiles(path, javFiles);

            // 第一步：处理所有 NFO 文件的基本元数据
            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                if (fileExt.ToLower().Contains(".nfo") && !javFile.Key.Contains(".bak.nfo"))
                {
                    await ProcessNfoFile(javFile.Key);
                }
            }

            // 在完成单文件处理后，根据备份配置打包备份（NFO / 图片）为 zip
            try
            {
                var cfg = BackupConfig.LoadFromFile();
                if (cfg.BackupNfo || cfg.BackupImages)
                {
                    CreateZipBackup(path);
                }
            }
            catch (Exception ex)
            {
                LogError("执行 zip 备份时发生错误", ex);
            }

            // 第二步：执行标签修复和重命名操作
            await FixNfoTagsAsync(path);
        }

        /// <summary>
        /// 调用外部修复器对目录下的 NFO 文件执行标签/名称的批量修正。
        /// </summary>
        /// <param name="path">要处理的根目录路径。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>此方法仅将任务委派给 <c>NfoDataFIx.FixNfoTagsAsync</c>，实际规则由该类实现。</remarks>
        public async Task FixNfoTagsAsync(string path)
        {
            await NfoDataFIx.FixNfoTagsAsync(_loggerFactory, path);
        }

        /// <summary>
        /// 对指定图片执行快速裁切以生成海报（poster）版本。
        /// </summary>
        /// <param name="path">图片文件的完整路径。仅支持 JPG/JPEG 文件。</param>
        /// <returns>一个表示异步操作的任务（内部为同步实现）。</returns>
        /// <remarks>如果文件不是 JPG/JPEG 或路径为空则直接返回。</remarks>
        public static async Task QuickCutCover(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            FileInfo fileInfo = new(path);
            var fileExt = Path.GetExtension(fileInfo.FullName).ToLower();

            if (fileExt.Contains(".jpg") || fileExt.Contains(".jpeg"))
            {
                string destName = $"{Path.GetFileNameWithoutExtension(fileInfo.FullName)}-poster";
                ImageUtils.CutImage(fileInfo.FullName, fileInfo.DirectoryName, "poster");
                Console.WriteLine("裁切完成！");
            }
        }

        /// <summary>
        /// 对单个 NFO 文件执行处理，包括验证、备份与元数据修正。
        /// </summary>
        /// <param name="filePath">要处理的 NFO 文件完整路径。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>方法内部会捕获处理期间抛出的异常并记录日志，避免批量处理被单个异常中断。</remarks>
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
                LogError($"处理文件 {filePath} 时发生错误", ex);
            }
        }

        /// <summary>
        /// 验证指定 NFO 文件是否可以处理。
        /// </summary>
        /// <param name="filePath">NFO 文件完整路径。</param>
        /// <returns>如果文件可处理则返回 true；当文件为隐藏文件时返回 false。</returns>
        /// <remarks>当前实现仅检查文件隐藏属性，后续可扩展为权限与完整性检查。</remarks>
        private bool ValidateNfoFile(string filePath)
        {
            var attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                LogInformation("当前 nfo 文件已隐藏，跳过！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 对已通过验证的 NFO 文件执行具体处理：备份、解析、抓取网站数据并保存修正结果。
        /// </summary>
        /// <param name="filePath">NFO 文件完整路径。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 1. 创建 .bak 备份；
        /// 2. 使用 <see cref="NfoFileManager"/> 读取 NFO 并解析番号；
        /// 3. 调用 <see cref="GetMetadataFromNfo"/> 抓取详细信息并调用 <see cref="ProcessMetadata"/> 完成修正与保存。
        /// </remarks>
        private async Task ProcessValidNfoFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            // 备份 nfo 文件
            CreateBackupIfNotExists(fileInfo);

            var nfoManager = new NfoDocument(filePath);
            if (string.IsNullOrEmpty(nfoManager.ToString()))
            {
                Console.WriteLine($"获取 「{filePath}」 番号异常，跳过执行。");
                return;
            }

            var metadata = await GetMetadataFromNfo(nfoManager);
            if (metadata == null)
                return;

            await ProcessMetadata(metadata, nfoManager, fileInfo);
            PrintUpdatedMetadata(metadata, metadata.JavId);
        }

        /// <summary>
        /// 为指定文件创建备份（扩展名为 .bak），如果备份已存在则不覆盖。
        /// </summary>
        /// <param name="fileInfo">要备份的文件信息对象。</param>
        /// <remarks>备份文件名格式为 {原文件名}.bak{原扩展名}，例如 foo.nfo -> foo.bak.nfo。</remarks>
        private static void CreateBackupIfNotExists(FileInfo fileInfo)
        {
            // 默认备份 nfo 到 .bak.nfo，如果配置要求将由外层调用统一打包zip，这里仅在 BackupNfo 为 true 时创建单个备份文件
            var backupConfig = BackupConfig.LoadFromFile();
            if (!backupConfig.BackupNfo)
                return;

            var destFileName = Path.Combine(fileInfo.DirectoryName,
                $"{Path.GetFileNameWithoutExtension(fileInfo.FullName)}{BACKUP_EXTENSION}{fileInfo.Extension}");

            if (!File.Exists(destFileName))
            {
                fileInfo.CopyTo(destFileName);
            }
        }

        /// <summary>
        /// 将指定目录中的备份文件和/或图片打包为 zip，文件名格式：backup_yyyyMMddHHmmss.zip
        /// </summary>
        /// <param name="rootPath">要打包的根目录</param>
        private void CreateZipBackup(string rootPath)
        {
            try
            {
                var cfg = BackupConfig.LoadFromFile();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var zipName = Path.Combine(rootPath, $"backup_{timestamp}.zip");

                using var zipFile = ZipFile.Open(zipName, ZipArchiveMode.Create);

                if (cfg.BackupNfo)
                {
                    // 查找所有 .nfo 文件并添加
                    var nfoFiles = Directory.GetFiles(rootPath, "*.nfo", SearchOption.AllDirectories);

                    foreach (var f in nfoFiles)
                    {
                        try
                        {
                            var entryName = Path.GetRelativePath(rootPath, f).Replace('\\', '/');
                            using var fs = File.OpenRead(f);
                            var entry = zipFile.CreateEntry(entryName, CompressionLevel.Optimal);
                            using var es = entry.Open();
                            fs.CopyTo(es);
                        }
                        catch { }
                    }
                }

                if (cfg.BackupImages)
                {
                    var imgs = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories)
                        .Where(p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

                    foreach (var img in imgs)
                    {
                        try
                        {
                            var entryName = Path.GetRelativePath(rootPath, img).Replace('\\', '/');
                            using var fs = File.OpenRead(img);
                            var entry = zipFile.CreateEntry(entryName, CompressionLevel.Optimal);
                            using var es = entry.Open();
                            fs.CopyTo(es);
                        }
                        catch { }
                    }
                }

                LogInformation($"创建备份文件: {zipName}");
            }
            catch (Exception ex)
            {
                LogError("创建 zip 备份失败", ex);
            }
        }

        /// <summary>
        /// 从 NFO 中读取基本字段并尝试通过番号抓取站点的详细信息以补全元数据。
        /// </summary>
        /// <param name="nfoDocument">用于读取和写入 NFO 的管理器。</param>
        /// <returns>如果成功返回包含抓取到的 <see cref="MetadataInfo"/>；否则返回 null。</returns>
        /// <remarks>
        /// 抓取策略：优先使用 <see cref="Jav123Scraper"/> 根据番号查询，若失败则回退到 <see cref="JavCaptain"/> 页面解析。
        /// 若无法获取番号或站点信息，方法将返回 null 并由调用方决定如何处理。
        /// </remarks>
        private async Task<MetadataInfo> GetMetadataFromNfo(NfoDocument nfoDocument)
        {
            var title = nfoDocument.GetTitle();
            var sortTitle = nfoDocument.GetSortTitle();
            var originalTitle = nfoDocument.GetOriginalTitle();
            var genres = nfoDocument.GetGenres();
            var tags = nfoDocument.GetTags();

            var javId = JavRecognizer.Parse(sortTitle) ??
                       JavRecognizer.Parse(originalTitle) ??
                       JavRecognizer.Parse(title);

            if (javId == null)
            {
                _logger.LogInformation($"获取番号异常，跳过执行");
                Console.WriteLine($"获取番号异常，跳过执行。");
                return null;
            }

            JavVideo javVideo = null;

            // 根据配置优先级尝试不同的刮削器
            foreach (var scraperName in _scraperConfig.PreferredScrapers ?? [])
            {
                try
                {

                    if (string.Equals(scraperName, "DMM", StringComparison.OrdinalIgnoreCase))
                    {
                        // Use DMM scraper to search and parse by jav id
                        javVideo = await new DMM(_loggerFactory).SearchAndParseJavVideo(javId.Id);
                    }
                    else if (string.Equals(scraperName, "Jav123", StringComparison.OrdinalIgnoreCase))
                    {
                        javVideo = await new Jav123Scraper(_loggerFactory).SearchAndParseJavVideo(javId.Id);
                    }
                    else if (string.Equals(scraperName, "JavCaptain", StringComparison.OrdinalIgnoreCase))
                    {
                        javVideo = await new JavCaptain(_loggerFactory).ParsePage($"https://javcaptain.com/zh/{javId}");
                    }

                    if (javVideo != null)
                        break;
                }
                catch (Exception ex)
                {
                    LogError($"使用刮削器 {scraperName} 抓取番号 {javId} 时发生错误", ex);
                }
            }

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
                Plot = javVideo.Plot,
                Genres = genres,
                Tags = tags,
                JavVideo = javVideo
            };
        }

        /// <summary>
        /// 基于抓取到的元数据信息执行一系列处理操作，包括：构建 VideoInfo、处理标题和标签、下载封面与示例图片，并保存最终元数据回 NFO。
        /// </summary>
        /// <param name="metadata">从网站与 NFO 合并得到的元数据信息。</param>
        /// <param name="nfoDocument">NFO 文件管理器，用于将修改写回文件。</param>
        /// <param name="fileInfo">当前 NFO 的文件信息（用于构建文件相关路径）。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>方法会调用 <see cref="CreateVideoInfo"/>, <see cref="ProcessTitleAndTags"/>, <see cref="ProcessCoverImage"/> 等辅助方法完成处理流程。</remarks>
        private async Task ProcessMetadata(MetadataInfo metadata, NfoDocument nfoDocument, FileInfo fileInfo)
        {
            var videoInfo = CreateVideoInfo(metadata, nfoDocument, fileInfo);

            // 处理标题和标签
            await ProcessTitleAndTags(metadata, videoInfo);

            // 处理封面图片
            await ProcessCoverImage(metadata, videoInfo);

            // 保存更新后的 nfo 文件
            // 注意：此处只进行基本的元数据保存，后续 FixNfoTagsAsync 会进一步处理标签和文件名
            nfoDocument.SaveMetadata(videoInfo.VideoTitle, videoInfo.VideoOriginalTitle, videoInfo.VideoSortTitle, videoInfo.VideoPlot, "",
                videoInfo.VideoActors, metadata.Genres, metadata.Tags);

            // 更新 metadata 对象用于打印
            metadata.Title = videoInfo.VideoTitle;
            metadata.OriginalTitle = videoInfo.VideoOriginalTitle;

            PrintUpdatedMetadata(metadata, videoInfo.VideoSortTitle);
        }

        /// <summary>
        /// 根据抓取到的元数据与当前文件上下文构建一个用于后续保存和处理的 <see cref="VideoInfo"/> 对象。
        /// </summary>
        /// <param name="metadata">合并后的元数据信息。</param>
        /// <param name="nfoDocument">当前 NFO 管理器（保留以便后续扩展）。</param>
        /// <param name="fileInfo">当前 NFO 文件的文件信息对象。</param>
        /// <returns>包含最终用于保存的标题、排序标题、演员、目录与文件名等信息的 <see cref="VideoInfo"/> 实例。</returns>
        private VideoInfo CreateVideoInfo(MetadataInfo metadata, NfoDocument nfoDocument, FileInfo fileInfo)
        {
            var videoId = metadata.JavId.Id.ToUpper();
            var originalTitle = metadata.JavVideo.Title?.Trim() ?? string.Empty;

            // 使用配置化的清理器处理标题中的特殊字符
            var cleanedTitle = SanitizeTitle(originalTitle);

            // 传递文件目录用于检测外挂字幕和目录特征
            var titleInfo = ProcessVideoTitle(cleanedTitle, videoId, fileInfo.DirectoryName);
            var genres = ProcessVideoGenres(metadata.JavVideo.Genres, titleInfo.HasChineseSubtitle, titleInfo.HasUncensored);

            metadata.Genres = genres;

            // 构建原始标题：{番号} {标题}
            var videoOriginalTitle = $"{videoId} {cleanedTitle}";

            return new VideoInfo
            {
                VideoId = videoId,
                VideoTitle = titleInfo.ProcessedTitle,
                VideoOriginalTitle = videoOriginalTitle,
                VideoSortTitle = titleInfo.SortTitle,
                VideoPlot = metadata.Plot,
                VideoActors = ApplyActorReplacement(metadata.JavVideo.Actors),
                DirectoryName = fileInfo.DirectoryName,
                BaseFileName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                HasChineseSubtitle = titleInfo.HasChineseSubtitle,
                HasUncensored = titleInfo.HasUncensored
            };
        }

        private List<string> ApplyActorReplacement(List<string> actors)
        {
            if (!_actorReplaceConfig.Enabled)
                return actors ?? new List<string>();

            return ActorNameReplacer.ReplaceActors(actors ?? new List<string>(), _actorReplaceConfig.Replacements);
        }

        /// <summary>
        /// 使用配置中的规则清理标题字符串（移除指定子串或进行替换）。
        /// </summary>
        /// <param name="title">原始标题</param>
        /// <returns>清理后的标题</returns>
        private string SanitizeTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            var result = title;

            // 先执行替换映射
            if (_titleSanitizerConfig.ReplaceMap != null)
            {
                foreach (var kv in _titleSanitizerConfig.ReplaceMap)
                {
                    if (string.IsNullOrEmpty(kv.Key))
                        continue;

                    result = result.Replace(kv.Key, kv.Value ?? string.Empty);
                }
            }

            // 再执行移除列表
            if (_titleSanitizerConfig.RemoveStrings != null)
            {
                foreach (var s in _titleSanitizerConfig.RemoveStrings)
                {
                    if (string.IsNullOrEmpty(s))
                        continue;

                    result = result.Replace(s, string.Empty);
                }
            }

            return result.Trim();
        }

        /// <summary>
        /// 根据原始标题、番号与目录信息判断是否存在中文字幕、无码等特征，并生成带前缀/后缀的展示标题与排序标题。
        /// </summary>
        /// <param name="originalTitle">从站点抓取并清理后的原始标题。</param>
        /// <param name="videoId">视频番号（ID）。</param>
        /// <param name="directoryPath">包含视频的目录路径（用于检查外挂字幕或目录名特征）。</param>
        /// <returns>返回包含处理后标题、排序标题及特征标志的 <see cref="TitleProcessingResult"/>。</returns>
        /// <remarks>
        /// 检测逻辑：
        /// - 文本中包含“中字”“中文”或目录下存在字幕文件将被视为存在中文字幕；
        /// - 番号后缀或文本包含“無碼”“无码”或目录名包含“Un Censored”视为无码；
        /// - 根据组合结果决定是否在标题前加上前缀（如 "[中字] "、"[无码] "、"[中字无码] "）并在排序标题后附加标识后缀（如 -C、-U、-UC）。
        /// </remarks>
        private static TitleProcessingResult ProcessVideoTitle(string originalTitle, string videoId, string directoryPath)
        {
            // 检测中文字幕和无码特征
            var hasChineseSubtitle = originalTitle.Contains("中字") || originalTitle.Contains("中文");
            var hasUncensored = videoId.EndsWith(UC_SUFFIX) ||
                               originalTitle.Contains("無碼") ||
                               originalTitle.Contains("无码") ||
                               Directory.GetParent(directoryPath)?.Name.Contains("Un Censored") == true;

            // 检查外挂字幕
            string[] subtitleExtensions = ["*.srt", "*.ssa", "*.ass", "*.vtt", "*.sub"];
            bool hasSubtitles = subtitleExtensions.Any(ext =>
                Directory.GetFiles(directoryPath, ext, SearchOption.AllDirectories).Length > 0);

            if (hasSubtitles)
            {
                hasChineseSubtitle = true;
            }

            var processedTitle = originalTitle;
            var sortTitle = videoId;

            // 根据标签组合设置标题前缀和排序标题
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

            return new TitleProcessingResult
            {
                ProcessedTitle = processedTitle,
                SortTitle = sortTitle,
                HasChineseSubtitle = hasChineseSubtitle,
                HasUncensored = hasUncensored
            };
        }

        /// <summary>
        /// 根据特征向原始类型列表中补充/添加表示“中文字幕”“无码破解”的类型标签。
        /// </summary>
        /// <param name="originalGenres">从站点抓取的原始类型（Genres）列表，可能为 null。</param>
        /// <param name="hasChineseSubtitle">是否检测到中文字幕特征。</param>
        /// <param name="hasUncensored">是否检测到无码特征。</param>
        /// <returns>返回包含原始类型与新增类型的最终类型列表。</returns>
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
        /// 处理并修正标题与标签，同时触发示例图片的下载。
        /// </summary>
        /// <param name="metadata">当前的元数据信息实例（会被方法更新）。</param>
        /// <param name="videoInfo">基于元数据与文件上下文构建的 VideoInfo。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>方法会调用 <see cref="ProcessVideoTags"/> 更新标签并调用 <see cref="DownloadSampleImages"/> 下载示例图。</remarks>
        private async Task ProcessTitleAndTags(MetadataInfo metadata, VideoInfo videoInfo)
        {
            // 处理标签
            ProcessVideoTags(metadata, videoInfo);

            // 下载示例图片
            await DownloadSampleImages(metadata.JavVideo.Samples, videoInfo.DirectoryName);
        }

        /// <summary>
        /// 根据视频特征（如外挂字幕、中文字幕、无码等）补全标签列表并应用标签映射规则。
        /// </summary>
        /// <param name="metadata">元数据信息对象，方法会更新其 <see cref="MetadataInfo.Tags"/> 字段。</param>
        /// <param name="videoInfo">包含检测到的特征信息（如 HasChineseSubtitle、HasUncensored、DirectoryName）。</param>
        /// <remarks>
        /// - 会检测目录下常见字幕文件扩展名以判断是否存在外挂字幕，并添加对应标签；
        /// - 最后通过 <see cref="ApplyTagMappings"/> 将简写标签替换为完整标签。
        /// </remarks>
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
        /// 将简化标签替换为映射后的完整标签。
        /// </summary>
        /// <param name="tags">要处理的标签列表，方法会原地修改该列表中的元素。</param>
        /// <remarks>映射规则由静态字典 <see cref="TagMappings"/> 提供，新的映射可在该字典中添加。</remarks>
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
        /// 下载并保存视频封面图片（如果封面 URL 可用且本地尚不存在对应的 poster 文件）。
        /// </summary>
        /// <param name="metadata">包含封面 URL 的元数据信息。</param>
        /// <param name="videoInfo">用于生成目标保存路径（DirectoryName & BaseFileName）。</param>
        /// <returns>表示异步操作的任务。</returns>
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
                LogInformation($"下载封面图片: {coverPath}");
            }
            catch (Exception ex)
            {
                LogError("下载封面图片失败", ex);
            }
        }





        /// <summary>
        /// 并行下载示例图片（samples），并将其保存到配置指定的目录或当前目录。
        /// </summary>
        /// <param name="samples">示例图片 URL 列表。</param>
        /// <param name="directoryName">基础目录路径（用于构建目标保存路径）。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>会根据 <see cref="SampleImageConfig.UseSeparateDirectory"/> 决定保存目录和文件命名规则。</remarks>
        private async Task DownloadSampleImages(List<string> samples, string directoryName)
        {
            if (samples == null || !samples.Any())
                return;

            var targetDir = GetSampleDownloadDirectory(directoryName);
            var downloadTasks = CreateSampleDownloadTasks(samples, targetDir);

            await Task.WhenAll(downloadTasks);
        }

        /// <summary>
        /// 根据配置返回示例图片的目标保存目录，如果配置为使用单独目录则确保目录存在。
        /// </summary>
        /// <param name="directoryName">基础目录路径。</param>
        /// <returns>用于保存示例图片的完整目录路径。</returns>
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
        /// 为每个示例图片 URL 创建一个异步下载任务，并返回任务列表以便并行执行。
        /// </summary>
        /// <param name="samples">示例图片 URL 列表。</param>
        /// <param name="targetDir">目标保存目录。</param>
        /// <returns>返回可等待的一系列下载任务的列表。</returns>
        /// <remarks>方法会根据示例图片配置决定文件命名（如 sample-XX 或 backdropX）。</remarks>
        private List<Task> CreateSampleDownloadTasks(List<string> samples, string targetDir)
        {
            return [.. samples.Select(async (sample, index) =>
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
                        // 直接下载到当前目录时，使用 backdrop 命名方式
                        fileName = $"backdrop{index + 1}";
                    }

                    await Downloader.DownloadJpegAsync(sample, targetDir, fileName);
                    LogInformation($"下载示例图片: {fileName}.jpg");
                }
                catch (Exception ex)
                {
                    string fileName = _sampleImageConfig.UseSeparateDirectory ? $"sample-{index + 1:D2}" : $"backdrop{index + 1}";
                    LogError($"下载示例图片 {fileName}.jpg 失败", ex);
                }
            })];
        }

        /// <summary>
        /// 将最终确定的元数据信息格式化并输出到控制台，便于人工核查与调试。
        /// </summary>
        /// <param name="metadata">最终的元数据信息对象。</param>
        /// <param name="videoSortTitle">用于显示的排序标题。</param>
        private void PrintUpdatedMetadata(MetadataInfo metadata, string videoSortTitle)
        {
            Console.WriteLine($"-----------------修正后的元数据-----------------");
            Console.WriteLine($"videoId -> {metadata.JavId.Id.ToUpper()}");
            Console.WriteLine($"videoTitle -> {metadata.Title}");
            Console.WriteLine($"videoOriginalTitle -> {metadata.OriginalTitle}");
            Console.WriteLine($"videoSortTitle -> {videoSortTitle}");
            Console.WriteLine($"tags -> {String.Join(",", metadata.Tags)}");
            Console.WriteLine($"genres -> {String.Join(",", metadata.Genres)}");
            Console.WriteLine($"================================================");
        }

        /// <summary>
        /// 元数据信息类，包含视频的基本信息和从网站抓取的详细信息
        /// </summary>
        private class MetadataInfo
        {
            /// <summary>JAV 视频 ID</summary>
            public JavId JavId { get; set; }
            /// <summary>标题</summary>
            public string Title { get; set; }
            /// <summary>原始标题</summary>
            public string OriginalTitle { get; set; }
            /// <summary>
            /// 内容简介
            /// </summary>
            public string Plot { get; set; }
            /// <summary>类型列表</summary>
            public List<string> Genres { get; set; }
            /// <summary>标签列表</summary>
            public List<string> Tags { get; set; }
            /// <summary>JAV 视频详细信息</summary>
            public JavVideo JavVideo { get; set; }
        }

        /// <summary>
        /// 视频信息类，包含处理后的视频元数据
        /// </summary>
        private class VideoInfo
        {
            /// <summary>视频 ID</summary>
            public string VideoId { get; set; }
            /// <summary>视频标题</summary>
            public string VideoTitle { get; set; }
            /// <summary>视频原始标题</summary>
            public string VideoOriginalTitle { get; set; }
            /// <summary>视频排序标题</summary>
            public string VideoSortTitle { get; set; }
            /// <summary>视频介绍</summary>
            public string VideoPlot { get; set; }
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
        /// 标题处理结果类，包含处理后的标题、排序标题以及检测到的特征标志。
        /// </summary>
        private class TitleProcessingResult
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
        /// 将一条信息同时写入日志和控制台。
        /// </summary>
        /// <param name="message">要记录并显示的消息文本。</param>
        private void LogInformation(string message)
        {
            _logger.LogInformation(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// 将异常信息写入日志并在控制台输出简要错误描述。
        /// </summary>
        /// <param name="message">错误消息或上下文说明。</param>
        /// <param name="ex">捕获到的异常对象，用于记录完整的异常堆栈信息。</param>
        private void LogError(string message, Exception ex)
        {
            _logger.LogError(ex, message);
            Console.WriteLine($"{message}: {ex.Message}");
        }
    }
}