using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace JavScraper.Tools.Utils
{
    public static class BackupUtils
    {
        public static void BackupImages(string directoryPath, ILogger logger)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupPath = Path.Combine(directoryPath, $"images_backup_{timestamp}.zip");

                var imageFiles = Directory.GetFiles(directoryPath, "*.jpg")
                    .Concat(Directory.GetFiles(directoryPath, "*.png"))
                    .Concat(Directory.GetFiles(directoryPath, "*.webp"))
                    .Where(f => !f.Contains("images_backup_"))
                    .ToList();

                if (imageFiles.Any())
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        foreach (var file in imageFiles)
                        {
                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));
                        }
                        ZipFile.CreateFromDirectory(tempDir, backupPath);
                        logger.LogInformation($"已创建图片备份：{backupPath}");
                    }
                    finally
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"创建图片备份失败：{ex.Message}");
            }
        }
    }
}