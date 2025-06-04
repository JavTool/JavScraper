using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JavScraper.App
{
    /// <summary>
    /// 字符串工具类。
    /// </summary>
    public class StringUtil
    {
        /// <summary>
        /// 判断是否为繁体。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBGI5(string str)
        {
            int lnBIG5 = 0; // 用于统计可能是繁体字的汉字个数
            int lnGB = 0; // 用于统计可能是简体字的汉字个数
            int liTranLen = str.Length;

            for (int i = 0; i < liTranLen; i++)
            {
                // 尾字节 40-7E 是 BGI5 码特有的，如果扫描到这种编码说明此字元串是繁体(经测试：有例外，可能是汉字的最后一个编码与英文编码组合而成的)
                if (str[i] >= 161 && str[i] <= 254 && str[i + 1] >= 64 && str[i + 1] <= 126)
                    lnBIG5++;

                // 首字节 A4-A9 在 GB 中为日文假名,希腊字母,俄文字母和制表符,正常文本中很少出现，而这个范围是 BIG5 的常用汉字,所以认为这是 BIG5 码
                if (str[i] >= 164 && str[i] <= 169 && str[i + 1] >= 161 && str[i + 1] <= 254)
                    lnBIG5++;

                // GB 中首字节 AA-AF 没有定义,所以首字节位于 AA-AF 之间，尾字节位于 A1-FE 的编码几乎 100% 是 BIG5(经测试：没有 100%)，认为是 BIG5 码
                if (str[i] >= 170 && str[i] <= 175 && str[i + 1] >= 161 && str[i + 1] <= 254)
                    lnBIG5++;

                // 首字节 C6-D7，尾字节 A1-FE 在 GB 中属于一级字库，是常用汉字，而在 BIG5 中，C6-C7 没有明确定义,但通常用来放日文假名和序号，C8-D7 属于罕用汉字区，所以可认为是 GB 码
                if (str[i] >= 196 && str[i] <= 215 && str[i + 1] >= 161 && str[i + 1] <= 254)
                    lnGB++;
            }

            // 如果扫描完整个字元串，可能是简体字的数目比可能是繁体字的数目多就认为是简体字不转简(不一定准确)
            if (lnGB > lnBIG5)
                return false;
            else
                return true;
        }

        /// <summary>
        /// 繁体转换为简体。
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string TraditionalToSimplified(string input)
        {
            return ChineseConverter.Convert(input, ChineseConversionDirection.TraditionalToSimplified);//转简体
        }


        public static string ReplaceNumber(string input)
        {
            string output = input;
            //string pattern = ".*[0-9]{1,}.*";
            //Regex regex = new(pattern);
            //if (regex.IsMatch(input))
            //{
            //    foreach (var pair in numMapping_reverse)
            //    {
            //        output = output.Replace(pair.Key.ToString(), pair.Value);
            //    }
            //}
            return output;
        }

        /// <summary>
        /// 格式化并替换字符。
        /// </summary>
        /// <param name="input">要搜索匹配项的字符串。</param>
        /// <returns></returns>
        public static string ReplaceFormat(string input)
        {
            input = input.Replace(" (", "「").Replace("(", "「").Replace(")", "」").Replace("（", "「").Replace("）", "」").Replace(" [", "「").Replace("[", "「").Replace("]", "」");

            string pattern = "([0-9])([\u0800-\ud7ff_a-zA-Z]+)";
            string output = Regex.Replace(input, pattern, "$1 $2");
            output = Regex.Replace(output, "([\u0800-\ud7ff_a-zA-Z]+)([0-9])", "$1 $2");
            output = output.Replace(" (", "「").Replace("(", "「").Replace(")", "」");
            return output;
        }
    }
}
