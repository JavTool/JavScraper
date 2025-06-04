using JavScraper.App.Entities;
using JavScraper.App.Scrapers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.App
{
    public class JavOrganization
    {
        public static void OrganizeVideo(string sourcePath, string destPath)
        {

            var files = Directory.GetFiles(sourcePath);

            foreach (var file in files)
            {

                var fileName = Path.GetFileNameWithoutExtension(file);
                var fileExt = Path.GetExtension(file);
                if (fileExt.ToLower().Contains(".ts") || fileExt.ToLower().Contains(".mp4"))
                {
                    var nameArray = fileName.Split(' ');
                    var dirName = string.Empty;
                    if (nameArray.Length > 1)
                    {
                        dirName = string.Format("{0} {1}", nameArray[0], nameArray[1]);
                    }
                    string destDirPath = string.Format("{0}/{1}", destPath, dirName);
                    if (!string.IsNullOrEmpty(dirName))
                    {
                        Console.WriteLine(destDirPath);
                    }
                }
            }
        }


        public static void OrganizeGallery(string sourcePath, string destPath)
        {
            var directories = Directory.EnumerateDirectories(sourcePath);
            foreach (var directory in directories)
            {
                var files = Directory.EnumerateFiles(directory);
                if (files.ToList().Count > 0)
                {
                    var directoryName = Path.GetFileNameWithoutExtension(directory);
                    var fileName = JavRecognizer.Parse(directoryName);
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file).ToLower().Contains(".zip") || Path.GetExtension(file).ToLower().Contains(".rar"))
                        {
                            Console.WriteLine(Path.GetFileNameWithoutExtension(file));
                            var destName = string.Format("{0}/{1}{2}", destPath, fileName, Path.GetExtension(file));
                            Console.WriteLine(destName);
                            File.Move(file, destName);
                        }

                    }
                    Console.WriteLine(fileName);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        public static void OrganizeDirectory(string sourcePath)
        {
            Dictionary<string, string> dirDict = new Dictionary<string, string>();

            FindAllDir(sourcePath, dirDict);

            foreach (var dir in dirDict)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir.Key);

                string dirName = dir.Value.Replace("(", "[").Replace(")", "]");
                string destFullName = string.Format("{0}/{1}", dirInfo.Parent.FullName, dirName);
                Directory.Move(dirInfo.FullName, destFullName);
                Console.WriteLine(dir.Value);
            }
        }

        /// <summary>
        /// 获取全部目录，包含子目录。
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="dirDict"></param>
        public static void FindAllDir(string dirPath, Dictionary<string, string> dirDict)
        {
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            DirectoryInfo[] dirs = dir.GetDirectories(); // 获取所有文件夹

            // 获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dirInfo in dirs)
            {
                FindAllDir(dirInfo.FullName, dirDict);
                dirDict.Add(dirInfo.FullName, dirInfo.Name);
            }
        }

        /// <summary>
        /// 获取全部文件，包含子目录中的文件。
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="fileDict"></param>
        public static void FindAllFile(string dirPath, Dictionary<string, string> fileDict)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            // 获取当前目录所有文件
            FileInfo[] files = dirInfo.GetFiles();
            // 获取当前目录所有文件夹
            DirectoryInfo[] dirs = dirInfo.GetDirectories();
            foreach (FileInfo file in files)
            {
                // 添加文件名到列表中
                fileDict.Add(file.FullName, file.Name);
            }
            // 递归遍历子文件夹内的文件列表
            foreach (DirectoryInfo dir in dirs)
            {
                FindAllFile(dir.FullName, fileDict);
            }
        }

        /// <summary>
        /// 判断目标是文件夹还是目录（目录包括磁盘）。
        /// </summary>
        /// <param name="filepath">文件名。</param>
        /// <returns></returns>
        public static bool IsDirectory(string filepath)
        {
            FileInfo fi = new FileInfo(filepath);
            if ((fi.Attributes & FileAttributes.Directory) != 0)
                return true;
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取 JavBus 媒体信息。
        /// </summary>
        /// <param name="loggerFactory">一个 <paramref name="loggerFactory"/> 对象实例。</param>
        /// <param name="sourcePath">文件路径。</param>
        /// <returns></returns>
        public static async Task JavBusOrganization(ILoggerFactory loggerFactory, string sourcePath)
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
        public static async Task JavDBOrganization(ILoggerFactory loggerFactory, string sourcePath)
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

        private static void OrganizeFiles(JavVideo javVideo, string sourceFile, string destPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(sourceFile);
            var fileExt = Path.GetExtension(sourceFile);
            string actorsDir = string.Join(' ', javVideo.Actors.ToArray());
            string title = string.Format("[{0}] - [{1}] - [{2}]", javVideo.Date, javVideo.Number, javVideo.Title);
            string destDirPath = string.Format("{0}/{1}/{2}", destPath, actorsDir, title);
            string destFilePath = string.Format("{0}/{1}/{2}/{3}{4}", destPath, actorsDir, title, title, fileExt);
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

        /// <summary>
        /// 获取单个番号的信息
        /// </summary>
        /// <param name="number">番号</param>
        /// <returns>番号信息</returns>
        public static async Task<JavInfo> GetJavInfo(string number)
        {
            try
            {
                var url = $"https://www.javbus.com/{number}";
                var javBus = new JavBus(null); // 或者传入 loggerFactory

                var javVideo = await javBus.ParsePage(url);
                if (javVideo == null)
                    return null;

                return new JavInfo
                {
                    Number = javVideo.Number,
                    Title = javVideo.Title,
                    ReleaseDate = javVideo.Date,
                    Studio = javVideo.Studio,
                    CoverUrl = $"https://www.javbus.com/{javVideo.Cover}",
                    Actresses = javVideo.Actors?.ToList(),
                    Genres = javVideo.Genres?.ToList(),
                    GalleryUrls = javVideo.Samples?.Select(s => 
                        Regex.IsMatch(s, @"^http(s)?://") ? s : $"https://www.javbus.com/{s}")
                        .ToList()
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            return LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Information);
            });
        }
    }
}
