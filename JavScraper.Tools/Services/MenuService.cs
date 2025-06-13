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

        public MenuService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MenuService>();
            _videoProcessingService = new VideoProcessingService(loggerFactory);
            _metadataService = new MetadataService(loggerFactory);
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
                case "q":
                    Environment.Exit(0);
                    break;
                default:
                    _logger.LogWarning("无效的命令");
                    break;
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
                await _metadataService.QuickCutCover(path);
            }
        }
    }
}