using AngleSharp;
using AngleSharp.Media;
using HtmlAgilityPack;
//using JavScraper.Domain;
using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using JavScraper.Tools.Tools;
using MagicFile.Test;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
//using System.Formats.Tar;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static JavScraper.Tools.ImageUtils;
using static System.Net.Mime.MediaTypeNames;

namespace JavScraper.Tools
{
    class Program
    {


        static async Task Main(string[] args)
        {

            // 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("Logs/app-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // 使用 Serilog 作为提供程序
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            });


            var logger = loggerFactory.CreateLogger<Program>();


            PrintMenu();

            while (true)
            {
                Console.Write("选择你要执行的功能模块：");
                var command = Console.ReadLine();
                Console.WriteLine();

                if (command == "1")
                {

                    Console.WriteLine("请输入一个路径：");
                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine("正在执行中......");


                        JavOrganization.OrganizeDirectory(path);
                        //foreach (var path in paths)
                        //{
                        if (!File.Exists(string.Format(@"{0}\{1}", path, "poster.jpg")))
                        {
                            await JavBusOrganization(loggerFactory, path);

                            //await Madou.MadouOrganization(loggerFactory, sourcePath);
                            Console.WriteLine("已整理目录：--->{0}", path);
                            logger.LogInformation("已整理目录：--->{0}", path);
                        }
                        //}
                        Console.WriteLine("执行结束");
                    }

                }
                else if (command == "2")
                {
                    Console.WriteLine("正在执行中......");
                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine("请输入一个路径：");
                        Dictionary<string, string> javFiles = new Dictionary<string, string>();
                        //JavOrganization.OrganizeDirectory(destDirectory);
                        Director(path, javFiles);
                        foreach (var javFile in javFiles)
                        {
                            if (!File.Exists(string.Format(@"{0}\{1}", path, "poster.jpg")))
                            {
                                await JavBusOrganization(loggerFactory, path);

                                //await Madou.MadouOrganization(loggerFactory, sourcePath);
                                Console.WriteLine("已整理目录：--->{0}", path);
                                logger.LogInformation("已整理目录：--->{0}", path);
                            }
                        }
                        Console.WriteLine("执行结束");
                    }
                }
                else if (command == "3")
                {

                    Console.WriteLine("请输入一个路径：");
                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine("正在执行中......");
                        Dictionary<string, string> javFiles = new Dictionary<string, string>();
                        Director(path, javFiles);
                        foreach (var javFile in javFiles)
                        {
                            var fileExt = Path.GetExtension(javFile.Key);
                            if (fileExt.ToLower().Contains(".wmv") || fileExt.ToLower().Contains(".mp4") ||
                                fileExt.ToLower().Contains(".mkv") || fileExt.ToLower().Contains(".avi") ||
                                fileExt.ToLower().Contains(".m2ts") ||
                                fileExt.ToLower().Contains(".ts"))
                            {
                                DirectoryInfo dirInfo = new DirectoryInfo(javFile.Key);
                                var fileDir = dirInfo.Parent.FullName;
                                var fileName = Path.GetFileNameWithoutExtension(javFile.Key);
                                var pictureName = fileDir + "\\" + fileName + "-poster.jpg";
                                if (!File.Exists(pictureName))
                                {
                                    VideoTools.GetPicFromVideo(javFile.Key, "15", pictureName);
                                }
                            }
                        }
                        Console.WriteLine("执行结束");
                    }

                }
                else if (command == "4")
                {
                    Console.Write("请输入一个路径：");

                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine();
                        Console.WriteLine("正在执行中......");
                        //await FixMetadataAsync(loggerFactory, path);

                        await NfoDataFIx.FixNfoDataAsync(loggerFactory, path);
                        Console.WriteLine("执行结束");
                    }
                }
                else if (command == "5")
                {
                    Console.Write("请输入一个路径：");

                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        Console.WriteLine();
                        Console.WriteLine("正在执行中......");
                        //await FixMetadataAsync(loggerFactory, path);

                        await NfoDataFIx.FixNfoTagsAsync(loggerFactory, path);
                        Console.WriteLine("执行结束");
                    }
                }
                else if (command == "6")
                {
                    Console.WriteLine("请输入一个路径：");
                    string path = Console.ReadLine();
                    if (!string.IsNullOrEmpty(path))
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        //Console.WriteLine("请输入一个路径：");
                        //DirectoryInfo dirInfo = new DirectoryInfo(path);
                        //foreach (var file in dirInfo.GetFiles())
                        //{
                        var fileExt = Path.GetExtension(fileInfo.FullName);
                        if (fileExt.ToLower().Contains(".jpg") || fileExt.ToLower().Contains(".jpeg"))
                        {
                            string destName = string.Format("{0}-{1}", Path.GetFileNameWithoutExtension(fileInfo.FullName), "poster");
                            ImageUtils.CutImage(fileInfo.FullName, fileInfo.DirectoryName, "poster");
                        }
                        //}
                    }
                    Console.WriteLine("裁切完成！");
                }
                else if (command == "q")
                {
                    Environment.Exit(0);
                }
                PrintMenu();

            }

            //Console.WriteLine("整理完毕!");
            Console.WriteLine("{0}：--->整理完毕!", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            logger.LogInformation("{0}：--->整理完毕!", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        static bool Confirm()
        {
            Console.WriteLine("是否确认执行此操作? ");
            Console.WriteLine("确认请输入[Y]，按任意键返回菜单");
            var key = Console.ReadKey();
            Console.WriteLine("");
            if (key.KeyChar.ToString().ToUpper() == "Y")
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// 打印菜单。
        /// 1、整理视频
        /// 2、下载更多图片
        /// 3、生成缩略图
        /// 4、修正标题
        /// </summary>
        static void PrintMenu()
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

        private static async Task JavBusOrganization(ILoggerFactory loggerFactory, string sourcePath)
        {
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            Director(sourcePath, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                if (fileExt.ToLower().Contains(".wmv") || fileExt.ToLower().Contains(".mp4") || fileExt.ToLower().Contains(".mkv") || fileExt.ToLower().Contains(".avi"))
                {
                    Console.WriteLine("正在整理文件：--->{0}", javFile.Value);
                    if (!File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), "Sample-001.jpg").ToLower()))
                    {
                        var javId = JavRecognizer.Parse(javFile.Value);

                        var url = string.Format("https://www.javbus.com/{0}", javId);

                        JavBus javBus = new JavBus(loggerFactory);

                        var javVideo = await javBus.ParsePage(url);
                        if (javVideo != null)
                        {
                            var coverUrl = string.Format("https://www.javbus.com/{0}", javVideo.Cover);

                            DirectoryInfo dirInfo = new DirectoryInfo(javFile.Key);
                            var fileDir = dirInfo.Parent.FullName;

                            var saveFileName = await Downloader.DownloadAsync(coverUrl, fileDir, javId);
                            var posterFileName = string.Format("{0}/{1}{2}", fileDir, "poster", ".jpg");
                            if (javId.Type != JavIdType.Uncensored && javId.Matcher != "OnlyNumber")
                            {
                                ImageUtils.CutImage(saveFileName, posterFileName);
                            }
                            else
                            {
                                ImageUtils.ConvertImage(saveFileName, posterFileName);
                            }

                            var fanartFileName = string.Format("{0}/{1}{2}", fileDir, "fanart", ".jpg");
                            ImageUtils.ConvertImage(saveFileName, fanartFileName);
                            var landscapeFileName = string.Format("{0}/{1}{2}", fileDir, "landscape", ".jpg");
                            ImageUtils.ConvertImage(saveFileName, landscapeFileName);
                            if (javVideo.Samples != null && javVideo.Samples.Count > 0)
                            {

                                for (int i = 0; i < javVideo.Samples.Count; i++)
                                {
                                    var sampleUrl = javVideo.Samples[i];
                                    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";

                                    if (!Regex.IsMatch(javVideo.Samples[i], urlRegex))
                                    {
                                        sampleUrl = string.Format("https://www.javbus.com/{0}", sampleUrl);
                                    }
                                    var saveName = await Downloader.DownloadAsync(sampleUrl, fileDir, string.Format("Sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
                                    Console.WriteLine(saveName);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// https://javdb.com/search?q=MXGS-917&f=all
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        private static async Task JavDBOrganization(ILoggerFactory loggerFactory, string sourcePath)
        {
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            Director(sourcePath, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                if (fileExt.ToLower().Contains(".wmv") || fileExt.ToLower().Contains(".mp4") || fileExt.ToLower().Contains(".mkv") || fileExt.ToLower().Contains(".avi") || fileExt.ToLower().Contains(".m2ts"))
                {
                    Console.WriteLine("{0}：--->正在整理文件{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), javFile.Value);
                    if (!File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), "Sample-001.jpg").ToLower()))
                    {
                        var javId = JavRecognizer.Parse(javFile.Value);

                        JavDB javDB = new JavDB(loggerFactory);

                        var javVideoList = await javDB.Query(javId.Id);
                        if (javVideoList != null && javVideoList.Count > 0)
                        {
                            var url = string.Format("https://javdb.com/{0}", javVideoList[0].Url);
                            var javVideo = await javDB.ParsePage(url);
                            if (javVideo != null)
                            {
                                OrganizeFiles(javVideo, javFile.Key, sourcePath);
                                var coverUrl = string.Format("{0}", javVideo.Cover);

                                DirectoryInfo dirInfo = new DirectoryInfo(javFile.Key);
                                var fileDir = dirInfo.Parent.FullName;

                                var saveFileName = await Downloader.DownloadAsync(coverUrl, fileDir, javId);
                                var posterFileName = string.Format("{0}/{1}{2}", fileDir, "poster", ".jpg");
                                if (javId.Type != JavIdType.Uncensored && javId.Matcher != "OnlyNumber")
                                {
                                    ImageUtils.CutImage(saveFileName, posterFileName);
                                }
                                else
                                {
                                    ImageUtils.ConvertImage(saveFileName, posterFileName);
                                }

                                var fanartFileName = string.Format("{0}/{1}{2}", fileDir, "fanart", ".jpg");
                                ImageUtils.ConvertImage(saveFileName, fanartFileName);
                                var landscapeFileName = string.Format("{0}/{1}{2}", fileDir, "landscape", ".jpg");
                                ImageUtils.ConvertImage(saveFileName, landscapeFileName);
                                if (javVideo.Samples != null && javVideo.Samples.Count > 0)
                                {
                                    int i = 0;
                                    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                                    foreach (var sample in javVideo.Samples)
                                    {
                                        if (Regex.IsMatch(sample, urlRegex))
                                        {
                                            var saveName = await Downloader.DownloadAsync(sample, fileDir, string.Format("sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
                                            i++;
                                            Console.WriteLine("{0}：--->已下载文件 {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), saveName);
                                        }
                                    }
                                    //for (int i = 0; i < javVideo.Samples.Count; i++)
                                    //{
                                    //    var sampleUrl = javVideo.Samples[i];
                                    //    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";

                                    //    if (Regex.IsMatch(javVideo.Samples[i], urlRegex))
                                    //    {
                                    //        //sampleUrl = string.Format("https://www.javbus.com/{0}", sampleUrl);
                                    //        var saveName = Downloader.Download(sampleUrl, fileDir, string.Format("Sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
                                    //        //Console.WriteLine(saveName);
                                    //        Console.WriteLine("{0}：--->已下载文件 {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), saveName);
                                    //    }
                                    //    else
                                    //    {

                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理欧美视频。
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        private static async Task JavCaptainOrganization(ILoggerFactory loggerFactory, string sourcePath)
        {
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            Director(sourcePath, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                if (fileExt.ToLower().Contains(".wmv") || fileExt.ToLower().Contains(".mp4") || fileExt.ToLower().Contains(".mkv") || fileExt.ToLower().Contains(".avi") || fileExt.ToLower().Contains(".m2ts"))
                {
                    Console.WriteLine("{0}：--->正在整理文件{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), javFile.Value);
                    if (!File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), "Sample-001.jpg").ToLower()))
                    {
                        //var url = string.Format("https://javcaptain.com/zh/{0}", javId);

                        //JavBus javBus = new JavBus(loggerFactory);

                        var javId = JavRecognizer.Parse(javFile.Value);

                        JavCaptain javCaptain = new JavCaptain(loggerFactory);

                        var javVideoList = await javCaptain.Query(javId.Id);
                        if (javVideoList != null && javVideoList.Count > 0)
                        {
                            //var url = string.Format("https://javcaptain.com/zh/{0}", javId);
                            var url = string.Format("https://javcaptain.com/zh/{0}", javVideoList[0].Url);
                            var javVideo = await javCaptain.ParsePage(javVideoList[0].Url);
                            if (javVideo != null)
                            {
                                OrganizeFiles(javVideo, javFile.Key, sourcePath);
                                var coverUrl = string.Format("{0}", javVideo.Cover);

                                DirectoryInfo dirInfo = new DirectoryInfo(javFile.Key);
                                var fileDir = dirInfo.Parent.FullName;

                                var saveFileName = await Downloader.DownloadAsync(coverUrl, fileDir, javId);
                                var posterFileName = string.Format("{0}/{1}{2}", fileDir, "poster", ".jpg");
                                if (javId.Type != JavIdType.Uncensored && javId.Matcher != "OnlyNumber")
                                {
                                    ImageUtils.CutImage(saveFileName, posterFileName);
                                }
                                else
                                {
                                    ImageUtils.ConvertImage(saveFileName, posterFileName);
                                }

                                var fanartFileName = string.Format("{0}/{1}{2}", fileDir, "fanart", ".jpg");
                                ImageUtils.ConvertImage(saveFileName, fanartFileName);
                                var landscapeFileName = string.Format("{0}/{1}{2}", fileDir, "landscape", ".jpg");
                                ImageUtils.ConvertImage(saveFileName, landscapeFileName);
                                if (javVideo.Samples != null && javVideo.Samples.Count > 0)
                                {
                                    int i = 0;
                                    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                                    foreach (var sample in javVideo.Samples)
                                    {
                                        if (Regex.IsMatch(sample, urlRegex))
                                        {
                                            var saveName = await Downloader.DownloadAsync(sample, fileDir, string.Format("sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
                                            i++;
                                            Console.WriteLine("{0}：--->已下载文件 {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), saveName);
                                        }
                                    }
                                    //for (int i = 0; i < javVideo.Samples.Count; i++)
                                    //{
                                    //    var sampleUrl = javVideo.Samples[i];
                                    //    string urlRegex = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";

                                    //    if (Regex.IsMatch(javVideo.Samples[i], urlRegex))
                                    //    {
                                    //        //sampleUrl = string.Format("https://www.javbus.com/{0}", sampleUrl);
                                    //        var saveName = Downloader.Download(sampleUrl, fileDir, string.Format("Sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
                                    //        //Console.WriteLine(saveName);
                                    //        Console.WriteLine("{0}：--->已下载文件 {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), saveName);
                                    //    }
                                    //    else
                                    //    {

                                    //    }
                                    //}
                                }
                            }
                        }
                    }
                }
            }
        }




        /// <summary>
        /// 修正元数据。
        /// </summary>
        /// <remarks>
        /// 标准格式
        /// title [中字] 人生初 絶頂、その向こう側へ
        /// originalTitle SSIS-531 人生初 絶頂、その向こう側へ
        /// sortTitle SSIS-531-C SSIS-531-UC
        /// </remarks>
        private static async Task FixMetadataAsync(ILoggerFactory loggerFactory, string path)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            Dictionary<string, string> javFiles = new Dictionary<string, string>();
            Director(path, javFiles);

            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                if (fileExt.ToLower().Contains(".nfo") && !javFile.Key.Contains(".bak.nfo"))
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(javFile.Key);
                        FileAttributes attributes = File.GetAttributes(javFile.Key);
                        if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            logger.LogInformation($"当前 nfo 文件已隐藏，跳过！");
                            Console.WriteLine($"当前 nfo 文件已隐藏，跳过！");
                            continue;
                        }

                        // 备份 nfo 文件
                        var destFileName = string.Format(@"{0}\{1}{2}", fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(javFile.Key) + ".bak", fileInfo.Extension);
                        if (!File.Exists(destFileName))
                        {
                            fileInfo.CopyTo(destFileName);
                        }

                        NfoFileManager nfoManager = new NfoFileManager(javFile.Key);
                        if (String.IsNullOrEmpty(nfoManager.ToString()))
                        {
                            Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                            continue;
                        }

                        // 从 nfo 文件获取元数据
                        var title = nfoManager.GetTitle();
                        var sortTitle = nfoManager.GetSortTitle();
                        var originalTitle = nfoManager.GetOriginalTitle();
                        var genres = nfoManager.GetGenres();
                        var tags = nfoManager.GetTags();

                        // 解析 javId
                        var javId = JavRecognizer.Parse(sortTitle) ?? JavRecognizer.Parse(originalTitle) ?? JavRecognizer.Parse(title);
                        if (javId == null)
                        {
                            logger.LogInformation($"修正元数据：---> {path} 获取 「{javFile.Key}」 番号异常，跳过执行");
                            Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                            continue;
                        }


                        Jav123Scraper jav123 = new Jav123Scraper(loggerFactory);


                        // 处理元数据
                        //DMM dmm = new DMM(loggerFactory);
                        //var url = dmm.Search(javId);
                        //var javVideo = await dmm.ParsePage(url) ?? await new JavCaptain(loggerFactory).ParsePage($"https://javcaptain.com/zh/{javId}");
                        //var url = jav123.Search(javId);
                        var javVideo = await jav123.SearchAndParseJavVideo(javId.Id) ?? await new JavCaptain(loggerFactory).ParsePage($"https://javcaptain.com/zh/{javId}");

                        if (javVideo == null)
                        {
                            Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                            continue;
                        }

                        // 定义标准视频元数据
                        var videoId = javId.Id.ToUpper();
                        var videoTitle = javVideo.Title.Trim();
                        var videoOriginalTitle = javVideo.OriginalTitle.Trim();
                        var videoSortTitle = videoId;
                        var videoActors = new List<string>();

                        // 整理元数据逻辑
                        bool hasChineseSubtitle = false;
                        bool hasUncensored = false;
                        // 从文件夹名称提取元数据
                        var folderName = fileInfo.Directory.Name;

                        if (VideoParser.IsValidTitleFormat(title))
                        {
                            // 从视频标题中提取视频信息
                            var videoInfo = VideoParser.ParseMetadataFormName(title);
                            if (videoInfo != null)
                            {
                                title = videoInfo.Title;
                            }
                            hasUncensored = videoInfo.IsUncensored;
                            hasChineseSubtitle = videoInfo.HasChineseSubtitle;
                        }


                        string pattern = @"\[([A-Za-z\-\d{3,4}]+)\]?(\[(無碼|中字|無碼破解|中字無碼破解)\]\s*)?\s*-\s*(.*)";


                        // 处理 title，去掉类似 "[IPZZ-485] [中字] " 的格式
                        title = Regex.Replace(title.Trim(), @"\[[^\]]*\]", "").Replace(javId.Id, "").Trim();
                        originalTitle = Regex.Replace(originalTitle.Trim(), @"\[[^\]]*\]", "").Replace(javId.Id, "").Trim();

                        // 如果 nfo 文件元数据不完整，则使用文件夹名称补全
                        //if (string.IsNullOrEmpty(originalTitle) || string.IsNullOrEmpty(javVideo.Title))
                        //{
                        //    title = folderTitle ?? javVideo.Title;
                        //    originalTitle = $"{videoId} {javVideo.Title}";
                        //}

                        // 比较 javVideo.Title 和 title
                        int matchingChars = title.Intersect(javVideo.Title).Count();
                        double similarityRatio = (double)matchingChars / Math.Max(title.Length, javVideo.Title.Length);

                        // 如果相似度过低，则以 title 为准
                        if (similarityRatio < 0.5) // 50% 的相似度阈值
                        {
                            videoTitle = title;
                            videoOriginalTitle = $"{videoId} {title}"; // 确保格式为 "IPZZ-485 标题"
                        }
                        else
                        {
                            videoOriginalTitle = $"{videoId} {originalTitle}";
                        }



                        // 根据不同情况设置 videoTitle、videoOriginalTitle、videoSortTitle、tags 和 genres
                        if (hasChineseSubtitle && hasUncensored)
                        {
                            videoTitle = $"[中字无码] {videoTitle}";
                            videoSortTitle = $"{videoId}-UC";
                            tags = new List<string> { "中文字幕", "無碼破解" }.Concat(tags).Distinct().ToList();
                            genres = new List<string> { "中文字幕", "無碼破解" }.Concat(genres).Distinct().ToList();
                        }
                        else if (hasChineseSubtitle)
                        {
                            videoTitle = $"[中字] {videoTitle}";
                            videoSortTitle = $"{videoId}-C";
                            tags = new List<string> { "中文字幕" }.Concat(tags).Distinct().ToList();
                            genres = new List<string> { "中文字幕" }.Concat(genres).Distinct().ToList();
                        }
                        else if (hasUncensored)
                        {
                            videoTitle = $"[无码] {videoTitle}";
                            videoSortTitle = $"{videoId}-U";
                            tags = new List<string> { "無碼破解" }.Concat(tags).Distinct().ToList();
                            genres = new List<string> { "無碼破解" }.Concat(genres).Distinct().ToList();
                        }
                        else
                        {
                            videoTitle = title;
                            videoSortTitle = videoId;
                        }
                        var tagList = new List<string>();
                        tagList = tags.Union(genres).ToList();
                        // 合并 tags 和 genres，去重并替换"中字"为"中文字幕"
                        tags = tagList.Select(t => t == "中字" ? "中文字幕" : t).Distinct().ToList();
                        genres = tagList.Select(g => g == "中字" ? "中文字幕" : g).Distinct().ToList();

                        if (javVideo.Samples != null && javVideo.Samples.Count > 0)
                        {
                            // 下载样品图片
                            for (int i = 0; i < javVideo.Samples.Count; i++)
                            {
                                if (i > 0)
                                {
                                    var sampleUrl = javVideo.Samples[i];
                                    // 处理缩略图地址，添加 "jp" 前缀
                                    //var fullImageUrl = Regex.Replace(sampleUrl, @"-(?=[^.]*$)", "jp-"); // 只在最后一个 "-" 前添加 "jp"
                                    var fullImageUrl = sampleUrl.Contains("jp-") ? sampleUrl : Regex.Replace(sampleUrl, @"-", "jp-"); // 只在最后一个 "-" 前添加 "jp"
                                    var fileName = $"backdrop{i}"; // 命名规则
                                    var savePath = Path.Combine(fileInfo.DirectoryName, fileName);
                                    await Downloader.DownloadJpegAsync(fullImageUrl, fileInfo.DirectoryName, fileName);
                                }
                            }
                        }

                        // 保存元数据
                        nfoManager.SaveMetadata(videoTitle, videoOriginalTitle, videoSortTitle, javId.Id, "", videoActors, genres, tags);

                        Console.WriteLine($"-----------------修正后的元数据-----------------");
                        Console.WriteLine($"fileName -> {fileInfo.Name}");
                        Console.WriteLine($"videoId -> {videoId}");
                        Console.WriteLine($"videoTitle -> {videoTitle}");
                        Console.WriteLine($"videoOriginalTitle -> {videoOriginalTitle}");
                        Console.WriteLine($"videoSortTitle -> {videoSortTitle}");
                        Console.WriteLine($"tags -> {String.Join(",", tags)}");
                        Console.WriteLine($"genres -> {String.Join(",", genres)}");
                        Console.WriteLine($"==============================================");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"处理文件 {javFile.Key} 时发生错误: {ex.Message}");
                        Console.WriteLine($"处理文件 {javFile.Key} 时发生错误: {ex.Message}");
                    }
                }
            }
        }

        private static void OrganizeFiles(JavVideo javVideo, string sourceFile, string destPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFile);
            var fileExt = Path.GetExtension(sourceFile);
            string actorsDir = string.Join(' ', javVideo.Actors.ToArray());
            string title = string.Format("[{0}] - [{1}] - [{2}]", javVideo.Date, javVideo.Number, javVideo.Title);
            string destDirPath = string.Format("{0}/{1}/{2}", destPath, actorsDir, title);
            string destFilePath = string.Format("{0}/{1}/{2}/{3}{4}", destPath, actorsDir, title, title, fileExt);
        }

        private static string BuilderNfo(JavVideo javVideo)
        {
            XmlDocument xml = NfoBuilder.GenerateNfo(javVideo);

            return xml.ToString();
        }

        public static void Director(string dir, Dictionary<string, string> keyValuePairs)
        {
            DirectoryInfo d = new(dir);
            FileInfo[] files = d.GetFiles();//文件
            DirectoryInfo[] directs = d.GetDirectories();//文件夹
            foreach (FileInfo f in files)
            {
                keyValuePairs.Add(f.FullName, f.Name);//添加文件名到列表中  
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                Director(dd.FullName, keyValuePairs);
            }
        }
    }

}
