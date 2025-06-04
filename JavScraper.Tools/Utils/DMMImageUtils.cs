using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JavScraper.Tools.Utils
{
    public static class DMMImageUtils
    {
        /// <summary>
        /// 将 DMM 番号缩略图 URL 转换为包含横版和竖版高清图的字典
        /// </summary>
        public static Dictionary<string, string> GetDMMBigImages(string url)
        {
            var result = new Dictionary<string, string>();
            Console.WriteLine($"原封面图地址 -> {url}");

            if (url.Contains("pics.dmm.co.jp/mono/movie/adult"))
            {
                result = ProcessMonoMovieUrl(url);
            }
            else
            {
                result = ProcessDigitalVideoUrl(url);
            }

            LogImageUrls(result);
            return result;
        }

        private static Dictionary<string, string> ProcessMonoMovieUrl(string url)
        {
            var result = new Dictionary<string, string>();
            var match = Regex.Match(url, @"adult/([a-zA-Z0-9]+)/\1p[sl]\.jpg");
            if (!match.Success)
            {
                Console.WriteLine("URL 格式不正确，无法提取番号");
                return result;
            }

            var newId = ProcessVideoId(match.Groups[1].Value);
            var baseUrl = $"https://awsimgsrc.dmm.co.jp/pics_dig/digital/video/{newId}/";
            
            AddImageUrls(result, baseUrl, newId);
            return result;
        }

        private static Dictionary<string, string> ProcessDigitalVideoUrl(string url)
        {
            var result = new Dictionary<string, string>();
            var match = Regex.Match(url, @"digital/video/([a-zA-Z0-9]+)/\1p[sl]\.jpg");
            if (!match.Success)
            {
                Console.WriteLine("URL 格式不正确，无法提取番号");
                return result;
            }

            var newId = ProcessVideoId(match.Groups[1].Value);
            var baseUrl = $"https://awsimgsrc.dmm.co.jp/pics_dig/digital/video/{newId}/";
            
            AddImageUrls(result, baseUrl, newId);
            return result;
        }

        private static string ProcessVideoId(string originalId)
        {
            var idMatch = Regex.Match(originalId, @"([a-zA-Z0-9]+?)(\d+)$");
            if (!idMatch.Success)
            {
                Console.WriteLine("番号格式不正确，无法拆分前缀和数字");
                return originalId;
            }

            string prefix = idMatch.Groups[1].Value;
            string number = idMatch.Groups[2].Value.PadLeft(5, '0');
            return prefix + number;
        }

        private static void AddImageUrls(Dictionary<string, string> result, string baseUrl, string newId)
        {
            result["poster"] = $"{baseUrl}{newId}ps.jpg";
            result["fanart"] = $"{baseUrl}{newId}pl.jpg";
        }

        private static void LogImageUrls(Dictionary<string, string> result)
        {
            foreach (var item in result)
            {
                Console.WriteLine($"{item.Key} -> {item.Value}");
            }
        }
    }
}