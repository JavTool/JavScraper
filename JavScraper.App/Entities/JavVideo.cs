using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JavScraper.App.Entities
{
    /// <summary>
    /// 视频
    /// </summary>
    public class JavVideo
    {

        public JavVideo()
        {
            Samples = new List<string>();
        }

        /// <summary>
        /// 原始标题
        /// </summary>
        private string _originalTitle;

        /// <summary>
        /// 原始标题
        /// </summary>
        public string OriginalTitle { get => string.IsNullOrWhiteSpace(_originalTitle) ? (_originalTitle = Title) : _originalTitle; set => _originalTitle = value; }

        /// <summary>
        /// 内容简介
        /// </summary>
        public string Plot { get; set; }

        /// <summary>
        /// 导演
        /// </summary>
        public string Director { get; set; }

        /// <summary>
        /// 影片时长
        /// </summary>
        public string Runtime { get; set; }

        /// <summary>
        /// 制作组
        /// </summary>
        public string Studio { get; set; }

        /// <summary>
        /// 厂商
        /// </summary>
        public string Maker { get; set; }

        /// <summary>
        /// 合集
        /// </summary>
        public string Set { get; set; }

        /// <summary>
        /// 类别
        /// </summary>
        public List<string> Genres { get; set; }

        /// <summary>
        /// 演员
        /// </summary>
        public List<string> Actors { get; set; }

        /// <summary>
        /// 样品图片
        /// </summary>
        public List<string> Samples { get; set; }

        /// <summary>
        /// %genre:中文字幕?中文:%
        /// </summary>
        private static readonly Regex regex_genre = new Regex("%genre:(?<a>[^?]+)?(?<b>[^:]*):(?<c>[^%]*)%", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 获取格式化文件名
        /// </summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="empty">空参数替代</param>
        /// <param name="clear_invalid_path_chars">是否移除路径中的非法字符</param>
        /// <returns></returns>
        public string GetFormatName(string format, string empty, bool clear_invalid_path_chars = false)
        {
            if (empty == null)
                empty = string.Empty;

            var m = this;
            void Replace(string key, string value)
            {
                var _index = format.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (_index < 0)
                    return;

                if (string.IsNullOrEmpty(value))
                    value = empty;

                do
                {
                    format = format.Remove(_index, key.Length);
                    format = format.Insert(_index, value);
                    _index = format.IndexOf(key, _index + value.Length, StringComparison.OrdinalIgnoreCase);
                } while (_index >= 0);
            }

            Replace("%num%", m.Number);
            Replace("%title%", m.Title);
            Replace("%title_original%", m.OriginalTitle);
            Replace("%actor%", m.Actors?.Any() == true ? string.Join(", ", m.Actors) : null);
            Replace("%actor_first%", m.Actors?.FirstOrDefault());
            Replace("%set%", m.Set);
            Replace("%director%", m.Director);
            Replace("%date%", m.Date);
            Replace("%year%", m.GetYear()?.ToString());
            Replace("%month%", m.GetMonth()?.ToString("00"));
            Replace("%studio%", m.Studio);
            Replace("%maker%", m.Maker);

            do
            {
                //%genre:中文字幕?中文:%
                var match = regex_genre.Match(format);
                if (match.Success == false)
                    break;
                var a = match.Groups["a"].Value;
                var genre_key = m.Genres?.Contains(a, StringComparer.OrdinalIgnoreCase) == true ? "b" : "c";
                var genre_value = match.Groups[genre_key].Value;
                format = format.Replace(match.Value, genre_value);
            } while (true);

            //移除非法字符，以及修正路径分隔符
            if (clear_invalid_path_chars)
            {
                format = string.Join(" ", format.Split(Path.GetInvalidPathChars()));
                if (Path.DirectorySeparatorChar == '/')
                    format = format.Replace('\\', '/');
                else if (Path.DirectorySeparatorChar == '\\')
                    format = format.Replace('/', '\\');
            }

            return format;
        }

        /// <summary>
        /// 地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 番号
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 封面
        /// </summary>
        public string Cover { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Number} {Title}";

        /// <summary>
        /// 获取年份
        /// </summary>
        /// <returns></returns>
        public int? GetYear()
        {
            if (!(Date?.Length >= 4))
                return null;
            if (int.TryParse(Date.Substring(0, 4), out var y) && y > 0)
                return y;
            return null;
        }

        /// <summary>
        /// 获取月份
        /// </summary>
        /// <returns></returns>
        public int? GetMonth()
        {
            if (!(Date?.Length >= 6))
                return null;
            var d = Date.Split("-/ 年月日".ToCharArray());
            if (d.Length > 1)
            {
                if (int.TryParse(d[1], out var m) && m > 0 && m <= 12)
                    return m;
                return null;
            }
            if (int.TryParse(Date.Substring(4, 2), out var m2) && m2 > 0 && m2 <= 12)
                return m2;
            return null;
        }



        /// <summary>
        /// 获取日期。
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset? GetDate()
        {
            if (string.IsNullOrEmpty(Date))
                return null;
            if (DateTimeOffset.TryParseExact(Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset result))
            {
                return result.ToUniversalTime();
            }
            else if (DateTimeOffset.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            {
                return result.ToUniversalTime();
            }
            return null;
        }


    }
}
