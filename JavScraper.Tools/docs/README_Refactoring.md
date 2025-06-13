# NFO 数据处理重构说明

## 概述

原始的 `NfoDataFIx.cs` 类已被重构为更加模块化和可维护的架构。新架构将单一的大类拆分为多个专门的服务类和工具类，提供更好的代码组织、错误处理和测试支持。

## 重构架构

### 核心组件

#### 1. 配置类
- **`Configuration/TagMappingConfig.cs`** - 静态配置，包含标签映射字典和支持的字幕扩展名

#### 2. 服务类
- **`Services/NfoDataService.cs`** - 核心 NFO 数据处理服务
- **`Services/TagProcessingService.cs`** - 标签和类型处理服务
- **`Services/FileOperationService.cs`** - 文件操作服务

#### 3. 模型类
- **`Models/ProcessingModels.cs`** - 处理过程中使用的数据模型

#### 4. 工具类
- **`Utilities/ProcessingUtilities.cs`** - 通用处理工具

#### 5. 主处理器
- **`NfoDataProcessor.cs`** - 主要的处理器类，整合所有服务

## 迁移指南

### 原始用法
```csharp
// 旧的用法
await NfoDataFIx.FixNfoTagsAsync(nfoFilePath);
await NfoDataFIx.FixNfoDataAsync(nfoFiles);
```

### 新用法
```csharp
// 新的用法
var processor = NfoDataProcessorFactory.Create();

// 修复单个 NFO 文件的标签
var tagResult = await processor.FixNfoTagsAsync(nfoFilePath);

// 批量更新 NFO 数据
var dataResult = await processor.FixNfoDataAsync(nfoFiles);

// 批量处理
var batchResult = await processor.ProcessBatchAsync(nfoFiles);
```

### 依赖注入用法
```csharp
// 在 DI 容器中注册服务
services.AddSingleton<TagProcessingService>();
services.AddSingleton<FileOperationService>();
services.AddSingleton<NfoDataService>();
services.AddSingleton<NfoDataProcessor>();

// 在控制器或服务中使用
public class MyService
{
    private readonly NfoDataProcessor _processor;
    
    public MyService(NfoDataProcessor processor)
    {
        _processor = processor;
    }
    
    public async Task ProcessNfoAsync(string filePath)
    {
        var result = await _processor.FixNfoTagsAsync(filePath);
        // 处理结果...
    }
}
```

## 新架构优势

### 1. 职责分离
- 每个类都有明确的职责
- 更容易理解和维护
- 符合单一职责原则

### 2. 更好的错误处理
- 结构化的错误信息
- 详细的处理结果反馈
- 更好的日志记录

### 3. 可测试性
- 每个服务都可以独立测试
- 支持依赖注入
- 更容易编写单元测试

### 4. 配置化
- 标签映射可以外部化配置
- 处理选项可以灵活调整
- 更好的扩展性

### 5. 性能优化
- 更好的并发控制
- 减少重复计算
- 优化的文件操作

## 返回值说明

### NfoProcessResult
```csharp
public class NfoProcessResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string FilePath { get; set; }
    public TagProcessStatistics Statistics { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

### BatchProcessResult
```csharp
public class BatchProcessResult
{
    public int TotalFiles { get; set; }
    public int SuccessfulFiles { get; set; }
    public int FailedFiles { get; set; }
    public List<NfoProcessResult> Results { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

## 配置选项

### ProcessingConfig
```csharp
public class ProcessingConfig
{
    public bool CreateBackup { get; set; } = true;
    public bool ProcessSubtitles { get; set; } = true;
    public bool UpdateDirectoryNames { get; set; } = true;
    public bool CopyToOtherNfoFiles { get; set; } = true;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
}
```

## 注意事项

1. **向后兼容性**: 原始的 `NfoDataFIx` 类仍然可用，但已标记为过时
2. **性能**: 新架构在大批量处理时性能更好
3. **错误处理**: 新架构提供更详细的错误信息和处理状态
4. **扩展性**: 更容易添加新的处理功能和配置选项

## 示例代码

### 基本使用
```csharp
using JavScraper.Tools;
using JavScraper.Tools.Services;

// 创建处理器
var processor = NfoDataProcessorFactory.Create();

// 处理单个文件
var result = await processor.FixNfoTagsAsync(@"C:\Videos\movie.nfo");
if (result.Success)
{
    Console.WriteLine($"处理成功: {result.Message}");
    Console.WriteLine($"处理统计: 添加了 {result.Statistics.TagsAdded} 个标签");
}
else
{
    Console.WriteLine($"处理失败: {result.Message}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"错误: {error}");
    }
}
```

### 批量处理
```csharp
var nfoFiles = Directory.GetFiles(@"C:\Videos", "*.nfo", SearchOption.AllDirectories);
var batchResult = await processor.ProcessBatchAsync(nfoFiles);

Console.WriteLine($"批量处理完成:");
Console.WriteLine($"总文件数: {batchResult.TotalFiles}");
Console.WriteLine($"成功: {batchResult.SuccessfulFiles}");
Console.WriteLine($"失败: {batchResult.FailedFiles}");
Console.WriteLine($"处理时间: {batchResult.ProcessingTime}");
```

## 迁移检查清单

- [ ] 更新所有对 `NfoDataFIx.FixNfoTagsAsync()` 的调用
- [ ] 更新所有对 `NfoDataFIx.FixNfoDataAsync()` 的调用
- [ ] 添加适当的错误处理代码
- [ ] 考虑使用依赖注入（如果适用）
- [ ] 更新单元测试
- [ ] 验证新的返回值结构
- [ ] 测试批量处理功能