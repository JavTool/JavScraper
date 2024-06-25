using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JavScraper.Domain
{
    /// <summary>
    /// 视频索引。
    /// </summary>
    public class JavVideoIndex
    {
        /// <summary>
        /// 适配器。
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 番号。
        /// </summary>
        public string Num { get; set; }

        /// <summary>
        /// 标题。
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 封面。
        /// </summary>
        public string Cover { get; set; }

        /// <summary>
        /// 日期。
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 转换为字符串。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Num} {Title}";

        /// <summary>
        /// 获取年份。
        /// </summary>
        /// <returns></returns>
        public int? GetYear()
        {
            if (!(Date?.Length >= 4))
                return null;
            if (int.TryParse(Date.Substring(0, 4), out var year) && year > 0)
                return year;
            return null;
        }

        /// <summary>
        /// 获取月份。
        /// </summary>
        /// <returns></returns>
        public int? GetMonth()
        {
            if (!(Date?.Length >= 6))
                return null;
            var date = Date.Split("-/ 年月日".ToCharArray());
            if (date.Length > 1)
            {
                if (int.TryParse(date[1], out var month) && month > 0 && month <= 12)
                    return month;
                return null;
            }
            if (int.TryParse(Date.Substring(4, 2), out var result) && result > 0 && result <= 12)
                return result;
            return null;
        }


        /// <summary>
        /// 获取日期。
        /// </summary>
        /// <returns></returns>
        public DateTime? GetDate()
        {
            if (string.IsNullOrEmpty(Date))
                return null;
            if (DateTime.TryParseExact(Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime result))
            {
                return result.ToUniversalTime();
            }
            else if (DateTime.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out result))
            {
                return result.ToUniversalTime();
            }
            return null;
        }

    }
}
