using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Tools.Tools
{
    /// <summary>
    /// 时间转换器
    /// </summary>
    public class DateTimeConverter
    {
        public static int ConvertToMinutes(string timeStr)
        {
            if (TimeSpan.TryParse(timeStr, out TimeSpan time))
            {
                return (int)time.TotalMinutes;
            }
            else
            {
                throw new ArgumentException("时间格式不正确，应为 HH:mm:ss");
            }
        }

        //// 示例
        //public static void Main()
        //{
        //    string timeStr = "02:01:18";
        //    int minutes = ConvertToMinutes(timeStr);
        //    Console.WriteLine(minutes); // 输出: 121
        //}
    }
}
