using System.IO;

namespace JavScraper.Tools.Utils
{
    public static class JavHelperUtils
    {
        public static void ReplaceJavHelperImages(string directoryPath, string directoryName, 
            string fanartPath, string folderPicturePath)
        {
            var javHelperFanart = Path.Combine(directoryPath, $"{directoryName}-fanart.jpg");
            var javHelperThumb = Path.Combine(directoryPath, $"{directoryName}-thumb.jpg");
            var javHelperPoster = Path.Combine(directoryPath, $"{directoryName}-poster.jpg");

            if (File.Exists(javHelperFanart))
            {
                File.Copy(fanartPath, javHelperFanart, true);
                File.Copy(fanartPath, javHelperThumb, true);
            }

            if (File.Exists(javHelperPoster))
            {
                File.Copy(folderPicturePath, javHelperPoster, true);
            }
        }
    }
}