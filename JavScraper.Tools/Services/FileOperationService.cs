using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JavScraper.Tools.Services
{
    /// <summary>
    /// 文件操作服务，负责处理文件和目录的各种操作。
    /// </summary>
    public class FileOperationService
    {
        private readonly ILogger<FileOperationService> _logger;

        public FileOperationService(ILogger<FileOperationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 备份文件。
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        /// <param name="backupSuffix">备份后缀，默认为 ".bak"</param>
        /// <returns>备份文件路径</returns>
        public async Task<string> BackupFileAsync(string filePath, string backupSuffix = ".bak")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }

                var backupPath = $"{filePath}{backupSuffix}";
                
                // 如果备份文件已存在，先删除
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    _logger.LogDebug("删除已存在的备份文件: {BackupPath}", backupPath);
                }

                // 复制文件
                await Task.Run(() => File.Copy(filePath, backupPath));
                
                _logger.LogDebug("文件备份完成: {OriginalPath} -> {BackupPath}", filePath, backupPath);
                return backupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "备份文件时发生错误: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 重命名文件。
        /// </summary>
        /// <param name="oldPath">原文件路径</param>
        /// <param name="newPath">新文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> RenameFileAsync(string oldPath, string newPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldPath))
                {
                    throw new ArgumentException("原文件路径不能为空", nameof(oldPath));
                }

                if (string.IsNullOrWhiteSpace(newPath))
                {
                    throw new ArgumentException("新文件路径不能为空", nameof(newPath));
                }

                if (!File.Exists(oldPath))
                {
                    _logger.LogWarning("原文件不存在: {OldPath}", oldPath);
                    return false;
                }

                if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("文件路径相同，无需重命名: {Path}", oldPath);
                    return true;
                }

                // 确保目标目录存在
                var targetDirectory = Path.GetDirectoryName(newPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    _logger.LogDebug("创建目标目录: {Directory}", targetDirectory);
                }

                // 如果目标文件已存在，先删除
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                    _logger.LogDebug("删除已存在的目标文件: {NewPath}", newPath);
                }

                await Task.Run(() => File.Move(oldPath, newPath));
                
                _logger.LogDebug("文件重命名完成: {OldPath} -> {NewPath}", oldPath, newPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重命名文件时发生错误: {OldPath} -> {NewPath}", oldPath, newPath);
                return false;
            }
        }

        /// <summary>
        /// 重命名目录。
        /// </summary>
        /// <param name="oldPath">原目录路径</param>
        /// <param name="newDirectoryName">新目录名称</param>
        /// <returns>是否成功</returns>
        public async Task<bool> RenameDirectoryAsync(string oldPath, string newDirectoryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(oldPath))
                {
                    throw new ArgumentException("原目录路径不能为空", nameof(oldPath));
                }

                if (string.IsNullOrWhiteSpace(newDirectoryName))
                {
                    throw new ArgumentException("新目录名称不能为空", nameof(newDirectoryName));
                }

                if (!Directory.Exists(oldPath))
                {
                    _logger.LogWarning("原目录不存在: {OldPath}", oldPath);
                    return false;
                }

                var parentDirectory = Path.GetDirectoryName(oldPath);
                var newPath = Path.Combine(parentDirectory ?? string.Empty, newDirectoryName);

                if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("目录路径相同，无需重命名: {Path}", oldPath);
                    return true;
                }

                // 如果目标目录已存在，需要特殊处理
                if (Directory.Exists(newPath))
                {
                    _logger.LogWarning("目标目录已存在: {NewPath}", newPath);
                    return false;
                }

                await Task.Run(() => Directory.Move(oldPath, newPath));
                
                _logger.LogDebug("目录重命名完成: {OldPath} -> {NewPath}", oldPath, newPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重命名目录时发生错误: {OldPath} -> {NewDirectoryName}", oldPath, newDirectoryName);
                return false;
            }
        }

        /// <summary>
        /// 复制文件。
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destinationPath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在的文件</param>
        /// <returns>是否成功</returns>
        public async Task<bool> CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    throw new ArgumentException("源文件路径不能为空", nameof(sourcePath));
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    throw new ArgumentException("目标文件路径不能为空", nameof(destinationPath));
                }

                if (!File.Exists(sourcePath))
                {
                    _logger.LogWarning("源文件不存在: {SourcePath}", sourcePath);
                    return false;
                }

                // 确保目标目录存在
                var targetDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    _logger.LogDebug("创建目标目录: {Directory}", targetDirectory);
                }

                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
                
                _logger.LogDebug("文件复制完成: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制文件时发生错误: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                return false;
            }
        }

        /// <summary>
        /// 删除文件。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("文件不存在，无需删除: {FilePath}", filePath);
                    return true;
                }

                await Task.Run(() => File.Delete(filePath));
                
                _logger.LogDebug("文件删除完成: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文件时发生错误: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 检查文件是否为只读，如果是则移除只读属性。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        public async Task<bool> EnsureFileWritableAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("文件不存在: {FilePath}", filePath);
                    return false;
                }

                await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                        _logger.LogDebug("移除文件只读属性: {FilePath}", filePath);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确保文件可写时发生错误: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// 获取文件大小。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件大小（字节），如果文件不存在返回 -1</returns>
        public async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("文件路径不能为空", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    return -1;
                }

                return await Task.Run(() => new FileInfo(filePath).Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件大小时发生错误: {FilePath}", filePath);
                return -1;
            }
        }
    }
}