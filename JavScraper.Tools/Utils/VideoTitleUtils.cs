using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JavScraper.Tools.Utils
{
    public static class VideoTitleUtils
    {
        public static (string Title, string SortTitle, List<string> Tags) ProcessVideoTitle(
            string videoId,
            string originalTitle,
            bool hasChineseSubtitle,
            bool hasUncensored,
            List<string> tagList)
        {
            string title = originalTitle;
            string sortTitle = videoId;
            
            if (hasChineseSubtitle && hasUncensored)
            {
                title = $"[中字无码] {title}";
                sortTitle = $"{videoId}-UC";
                tagList = new List<string> { "中文字幕", "无码破解" }.Concat(tagList).Distinct().ToList();
            }
            else if (hasChineseSubtitle || tagList.Contains("中字"))
            {
                title = $"[中字] {title}";
                sortTitle = $"{videoId}-C";
                tagList = new List<string> { "中文字幕" }.Concat(tagList).Distinct().ToList();
            }
            else if (hasUncensored)
            {
                title = $"[无码] {title}";
                sortTitle = $"{videoId}-U";
                tagList = new List<string> { "无码破解" }.Concat(tagList).Distinct().ToList();
            }

            return (title, sortTitle, tagList);
        }

        public static List<string> CleanupTags(IEnumerable<string> tags)
        {
            var regex = new Regex(@"\b(?:[1-9]\d{2,}p|4k|24|30|50|60|120|240|29\.97|\d+\.?\d*fps)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            return tags.Select(item => item == "中字" ? "中文字幕" : item)
                      .Select(item => item == "無碼破解" ? "无码破解" : item)
                      .Select(item => item == "無碼流出" ? "无码流出" : item)
                      .Distinct()
                      .Where(tag => !regex.IsMatch(tag))
                      .Where(t => !t.Contains("/") && !t.Contains("(") && t.Length <= 10)
                      .Where(t => !t.Contains("店長") && !t.Contains("上映中") && !t.Contains("動画"))
                      .ToList();
        }
    }
}