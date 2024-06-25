using AngleSharp;
using HtmlAgilityPack;
//using JavScraper.Domain;
using JavScraper.Tools.Entities;
using JavScraper.Tools.Scrapers;
using MagicFile.Test;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
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
                        await FixMetadataAsync(loggerFactory, path);
                        Console.WriteLine("执行结束");
                    }
                }
                else if (command == "5")
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
            Console.WriteLine("5、快速裁切封面");
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

                            var saveFileName = Downloader.Download(coverUrl, fileDir, javId);
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
                                    var saveName = Downloader.Download(sampleUrl, fileDir, string.Format("Sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
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

                                var saveFileName = Downloader.Download(coverUrl, fileDir, javId);
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
                                            var saveName = Downloader.Download(sample, fileDir, string.Format("sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
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

                                var saveFileName = Downloader.Download(coverUrl, fileDir, javId);
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
                                            var saveName = Downloader.Download(sample, fileDir, string.Format("sample-{0}", (i + 1).ToString().PadLeft(3, '0')));
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
                    FileInfo fileInfo = new FileInfo(javFile.Key);


                    FileAttributes attributes = File.GetAttributes(javFile.Key);
                    if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        logger.LogInformation($"当前 nfo 文件已隐藏，跳过！");
                        Console.WriteLine($"当前 nfo 文件已隐藏，跳过！");
                    }

                    var destFileName = string.Format(@"{0}\{1}{2}", fileInfo.DirectoryName, Path.GetFileNameWithoutExtension(javFile.Key) + ".bak", fileInfo.Extension);
                    if (!File.Exists(destFileName))
                    {
                        fileInfo.CopyTo(destFileName);
                    }
                    else
                    {
                        logger.LogInformation($"当前视频已修正，跳过！");
                        Console.WriteLine($"当前视频已修正，跳过！");
                        continue;
                    }
                    NfoFileManager nfoManager = new NfoFileManager(javFile.Key);
                    if (String.IsNullOrEmpty(nfoManager.ToString()))
                    {
                        Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                        continue;
                    }
                    var isChinese = false;
                    var isJavCaptainTitle = false;
                    var isUncensored = false;
                    var title = nfoManager.GetTitle();
                    var sortTitle = nfoManager.GetSortTitle();
                    var originalTitle = nfoManager.GetOriginalTitle();

                    // 标签和类型
                    var genres = nfoManager.GetGenres();
                    var tags = nfoManager.GetTags();
                    Console.WriteLine($"-----------------修正前的元数据-----------------");
                    Console.WriteLine($"javFile -> {javFile.Value}");
                    Console.WriteLine($"videoTitle -> {title}");
                    Console.WriteLine($"videoOriginalTitle -> {originalTitle}");
                    Console.WriteLine($"videoSortTitle -> {sortTitle}");
                    Console.WriteLine($"tags -> {String.Join(",", tags)}");
                    Console.WriteLine($"genres -> {String.Join(",", genres)}");

                    var code = String.IsNullOrEmpty(sortTitle) ? originalTitle : sortTitle;

                    var javId = JavRecognizer.Parse(sortTitle);
                    if (javId == null)
                    {
                        javId = JavRecognizer.Parse(originalTitle);
                    }
                    if (javId == null)
                    {
                        javId = JavRecognizer.Parse(title);
                    }

                    if (javId == null)
                    {
                        logger.LogInformation($"修正元数据：---> {path} 获取 「{javFile.Key}」 番号异常，跳过执行");

                        Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                        continue;
                    }
                    DMM dmm = new DMM(loggerFactory);

                    var url = dmm.GetPageUrl(javId);
                    var javVideo = await dmm.ParsePage(url);

                    if (javId == null || javVideo == null)
                    {
                        JavCaptain javCaptain = new JavCaptain(loggerFactory);
                        var javCaptainUrl = string.Format("https://javcaptain.com/zh/{0}", javId);
                        javVideo = await javCaptain.ParsePage(javCaptainUrl);
                        if (javVideo == null)
                        {
                            //logger.LogInformation($"修正元数据：---> {path} 获取 「{javFile.Key}」 番号异常，跳过执行");

                            Console.WriteLine($"获取 「{javFile.Key}」 番号异常，跳过执行。");
                            continue;
                        }
                        isJavCaptainTitle = true;
                    }

                    // 定义标准视频元数据
                    var videoId = javId.Id.ToUpper();
                    var videoTitle = javVideo.Title;
                    var videoOriginalTitle = String.Format("{0} {1}", videoId, javVideo.OriginalTitle);
                    var videoSortTitle = videoId;

                    // 判断 标题 和 原标题 是否一致
                    if (title.Equals(originalTitle))
                    {
                        if (tags.Contains("無碼流出") || genres.Contains("無碼流出") || tags.Contains("無碼破解") || genres.Contains("無碼破解"))
                        {
                            if (originalTitle.Contains("[中字]") || title.Contains("[中字]") || tags.Contains("中字") || genres.Contains("中字") || tags.Contains("中文字幕") || genres.Contains("中文字幕"))
                            {
                                videoSortTitle = String.Format("{0}-UC", videoId);
                                isChinese = true;
                            }
                            else
                            {
                                videoSortTitle = String.Format("{0}-U", videoId);
                            }
                            isUncensored = true;
                        }
                        else
                        {
                            if (originalTitle.Contains("[中字]") || title.Contains("[中字]") || tags.Contains("中字") || genres.Contains("中字") || tags.Contains("中文字幕") || genres.Contains("中文字幕"))
                            {
                                videoSortTitle = String.Format("{0}-C", videoId);
                                isChinese = true;
                            }
                            else
                            {
                                videoSortTitle = String.Format("{0}", videoId);
                            }
                        }
                    }
                    else
                    {
                        // abp-843-C.mp4
                        if (!String.IsNullOrEmpty(originalTitle) && ((videoId.ToUpper() + "-C").Equals(originalTitle.Split(".")[0]?.ToUpper())) || (videoId.ToUpper() + "-UC").ToUpper().Equals(originalTitle.Split(".")[0]?.ToUpper()))
                        {
                            if (title.Contains("[中字]") || originalTitle.Contains("[中字]"))
                            {
                                videoSortTitle = String.Format("{0}-C", videoId);
                            }
                            else
                            {
                                videoSortTitle = String.Format("{0}-C", videoId);
                            }
                            isChinese = true;
                        }

                        if (tags.Contains("無碼流出") || genres.Contains("無碼流出") || tags.Contains("無碼破解") || genres.Contains("無碼破解"))
                        {
                            if (originalTitle.Contains("[中字]") || title.Contains("[中字]") || tags.Contains("中字") || genres.Contains("中字") || tags.Contains("中文字幕") || genres.Contains("中文字幕"))
                            {
                                videoSortTitle = String.Format("{0}-UC", videoId);
                                isChinese = true;
                            }
                            else
                            {
                                videoSortTitle = String.Format("{0}-U", videoId);
                            }
                            isUncensored = true;
                        }
                        else
                        {
                            if (originalTitle.Contains("[中字]") || title.Contains("[中字]") || tags.Contains("中字") || genres.Contains("中字") || tags.Contains("中文字幕") || genres.Contains("中文字幕"))
                            {
                                videoSortTitle = String.Format("{0}-C", videoId);
                                isChinese = true;
                            }
                            else
                            {
                                videoSortTitle = String.Format("{0}", videoId);
                            }
                        }
                    }
                    if (!genres.Any())
                    {
                        genres = javVideo.Genres;
                    }

                    if (!tags.Any())
                    {
                        tags = genres;
                    }
                    if (isChinese)
                    {
                        videoTitle = $"[中字] {javVideo.Title}";
                        if (!tags.Contains("中字"))
                        {
                            tags.Add("中字");
                        }
                        if (!genres.Contains("中字"))
                        {
                            genres.Add("中字");
                        }
                        if (isUncensored)
                        {
                            videoSortTitle = String.Format("{0}-UC", videoId);
                        }
                        else
                        {
                            videoSortTitle = String.Format("{0}-C", videoId);
                        }
                    }
                    //if (isUncensored) {
                    //    if (!tags.Contains("無碼"))
                    //    {
                    //        tags.Add("無碼");
                    //    }
                    //    if (!genres.Contains("無碼"))
                    //    {
                    //        genres.Add("無碼");
                    //    }
                    //}

                    if (!title.Equals(videoTitle))
                    {
                        if (title.Replace("[中字] ", "").Replace("[中字] ", "").Equals(javVideo.Title))
                        {
                            //nfoManager.SetTitle(videoTitle);
                            title = videoTitle;
                        }
                        else
                        {
                            //nfoManager.SetTitle($"[中字] {title.Replace("[中字] ", "").Replace("[中字]", "")}");
                            title = $"[中字] {title.Replace("[中字] ", "").Replace("[中字]", "")}";
                        }
                        if (isJavCaptainTitle)
                        {
                            title = videoTitle;
                        }
                    }



                    // 保存
                    nfoManager.SaveMetadata(videoTitle, videoOriginalTitle, videoSortTitle, genres, tags);

                    if (fileInfo.Name.Contains("-CD") || fileInfo.Name.Contains("- CD") && javVideo.Actors.Any())
                    {
                        var actors = new List<string>();

                        foreach (var actor in javVideo.Actors)
                        {
                            Match match = Regex.Match(actor, @"\s*\（.*?\）\s*");

                            // 如果找到匹配项，返回括号中的内容
                            if (match.Success)
                            {
                                actors.Add(Regex.Replace(actor, @"\s*\（.*?\）\s*", string.Empty));
                            }
                            else
                            {
                                actors.Add(actor);
                            }
                        }
                        Console.WriteLine($"fix actors -> {String.Join(",", actors)}");
                        nfoManager.SetActors(actors);
                    }

                    Console.WriteLine($"-----------------修正后的元数据-----------------");
                    Console.WriteLine($"videoId -> {videoId}");
                    Console.WriteLine($"videoTitle -> {videoTitle}");
                    Console.WriteLine($"videoOriginalTitle -> {videoOriginalTitle}");
                    Console.WriteLine($"videoSortTitle -> {videoSortTitle}");
                    Console.WriteLine($"tags -> {String.Join(",", tags)}");
                    Console.WriteLine($"genres -> {String.Join(",", genres)}");

                    Console.WriteLine($"==============================================");
                }

            }
            //Console.WriteLine("执行结束");
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
