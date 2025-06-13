using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using JavScraper.Tools.Tools;
using MagicFile.Test;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.Tools.Services
{
    public class VideoProcessingService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<VideoProcessingService> _logger;

        public VideoProcessingService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<VideoProcessingService>();
        }

        public async Task ProcessVideoDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _logger.LogInformation("正在执行中......");
            Console.WriteLine("正在执行中......");

            JavOrganization.OrganizeDirectory(path);

            if (!File.Exists(Path.Combine(path, "poster.jpg")))
            {
                await JavBusOrganization(path);
                _logger.LogInformation("已整理目录：--->{0}", path);
                Console.WriteLine("已整理目录：--->{0}", path);
            }

            _logger.LogInformation("执行结束");
            Console.WriteLine("执行结束");
        }

        public async Task DownloadSampleImages(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _logger.LogInformation("正在执行中......");
            Console.WriteLine("正在执行中......");

            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            DirectoryHelper.GetAllFiles(path, javFiles);

            foreach (var javFile in javFiles)
            {
                if (!File.Exists(Path.Combine(path, "poster.jpg")))
                {
                    await JavBusOrganization(path);
                    _logger.LogInformation("已整理目录：--->{0}", path);
                    Console.WriteLine("已整理目录：--->{0}", path);
                }
            }

            _logger.LogInformation("执行结束");
            Console.WriteLine("执行结束");
        }

        public async Task GenerateVideoThumbnails(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            _logger.LogInformation("正在执行中......");
            Console.WriteLine("正在执行中......");

            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            DirectoryHelper.GetAllFiles(path, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key).ToLower();
                if (IsVideoFile(fileExt))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(javFile.Key);
                    var fileDir = dirInfo.Parent.FullName;
                    var fileName = Path.GetFileNameWithoutExtension(javFile.Key);
                    var pictureName = Path.Combine(fileDir, $"{fileName}-poster.jpg");

                    if (!File.Exists(pictureName))
                    {
                        VideoTools.GetPicFromVideo(javFile.Key, "15", pictureName);
                    }
                }
            }

            _logger.LogInformation("执行结束");
            Console.WriteLine("执行结束");
        }

        private async Task JavBusOrganization(string sourcePath)
        {
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            DirectoryHelper.GetAllFiles(sourcePath, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key).ToLower();
                if (IsVideoFile(fileExt))
                {
                    _logger.LogInformation("正在整理文件：--->{0}", javFile.Value);
                    Console.WriteLine("正在整理文件：--->{0}", javFile.Value);

                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(javFile.Key), "Sample-001.jpg").ToLower()))
                    {
                        var javId = JavRecognizer.Parse(javFile.Value);
                        var url = $"https://www.javbus.com/{javId}";

                        JavBus javBus = new JavBus(_loggerFactory);
                        var javVideo = await javBus.ParsePage(url);

                        if (javVideo != null)
                        {
                            await ProcessJavVideo(javVideo, javFile.Key, javId);
                        }
                    }
                }
            }
        }

        private async Task ProcessJavVideo(JavVideo javVideo, string filePath, JavId javId)
        {
            var coverUrl = $"https://www.javbus.com/{javVideo.Cover}";
            DirectoryInfo dirInfo = new DirectoryInfo(filePath);
            var fileDir = dirInfo.Parent.FullName;

            var saveFileName = await Downloader.DownloadAsync(coverUrl, fileDir, javId);
            await ProcessImages(saveFileName, fileDir, javId);
            await DownloadSamples(javVideo.Samples, fileDir);
        }

        private async Task ProcessImages(string sourceFile, string fileDir, JavId javId)
        {
            var posterFileName = Path.Combine(fileDir, "poster.jpg");
            if (javId.Type != JavIdType.Uncensored && javId.Matcher != "OnlyNumber")
            {
                ImageUtils.CutImage(sourceFile, posterFileName);
            }
            else
            {
                ImageUtils.ConvertImage(sourceFile, posterFileName);
            }

            var fanartFileName = Path.Combine(fileDir, "fanart.jpg");
            ImageUtils.ConvertImage(sourceFile, fanartFileName);

            var landscapeFileName = Path.Combine(fileDir, "landscape.jpg");
            ImageUtils.ConvertImage(sourceFile, landscapeFileName);
        }

        private async Task DownloadSamples(List<string> samples, string fileDir)
        {
            if (samples != null && samples.Count > 0)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    var sampleUrl = samples[i];
                    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";

                    if (!Regex.IsMatch(samples[i], urlRegex))
                    {
                        sampleUrl = $"https://www.javbus.com/{sampleUrl}";
                    }

                    var saveName = await Downloader.DownloadAsync(sampleUrl, fileDir, $"Sample-{(i + 1).ToString().PadLeft(3, '0')}");
                    _logger.LogInformation(saveName);
                    Console.WriteLine(saveName);
                }
            }
        }

        private bool IsVideoFile(string fileExtension)
        {
            return fileExtension.Contains(".wmv") || 
                   fileExtension.Contains(".mp4") || 
                   fileExtension.Contains(".mkv") || 
                   fileExtension.Contains(".avi") || 
                   fileExtension.Contains(".m2ts") || 
                   fileExtension.Contains(".ts");
        }
    }
}