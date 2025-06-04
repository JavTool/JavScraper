using JavScraper.Tools.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.Tools.Tools
{
    public static class JavVideoHelper
    {
        public static string ToHalfWidth(string input)
        {
            return new string(input.Select(c =>
                c == 0x3000 ? (char)0x20 :
                (c >= 0xFF01 && c <= 0xFF5E) ? (char)(c - 0xFEE0) : c
            ).ToArray());
        }

        public static string CleanForSplit(string text, string? keyword = null)
        {
            if (!string.IsNullOrEmpty(keyword))
                text = text.Replace(keyword, "");
            // 去除中英文符号但保留空格
            text = Regex.Replace(text, @"[（）\(\)：:~～\-、・・\[\]【】「」『』]", "");
            text = ToHalfWidth(text).Trim();
            return text;
        }

        public static bool IsTitleMatchByParts(string videoTitle, string title, string? keyword = null)
        {
            var cleanedVideoTitle = CleanForSplit(videoTitle);
            var cleanedTitle = CleanForSplit(title, keyword);

            var videoParts = cleanedVideoTitle.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var titleParts = cleanedTitle.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // 遍历 videoParts，如果 titleParts 中有任何一个包含它（或等于），就移除
            for (int i = videoParts.Count - 1; i >= 0; i--)
            {
                string vp = videoParts[i];
                if (titleParts.Any(tp => tp.Contains(vp) || vp.Contains(tp)))
                {
                    videoParts.RemoveAt(i);
                }
            }

            // 如果 videoParts 为空，说明完全匹配
            return videoParts.Count == 0;
        }

        //public static string ToHalfWidth(string input)
        //{
        //    var sb = new StringBuilder();
        //    foreach (char c in input)
        //    {
        //        if (c == 0x3000)
        //            sb.Append((char)0x20);
        //        else if (c >= 0xFF01 && c <= 0xFF5E)
        //            sb.Append((char)(c - 0xFEE0));
        //        else
        //            sb.Append(c);
        //    }
        //    return sb.ToString();
        //}

        public static string CleanText(string text, string keyword = null)
        {
            if (!string.IsNullOrEmpty(keyword))
                text = text.Replace(keyword, "");

            text = Regex.Replace(text, @"[\(（][^()（）]*[\)）]", ""); // 括号内容
            text = Regex.Replace(text, @"[:：～~\-]", ""); // 特殊符号
            text = text.Replace(" ", "").Trim();
            return ToHalfWidth(text);
        }

        public static List<string> SplitToParts(string input)
        {
            var parts = Regex.Split(input, @"[~～\-\s、・]+")
                             .Where(p => !string.IsNullOrWhiteSpace(p))
                             .Select(s => CleanText(s))
                             .ToList();
            return parts;
        }

        //public static bool AllPartsInOrder(string source, string target)
        //{
        //    int index = 0;
        //    foreach (string part in SplitToParts(source))
        //    {
        //        index = target.IndexOf(part, index);
        //        if (index == -1)
        //            return false;
        //        index += part.Length;
        //    }
        //    return true;
        //}
        public static bool AllPartsInOrder(string source, string target)
        {
            var parts = SplitToParts(source);
            if (parts.Count == 0) return false;

            // 构造正则表达式：part1.*part2.*part3（按顺序允许中间插入任意字符）
            var pattern = string.Join(".*?", parts.Select(Regex.Escape));
            return Regex.IsMatch(target, pattern);
        }

        public static double CalculateSimilarity(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;
            int[,] dp = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++) dp[i, 0] = i;
            for (int j = 0; j <= len2; j++) dp[0, j] = j;

            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost
                    );
                }
            }

            int maxLen = Math.Max(len1, len2);
            return maxLen == 0 ? 1.0 : 1.0 - (double)dp[len1, len2] / maxLen;
        }

        public static bool IsTitleMatch(string videoTitle, string title, string keyword, double similarityThreshold = 0.9)
        {
            string cleanedVideoTitle = CleanText(videoTitle);
            string cleanedTitle = CleanText(title, keyword);

            // 1. 完整包含
            if (cleanedTitle.Contains(cleanedVideoTitle) || cleanedVideoTitle.Contains(cleanedTitle))
                return true;

            // 2. 分段匹配
            if (AllPartsInOrder(videoTitle, cleanedTitle))
                return true;
            if (AllPartsInOrder(title, videoTitle))
                return true;
            if (AllPartsInOrder(videoTitle, title))
                return true;
            // 3. 相似度兜底
            double similarity = CalculateSimilarity(cleanedVideoTitle, cleanedTitle);
            return similarity >= similarityThreshold;
        }

        ///// <summary>
        ///// Levenshtein 距离相似度计算
        ///// </summary>
        ///// <param name="s1"></param>
        ///// <param name="s2"></param>
        ///// <returns></returns>
        //public static double CalculateSimilarity(string s1, string s2)
        //{
        //    int len1 = s1.Length;
        //    int len2 = s2.Length;
        //    int[,] dp = new int[len1 + 1, len2 + 1];

        //    for (int i = 0; i <= len1; i++) dp[i, 0] = i;
        //    for (int j = 0; j <= len2; j++) dp[0, j] = j;

        //    for (int i = 1; i <= len1; i++)
        //    {
        //        for (int j = 1; j <= len2; j++)
        //        {
        //            int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
        //            dp[i, j] = Math.Min(
        //                Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
        //                dp[i - 1, j - 1] + cost
        //            );
        //        }
        //    }

        //    int maxLen = Math.Max(len1, len2);
        //    return maxLen == 0 ? 1.0 : 1.0 - (double)dp[len1, len2] / maxLen;
        //}

        //// 移除 keyword 和括号内文字，清理特殊符号
        //public static string CleanTitle(string title, string keyword)
        //{
        //    string result = title;

        //    // 移除 keyword
        //    if (!string.IsNullOrEmpty(keyword))
        //    {
        //        result = result.Replace(keyword, "");
        //    }

        //    // 移除中英文括号内的内容
        //    result = Regex.Replace(result, @"[\(（][^()（）]*[\)）]", "");

        //    // 移除冒号、波浪线等无关字符
        //    result = Regex.Replace(result, @"[:：～~\-]", "");

        //    // 移除空格
        //    result = result.Replace(" ", "").Trim();

        //    return result;
        //}


        /// <summary>
        /// 将 b 中与 a 不同的字段值赋给 a
        /// </summary>
        public static void SyncDifferences(JavVideo a, JavVideo b)
        {
            if (a == null || b == null) return;

            // 原始标题
            if (!AreValuesEqual(a.OriginalTitle, b.OriginalTitle))
                a.OriginalTitle = b.OriginalTitle;

            // 字符串类型字段
            SyncStringField(a.Plot, b.Plot, (x, y) => a.Plot = y);
            SyncStringField(a.Director, b.Director, (x, y) => a.Director = y);
            SyncStringField(a.Runtime, b.Runtime, (x, y) => a.Runtime = y);
            SyncStringField(a.Studio, b.Studio, (x, y) => a.Studio = y);
            SyncStringField(a.Maker, b.Maker, (x, y) => a.Maker = y);
            SyncStringField(a.Set, b.Set, (x, y) => a.Set = y);
            SyncStringField(a.Url, b.Url, (x, y) => a.Url = y);
            SyncStringField(a.Number, b.Number, (x, y) => a.Number = y);
            SyncStringField(a.Title, b.Title, (x, y) => a.Title = y);
            SyncStringField(a.SortTitle, b.SortTitle, (x, y) => a.SortTitle = y);
            SyncStringField(a.Cover, b.Cover, (x, y) => a.Cover = y);
            SyncStringField(a.Date, b.Date, (x, y) => a.Date = y);

            // 列表类型字段
            SyncListField(a.Tags, b.Tags, (x, y) => a.Tags = y?.ToList());
            SyncListField(a.Genres, b.Genres, (x, y) => a.Genres = y?.ToList());
            SyncListField(a.Actors, b.Actors, (x, y) => a.Actors = y?.ToList());
            SyncListField(a.Samples, b.Samples, (x, y) => a.Samples = y?.ToList());
        }

        //-------------------------------------
        // 辅助方法
        //-------------------------------------

        /// <summary>
        /// 比较字符串字段
        /// </summary>
        private static void SyncStringField(string aVal, string bVal, Action<string, string> setter)
        {
            if (!AreValuesEqual(aVal, bVal))
                setter(aVal, bVal);
        }

        /// <summary>
        /// 比较列表字段
        /// </summary>
        private static void SyncListField(List<string> aList, List<string> bList, Action<List<string>, List<string>> setter)
        {
            if (!AreListsEqual(aList, bList))
                setter(aList, bList?.ToList());
        }

        /// <summary>
        /// 判断两个值是否相等（支持 null）
        /// </summary>
        private static bool AreValuesEqual(string x, string y)
        {
            return string.Equals(x, y, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断两个列表是否相等（元素相同且顺序无关）
        /// </summary>
        private static bool AreListsEqual(List<string> list1, List<string> list2)
        {
            if (ReferenceEquals(list1, list2)) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            // 将列表排序后比较元素
            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }
    }
}
