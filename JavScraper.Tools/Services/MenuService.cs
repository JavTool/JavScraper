using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JavScraper.Tools.Services
{
    public class MenuService
    {
        private readonly ILogger<MenuService> _logger;
        private readonly VideoProcessingService _videoProcessingService;
        private readonly MetadataService _metadataService;
        private readonly FixNfoService _fixNfoService;

        public MenuService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MenuService>();
            _videoProcessingService = new VideoProcessingService(loggerFactory);
            _metadataService = new MetadataService(loggerFactory);
            _fixNfoService = new FixNfoService(loggerFactory);
        }

        public void PrintMenu()
        {
            Console.WriteLine("----------------------------------------------------------------------------");
            Console.WriteLine("------------------------- JavScraper.Tools v1.0.0 -------------------------");
            Console.WriteLine("1、整理视频（暂未实现）");
            Console.WriteLine("2、下载示例图片");
            Console.WriteLine("3、根据视频生成封面图");
            Console.WriteLine("4、修正标题和标签");
            Console.WriteLine("5、修正分类和标签");
            Console.WriteLine("6、快速裁切封面");
            Console.WriteLine("7、修复模式");
            Console.WriteLine("q、退出");
            Console.WriteLine("----------------------------------------------------------------------------");
        }

        public async Task HandleCommand(string command)
        {
            switch (command)
            {
                case "1":
                    await HandleOrganizeVideo();
                    break;
                case "2":
                    await HandleDownloadSampleImages();
                    break;
                case "3":
                    await HandleGenerateVideoThumbnails();
                    break;
                case "4":
                    await HandleFixMetadata();
                    break;
                case "5":
                    await HandleFixTags();
                    break;
                case "6":
                    await HandleQuickCutCover();
                    break;
                case "7":
                    await HandleFixMode();
                    break;
                case "q":
                    Environment.Exit(0);
                    break;
                default:
                    _logger.LogWarning("无效的命令");
                    break;
            }
        }
        /// <summary>
        /// 修复模式：根据用户输入的路径，修复该路径下的视频文件的元数据，包括标题、标签、分类等信息。用户可以选择一个目录，程序会递归地处理该目录下的所有视频文件，确保它们的元数据正确无误。这对于那些元数据不完整或错误的视频文件非常有用，可以帮助用户更好地管理和组织他们的视频库。
        /// </summary>
        /// <returns></returns>
        private async Task HandleFixMode()
        {
            Console.Write("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                Console.WriteLine();
                await _fixNfoService.FixNfoFilesAsync(path);
            }
        }

        private async Task HandleOrganizeVideo()
        {
            Console.WriteLine("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                await _videoProcessingService.ProcessVideoDirectory(path);
            }
        }

        private async Task HandleDownloadSampleImages()
        {
            Console.WriteLine("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                await _videoProcessingService.DownloadSampleImages(path);
            }
        }

        private async Task HandleGenerateVideoThumbnails()
        {
            Console.WriteLine("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                await _videoProcessingService.GenerateVideoThumbnails(path);
            }
        }

        private async Task HandleFixMetadata()
        {
            Console.Write("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                Console.WriteLine();
                await _metadataService.FixMetadataAsync(path);
            }
        }

        private async Task HandleFixTags()
        {
            Console.Write("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                Console.WriteLine();
                await _metadataService.FixNfoTagsAsync(path);
            }
        }

        private async Task HandleQuickCutCover()
        {
            Console.WriteLine("请输入一个路径：");
            string path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path))
            {
                await MetadataService.QuickCutCover(path);
            }
        }
    }
}