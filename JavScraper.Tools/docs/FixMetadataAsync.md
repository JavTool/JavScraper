# FixMetadataAsync 方法执行流程文档

## 方法概述

`FixMetadataAsync` 是 `MetadataService` 类中的一个公共异步方法，用于修正指定路径下所有 NFO 文件的元数据。该方法会遍历指定目录及其子目录中的所有 NFO 文件，并对每个文件进行处理，包括元数据修正、标签处理、封面图片下载等操作。

## 方法签名

```csharp
public async Task FixMetadataAsync(string path)
```

### 参数

- `path`：要处理的目录路径，字符串类型

### 返回值

- `Task`：异步任务

## 执行流程

### 1. 获取所有文件

方法首先创建一个字典用于存储所有文件的路径和名称：

```csharp
Dictionary<string, string> javFiles = new Dictionary<string, string>();
DirectoryHelper.GetAllFiles(path, javFiles);
```

`DirectoryHelper.GetAllFiles` 方法会递归遍历指定目录及其子目录，将所有文件的路径和名称添加到字典中。

### 2. 遍历处理 NFO 文件

然后，方法遍历字典中的所有文件，筛选出扩展名为 .nfo 且不包含 .bak.nfo 的文件进行处理：

```csharp
foreach (var javFile in javFiles)
{
    var fileExt = Path.GetExtension(javFile.Key);
    if (fileExt.ToLower().Contains(".nfo") && !javFile.Key.Contains(".bak.nfo"))
    {
        await ProcessNfoFile(javFile.Key);
    }
}
```

### 3. 处理单个 NFO 文件 (ProcessNfoFile)

对于每个符合条件的 NFO 文件，调用 `ProcessNfoFile` 方法进行处理：

```csharp
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
```

该方法首先验证 NFO 文件是否可以处理，然后调用 `ProcessValidNfoFile` 方法处理有效的 NFO 文件。如果处理过程中发生异常，则记录错误日志并显示错误信息。

### 4. 验证 NFO 文件 (ValidateNfoFile)

```csharp
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
```

该方法检查 NFO 文件是否为隐藏文件，如果是隐藏文件则跳过处理。

### 5. 处理有效的 NFO 文件 (ProcessValidNfoFile)

```csharp
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
```

该方法执行以下步骤：

1. 创建 NFO 文件的备份（如果备份不存在）
2. 创建 NfoFileManager 实例用于管理 NFO 文件
3. 从 NFO 文件获取元数据信息
4. 处理元数据，包括标题、标签、封面图片等
5. 打印更新后的元数据信息

### 6. 创建备份文件 (CreateBackupIfNotExists)

```csharp
private void CreateBackupIfNotExists(FileInfo fileInfo)
{
    var destFileName = Path.Combine(fileInfo.DirectoryName,
        $"{Path.GetFileNameWithoutExtension(fileInfo.FullName)}{BACKUP_EXTENSION}{fileInfo.Extension}");
    
    if (!File.Exists(destFileName))
    {
        fileInfo.CopyTo(destFileName);
    }
}
```

该方法检查备份文件是否存在，如果不存在则创建备份文件。备份文件的命名格式为：原文件名.bak.nfo。

### 7. 从 NFO 文件获取元数据信息 (GetMetadataFromNfo)

```csharp
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
```

该方法执行以下步骤：

1. 从 NFO 文件获取标题、排序标题、原始标题、类型和标签
2. 解析 JAV 番号
3. 使用 Jav123Scraper 或 JavCaptain 获取 JAV 视频信息
4. 创建并返回 MetadataInfo 对象

### 8. 处理元数据 (ProcessMetadata)

```csharp
private async Task ProcessMetadata(MetadataInfo metadata, NfoFileManager nfoManager, FileInfo fileInfo)
{
    var videoInfo = CreateVideoInfo(metadata, nfoManager, fileInfo);
    
    // 处理标题和标签
    await ProcessTitleAndTags(metadata, videoInfo);

    // 处理封面图片
    await ProcessCoverImage(metadata, videoInfo);

    // 保存更新后的 nfo 文件
    nfoManager.SaveMetadata(videoInfo.VideoTitle, videoInfo.VideoOriginalTitle, videoInfo.VideoSortTitle, videoInfo.VideoId, "", 
        videoInfo.VideoActors, metadata.Genres, metadata.Tags);
}
```

该方法执行以下步骤：

1. 创建视频信息对象
2. 处理标题和标签
3. 处理封面图片
4. 保存更新后的 NFO 文件

### 9. 创建视频信息对象 (CreateVideoInfo)

```csharp
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
```

该方法执行以下步骤：

1. 获取视频 ID、标题、原始标题、排序标题和演员列表
2. 处理视频标题，检测是否有中文字幕和无码标志
3. 处理视频类型，根据是否有中文字幕和无码添加相应的类型
4. 创建并返回 VideoInfo 对象

### 10. 处理视频标题 (ProcessVideoTitle)

