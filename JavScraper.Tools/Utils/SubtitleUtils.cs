using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace JavScraper.Tools.Utils
{
    public static class SubtitleUtils
    {
        public static bool HasExternalSubtitles(string directoryPath)
        {
            string[] subtitleFiles = Directory.GetFiles(directoryPath, "*.srt", SearchOption.AllDirectories);
            return subtitleFiles != null && subtitleFiles.Any();
        }

        public static void AddSubtitleTags(string directoryPath, List<string> tags)
        {
            if (HasExternalSubtitles(directoryPath) && !tags.Contains("外挂字幕"))
            {
                tags.Add("外挂字幕");
            }
        }
    }
}