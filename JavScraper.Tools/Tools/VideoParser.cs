using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.Tools.Tools
{

    /// <summary>
    /// 视频信息实体类
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// 拍摄日期（格式：yyyy-MM-dd）
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 产品编号（格式：ABC-123）
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// 视频标题（固定格式：标题）
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 原始标题（固定格式：番号 标题）
        /// </summary>
        public string OriginalTitle { get; set; }

        /// <summary>
        /// 排序标题（固定格式：番号、番号-U、番号-C、番号-UC）
        /// </summary>
        public string SortTitle { get; set; }

        /// <summary>
        /// 显示标题（媒体库显示使用）
        /// </summary>
        public string DisplayTitle { get; set; }

        /// <summary>
        /// 是否包含中文字幕
        /// </summary>
        public bool HasChineseSubtitle { get; set; }

        /// <summary>
        /// 是否为无修正/无码版本
        /// </summary>
        public bool IsUncensored { get; set; }
    }

    /// <summary>
    /// 视频信息解析器
    /// </summary>
    public class VideoParser
    {
        // 综合正则表达式（结构验证）
        const string pattern = @"(?:\[(無碼|中字無碼|中字)\]\s*-\s*)?\[(\d{4}-\d{2}-\d{2})\]\s*-\s*\[([A-Za-z0-9\-]+)\]\s*-\s*(.*)";

        /// <summary>
        /// 验证是否 “格式示例：[番号] [标签] [标题片段1] [标题片段2]” 标题格式
        /// </summary>
        /// <param name="input">格式示例：[番号] [标签] [标题片段1] [标题片段2]</param>
        /// <returns></returns>
        public static bool IsValidTitleFormat(string input)
        {
            // 新的正则表达式，支持更多格式，匹配没有 "中字" 或 "無碼" 的情况
            string pattern = @"\[(?<number>[A-Za-z{3,4}\-\d{3,4}]+)\]\s*\[(?<tags>中字無碼破解|無碼破解|中字)\]?\s*(?<title>.*)";
            // \[(?<number>(?:[A-Za-z\-\d{3,4}]*)+)\]?(\[(?<tags>無碼|中字|無碼破解|中字無碼破解)\]\s*)?\s*-\s*(?<title>.*)
            // 禁用多行和忽略空格选项
            return Regex.IsMatch(
                input,
                pattern,
                RegexOptions.IgnorePatternWhitespace // 仅忽略表达式中的说明性空格
            );// && ValidateContent(input);
        }

        /// <summary>
        /// 解析视频信息字符串
        /// </summary>
        /// <param name="input">格式示例：[标签] - [日期] - [番号] - [标题片段1] [标题片段2]</param>
        /// <returns>结构化视频信息</returns>
        public static VideoInfo ParseMetadataFormTitle(string input)
        {
            var info = new VideoInfo();

            string pattern = @"\[(?<number>[A-Za-z{3,4}\-\d{3,4}]+)\]\s*\[(?<tags>中字無碼破解|無碼破解|中字)\]?\s*(?<title>.*)";

            // 使用正则表达式提取所有方括号内的内容
            var match = Regex.Match(input, pattern);

            // 如果匹配成功，则提取各个字段
            if (match.Success)
            {
                string tags = match.Groups["tags"].Value;  // 标签
                info.Number = match.Groups["number"].Value;  // 番号
                info.Title = match.Groups["title"].Value;  // 标题

                if (!string.IsNullOrEmpty(tags))
                {
                    switch (tags)
                    {
                        case "中字無碼破解":
                            info.HasChineseSubtitle = true;
                            info.IsUncensored = true;
                            info.SortTitle = $"{info.Number}-UC";
                            break;
                        case "中字":
                            info.HasChineseSubtitle = true;
                            info.SortTitle = $"{info.Number}-C";
                            break;
                        case "無碼破解":
                            info.IsUncensored = true;
                            info.SortTitle = $"{info.Number}-U";
                            break;
                    }
                }

                // 验证番号格式（大写字母+横线+数字）
                if (!Regex.IsMatch(info.Number, @"^[A-Z]+-\d+$"))
                    info.Number = null; // 无效番号置空
            }
            else
            {
                Console.WriteLine("未匹配到信息。");
            }

            return info;
        }


        /// <summary>
        /// 验证是否 “[标签] - [日期] - [番号] - [标题片段1] [标题片段2]” 文件名称格式
        /// </summary>
        /// <param name="input">格式示例：[标签] - [日期] - [番号] - [标题片段1] [标题片段2]</param>
        /// <returns></returns>
        public static bool IsValidNameFormat(string input)
        {
            // 支持更多格式，匹配没有 "中字" 或 "無碼" 的情况
            string pattern = @"(?:\[(?<tags>無碼|中字無碼破解|中字)\]\s*-\s*)?\[(?<date>\d{4}-\d{2}-\d{2})\]\s*-\s*\[(?<number>[A-Za-z0-9\-]+)\]\s*-\s*\[(?<title>.*)\]";

            // 禁用多行和忽略空格选项
            return Regex.IsMatch(
                input,
                pattern,
                RegexOptions.IgnorePatternWhitespace // 仅忽略表达式中的说明性空格
            );// && ValidateContent(input);
        }

        /// <summary>
        /// 解析视频信息字符串
        /// </summary>
        /// <param name="input">格式示例：[标签] - [日期] - [番号] - [标题片段1] [标题片段2]</param>
        /// <returns>结构化视频信息</returns>
        public static VideoInfo ParseMetadataFormName(string input)
        {
            var info = new VideoInfo();
            // (?:\[(無碼|中字無碼|中字)\]\s*-\s*)?\[(\d{4}-\d{2}-\d{2})\]\s*-\s*\[([A-Za-z0-9\-]+)\]\s*-\s*\[(.*)\]
            // 新的正则表达式，支持更多格式，匹配没有 "中字" 或 "無碼" 的情况
            string pattern = @"(?:\[(?<tags>無碼|中字無碼破解|中字)\]\s*-\s*)?\[(?<date>\d{4}-\d{2}-\d{2})\]\s*-\s*\[(?<number>[A-Za-z0-9\-]+)\]\s*-\s*\[(?<title>.*)\]";

            // 使用正则表达式提取所有方括号内的内容
            var match = Regex.Match(input, pattern);

            // 如果匹配成功，则提取各个字段
            if (match.Success)
            {
                // 提取标签、日期、番号、标题
                string tags = match.Groups[1].Value;
                info.Date = match.Groups[2].Value;
                info.Number = match.Groups[3].Value;
                info.Title = match.Groups[4].Value;

                // 第一阶段：处理特殊标签
                if (!string.IsNullOrEmpty(tags))
                {
                    switch (tags)
                    {
                        case "中字無碼":
                            info.HasChineseSubtitle = true;
                            info.IsUncensored = true;
                            info.SortTitle = $"{info.Number}-UC";
                            break;
                        case "中字":
                            info.HasChineseSubtitle = true;
                            info.SortTitle = $"{info.Number}-C";
                            break;
                        case "無碼":
                            info.IsUncensored = true;
                            info.SortTitle = $"{info.Number}-U";
                            break;
                        default:
                            info.SortTitle = $"{info.Number}";
                            break;

                    }
                }
                else
                {
                    info.SortTitle = $"{info.Number}";
                }

                // 第三阶段：格式验证
                // 验证日期是否符合标准格式
                if (!IsValidDate(info.Date))
                    info.Date = null; // 无效日期置空

                // 验证番号格式（大写字母+横线+数字）
                if (!Regex.IsMatch(info.Number, @"^[A-Z]+-\d+$"))
                    info.Number = null; // 无效番号置空
            }
            else
            {
                Console.WriteLine("未匹配到信息。");
            }

            return info;
        }
        /// <summary>
        /// 二次验证日期和番号
        /// </summary>
        private static bool ValidateContent(string input)
        {
            var parts = Regex.Matches(input, @"(?:\[(無碼|中字無碼|中字)\]\s*-\s*)?\[(\d{4}-\d{2}-\d{2})\]\s*-\s*\[([A-Za-z0-9\-]+)\]\s*-\s*(.*)")
                            .Cast<Match>()
                            .Select(m => m.Groups[1].Value)
                            .ToList();

            int index = parts[0] switch
            {
                "中字無碼" or "中字" or "無碼" => 1,
                _ => 0
            };

            return IsValidDate(parts[index]) &&
                   Regex.IsMatch(parts[index + 1], @"^[A-Z]+-\d+$");
        }

        /// <summary>
        /// 验证日期有效性（精确到日）
        /// </summary>
        private static bool IsValidDate(string date)
        {
            return DateTime.TryParseExact(
                date,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out _
            );
        }

        /// <summary>
        /// 验证番号格式（大写字母+横线+数字）
        /// </summary>
        private static bool IsValidNumber(string number)
        {
            string input = "123abc";

            if (Regex.IsMatch(input, @"^\d+$", RegexOptions.IgnorePatternWhitespace)) // 纯数字
            {
                Console.WriteLine("匹配纯数字");
            }
            else if (Regex.IsMatch(input, @"^[a-zA-Z]+$")) // 纯字母
            {
                Console.WriteLine("匹配纯字母");
            }
            else if (Regex.IsMatch(input, @"^\d{3}-\d{2}-\d{4}$")) // 格式 XXX-XX-XXXX
            {
                Console.WriteLine("匹配社会安全号格式");
            }
            else
            {
                Console.WriteLine("不匹配任何已知模式");
            }

            return Regex.IsMatch(number, @"^[A-Z]+-\d+$");
        }
    }
}