```csharp
private ProcessedTitleInfo ProcessVideoTitle(string originalTitle, string videoId)
{
    var hasChineseSubtitle = originalTitle.Contains("中字") || originalTitle.Contains("中文");
    var hasUncensored = videoId.EndsWith(UC_SUFFIX) || originalTitle.Contains("無碼") || originalTitle.Contains("无码");
    
    var processedTitle = originalTitle;
    var sortTitle = videoId;

    if (hasChineseSubtitle && hasUncensored)
    {
        processedTitle = $"{CHINESE_UNCENSORED_PREFIX}{originalTitle}";
        sortTitle = $"{videoId}{UC_SUFFIX}";
    }
    else if (hasChineseSubtitle)
    {
        processedTitle = $"{CHINESE_PREFIX}{originalTitle}";
        sortTitle = $"{videoId}{C_SUFFIX}";
    }
    else if (hasUncensored)
    {
        processedTitle = $"{UNCENSORED_PREFIX}{originalTitle}";
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
```

该方法执行以下步骤：

1. 检测标题是否包含中文字幕和无码标志
2. 根据检测结果处理标题和排序标题
3. 创建并返回 ProcessedTitleInfo 对象

### 11. 处理视频类型 (ProcessVideoGenres)

```csharp
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
```

该方法执行以下步骤：

1. 复制原始类型列表
2. 如果有中文字幕且类型列表中不包含中文字幕标签，则添加中文字幕标签
3. 如果是无码且类型列表中不包含无码标签，则添加无码标签
4. 返回处理后的类型列表

### 12. 处理标题和标签 (ProcessTitleAndTags)

```csharp
private async Task ProcessTitleAndTags(MetadataInfo metadata, VideoInfo videoInfo)
{
    // 处理标签
    ProcessVideoTags(metadata, videoInfo);
    
    // 下载样本图片
    await DownloadSampleImages(metadata.JavVideo.Samples, videoInfo.DirectoryName);
}
```

该方法执行以下步骤：

1. 处理视频标签
2. 下载样本图片

### 13. 处理视频标签 (ProcessVideoTags)

```csharp
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
    
    // 应用标签映射
    ApplyTagMappings(tags);
    
    metadata.Tags = tags;
}
```

该方法执行以下步骤：

1. 复制原始标签列表
2. 如果有中文字幕且标签列表中不包含中文字幕标签，则添加中文字幕标签
3. 如果是无码且标签列表中不包含无码标签，则添加无码标签
4. 应用标签映射
5. 更新元数据的标签列表

### 14. 应用标签映射 (ApplyTagMappings)

```csharp
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
```

该方法遍历标签列表，将简化标签映射为完整标签。

### 15. 处理封面图片 (ProcessCoverImage)

```csharp
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
```

该方法执行以下步骤：

1. 检查封面图片 URL 是否为空，如果为空则返回
2. 构建封面图片路径
3. 检查封面图片是否已存在，如果已存在则返回
4. 下载封面图片
5. 记录日志并显示下载信息

### 16. 下载样本图片 (DownloadSampleImages)

```csharp
private async Task DownloadSampleImages(List<string> samples, string directoryName)
{
    if (samples == null || !samples.Any())
        return;

    var targetDir = GetSampleDownloadDirectory(directoryName);
    var downloadTasks = CreateSampleDownloadTasks(samples, targetDir);
    
    await Task.WhenAll(downloadTasks);
}
```

该方法执行以下步骤：

1. 检查样本图片 URL 列表是否为空，如果为空则返回
2. 获取样本图片下载目录
3. 创建样本图片下载任务
4. 等待所有下载任务完成

### 17. 获取样本图片下载目录 (GetSampleDownloadDirectory)

```csharp
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
```

该方法根据配置决定样本图片的下载目录：

1. 如果配置为使用单独目录，则创建并返回单独目录
2. 否则，直接返回当前目录

### 18. 创建样本图片下载任务 (CreateSampleDownloadTasks)

```csharp
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
```

该方法为每个样本图片 URL 创建一个下载任务：

1. 根据配置决定文件名格式
2. 下载样本图片
3. 记录日志并显示下载信息

### 19. 打印更新后的元数据信息 (PrintUpdatedMetadata)

```csharp
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
```

该方法将更新后的元数据信息打印到控制台。

## 辅助方法

### LogAndDisplay

```csharp
private void LogAndDisplay(string message)
{
    _logger.LogInformation(message);
    Console.WriteLine(message);
}
```

该方法记录日志并显示信息到控制台。

### LogAndDisplayError

```csharp
private void LogAndDisplayError(string message, Exception ex)
{
    _logger.LogError(ex, message);
    Console.WriteLine($"{message}: {ex.Message}");
}
```

该方法记录错误日志并显示错误信息到控制台。

## 内部类

### MetadataInfo

```csharp
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
```

### VideoInfo

```csharp
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
```

### ProcessedTitleInfo

```csharp
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
```

## 总结

`FixMetadataAsync` 方法是一个复杂的异步方法，用于修正指定路径下所有 NFO 文件的元数据。该方法通过一系列步骤，包括获取文件、验证文件、处理元数据、下载封面图片和样本图片等，实现了对 JAV 视频元数据的全面修正和优化。方法的执行流程清晰，错误处理完善，是一个功能强大的元数据处理工具。