using System.Collections.Generic;
using System.IO;

namespace JavScraper.App.Services
{
    public static class DirectoryHelper
    {
        public static void GetAllFiles(string dir, Dictionary<string, string> keyValuePairs)
        {
            DirectoryInfo d = new(dir);
            FileInfo[] files = d.GetFiles(); // 文件
            DirectoryInfo[] directs = d.GetDirectories(); // 文件夹

            foreach (FileInfo f in files)
            {
                keyValuePairs.Add(f.FullName, f.Name); // 添加文件名到列表中  
            }

            // 获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                GetAllFiles(dd.FullName, keyValuePairs);
            }
        }
    }
}