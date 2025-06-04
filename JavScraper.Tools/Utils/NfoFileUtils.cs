using System;
using System.Collections.Generic;
using System.IO;

namespace JavScraper.Tools.Utils
{
    public static class NfoFileUtils
    {
        /// <summary>
        /// 遍历指定目录及子目录，获取所有的 .nfo 文件，并按目录名称分组
        /// </summary>
        public static Dictionary<string, List<string>> GetNfoFilesGroupedByDirectoryName(string rootDirectory)
        {
            Dictionary<string, List<string>> groupedFiles = new Dictionary<string, List<string>>();

            try
            {
                string[] files = Directory.GetFiles(rootDirectory, "*.nfo", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.EndsWith("bak.nfo")) continue;

                    string directoryName = Path.GetFileName(Path.GetDirectoryName(file));
                    if (!groupedFiles.ContainsKey(directoryName))
                    {
                        groupedFiles[directoryName] = new List<string>();
                    }

                    groupedFiles[directoryName].Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }

            return groupedFiles;
        }
    }
}