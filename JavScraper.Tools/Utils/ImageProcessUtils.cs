using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static JavScraper.Tools.ImageUtils;

namespace JavScraper.Tools.Utils
{
    public static class ImageProcessUtils
    {
        public static async Task ProcessCoverImages(
            string directoryPath,
            Dictionary<string, string> coverDict,
            string saveName,
            float targetRatio,
            ILogger logger)
        {
            if (!File.Exists(saveName)) return;

            var thumbPicture = Path.Combine(directoryPath, "thumb.jpg");
            var folderPicture = Path.Combine(directoryPath, "folder.jpg");
            var posterPicture = Path.Combine(directoryPath, "poster.jpg");

            try
            {
                File.Copy(saveName, thumbPicture, true);

                if (coverDict != null && coverDict.Count > 0 && coverDict.ContainsKey("poster"))
                {
                    ProcessPosterImage(coverDict["poster"], folderPicture, posterPicture, saveName, targetRatio);
                }
                else
                {
                    ImageUtils.CropImage(saveName, folderPicture, targetRatio, CropMode.Right);
                    File.Copy(folderPicture, posterPicture, true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"处理封面图片时出错：{ex.Message}");
            }
        }

        private static void ProcessPosterImage(string posterPath, string folderPicture, string posterPicture, string saveName, float targetRatio)
        {
            using (var image = System.Drawing.Image.FromFile(posterPath))
            {
                if (image.Height < 400 || image.Width < 300)
                {
                    ImageUtils.CropImage(saveName, folderPicture, targetRatio, CropMode.Right);
                    File.Copy(folderPicture, posterPicture, true);
                }
                else
                {
                    File.Copy(posterPath, folderPicture, true);
                    File.Copy(posterPath, posterPicture, true);
                }
            }
        }
    }
}