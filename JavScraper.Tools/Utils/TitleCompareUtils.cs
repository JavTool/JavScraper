using System;
using System.Linq;

namespace JavScraper.Tools.Utils
{
    public static class TitleCompareUtils
    {
        public static bool AreTitlesSimilar(string title1, string title2, double threshold = 0.5)
        {
            if (string.IsNullOrEmpty(title1) || string.IsNullOrEmpty(title2))
                return false;

            if (title1.Contains(title2) || title2.Contains(title1))
                return true;

            int matchingChars = title1.Intersect(title2).Count();
            double similarityRatio = (double)matchingChars / Math.Max(title1.Length, title2.Length);

            return similarityRatio >= threshold;
        }

        public static string SelectBetterTitle(string nfoTitle, string webTitle, double threshold = 0.5)
        {
            if (AreTitlesSimilar(nfoTitle, webTitle, threshold))
            {
                return webTitle;
            }
            return nfoTitle;
        }
    }
}