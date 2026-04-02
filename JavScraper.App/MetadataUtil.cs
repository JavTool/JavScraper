using JavScraper.App.Models;
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
    internal class MetadataUtil
    {
        private const string HiddenFileLog = "当前 nfo 文件已隐藏，跳过！";
        private const string AlreadyFixedLog = "当前视频已修正，跳过！";
        private const string MetadataErrorLog = "获取 「{0}」 番号异常，跳过执行。";
        private const string SubtitleTag = "[中字]";
        private const string UncensoredTag1 = "無碼流出";
        private const string UncensoredTag2 = "無碼破解";
        private const string SubtitleTag1 = "中字";
        private const string SubtitleTag2 = "中文字幕";

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

     
        private static void LogAndPrint(ILogger logger, string message)
        {
            logger.LogInformation(message);
            Console.WriteLine(message);
        }

        private static (string Title, string SortTitle, string OriginalTitle, List<string> Genres, List<string> Tags) GetNfoMetadata(NfoDocument nfoManager)
        {
            var title = nfoManager.GetTitle();
            var sortTitle = nfoManager.GetSortTitle();
            var originalTitle = nfoManager.GetOriginalTitle();
            var genres = nfoManager.GetGenres().ToList();
            var tags = nfoManager.GetTags().ToList();
            return (title, sortTitle, originalTitle, genres, tags);
        }

        private static void PrintMetadata(string javFile, string videoTitle, string videoOriginalTitle, string videoSortTitle, List<string> tags, List<string> genres)
        {
            Console.WriteLine($"javFile -> {javFile}");
            Console.WriteLine($"videoTitle -> {videoTitle}");
            Console.WriteLine($"videoOriginalTitle -> {videoOriginalTitle}");
            Console.WriteLine($"videoSortTitle -> {videoSortTitle}");
            Console.WriteLine($"tags -> {string.Join(",", tags)}");
            Console.WriteLine($"genres -> {string.Join(",", genres)}");
        }

        private static JavId GetJavId(string sortTitle, string originalTitle, string title)
        {
            return JavRecognizer.Parse(sortTitle) ?? JavRecognizer.Parse(originalTitle) ?? JavRecognizer.Parse(title);
        }

        private static async Task<JavVideo> GetJavVideoAsync(ILoggerFactory loggerFactory, JavId javId)
        {
            var dmm = new DMM(loggerFactory);
            var url = dmm.GetPageUrl(javId);
            var javVideo = await dmm.ParsePage(url);

            if (javVideo != null) return javVideo;

            var javCaptain = new JavCaptain(loggerFactory);
            var javCaptainUrl = $"https://javcaptain.com/zh/{javId}";
            return await javCaptain.ParsePage(javCaptainUrl);
        }

        private static (string VideoTitle, string VideoOriginalTitle, string VideoSortTitle, bool IsChinese) FixVideoMetadata(string videoId, JavVideo javVideo, string title, string originalTitle, List<string> tags, List<string> genres)
        {
            var isChinese = false;
            var videoSortTitle = videoId;

            if (title.Equals(originalTitle))
            {
                videoSortTitle = GetVideoSortTitle(videoId, tags, genres, out isChinese);
            }
            else if (!string.IsNullOrEmpty(originalTitle) && (videoId + "-C").Equals(originalTitle.Split(".")[0], StringComparison.OrdinalIgnoreCase))
            {
                videoSortTitle = GetVideoSortTitle(videoId, tags, genres, out isChinese);
            }

            if (!genres.Any()) genres = javVideo.Genres.ToList();
            if (!tags.Any()) tags = genres.ToList();
            if (isChinese)
            {
                tags.Add(SubtitleTag1);
                genres.Add(SubtitleTag1);
            }

            var videoTitle = isChinese ? $"{SubtitleTag} {javVideo.Title}" : javVideo.Title;
            var videoOriginalTitle = $"{videoId} {javVideo.OriginalTitle}";
            return (videoTitle, videoOriginalTitle, videoSortTitle, isChinese);
        }

        private static string GetVideoSortTitle(string videoId, List<string> tags, List<string> genres, out bool isChinese)
        {
            var videoSortTitle = videoId;
            isChinese = false;

            if (tags.Contains(UncensoredTag1) || genres.Contains(UncensoredTag1) || tags.Contains(UncensoredTag2) || genres.Contains(UncensoredTag2))
            {
                if (tags.Contains(SubtitleTag1) || genres.Contains(SubtitleTag1) || tags.Contains(SubtitleTag2) || genres.Contains(SubtitleTag2))
                {
                    videoSortTitle = $"{videoId}-UC";
                    isChinese = true;
                }
                else
                {
                    videoSortTitle = $"{videoId}-U";
                }
            }
            else if (tags.Contains(SubtitleTag1) || genres.Contains(SubtitleTag1) || tags.Contains(SubtitleTag2) || genres.Contains(SubtitleTag2))
            {
                videoSortTitle = $"{videoId}-C";
                isChinese = true;
            }

            return videoSortTitle;
        }

        private static void FixActors(FileInfo fileInfo, NfoDocument nfoManager, JavVideo javVideo)
        {
            if (fileInfo.Name.Contains("-CD") || fileInfo.Name.Contains("- CD") && javVideo.Actors.Any())
            {
                var actors = javVideo.Actors.Select(actor => Regex.Replace(actor, @"\s*\（.*?\）\s*", string.Empty)).ToList();
                Console.WriteLine($"fix actors -> {string.Join(",", actors)}");
                nfoManager.SetActors(actors);
            }
        }
    }
}
