using System.Collections.Generic;
using System.IO;

namespace JavScraper.Tools.Services
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

        public static bool Confirm(string message = "是否确认执行此操作? ")
        {
            System.Console.WriteLine(message);
            System.Console.WriteLine("确认请输入[Y]，按任意键返回菜单");
            var key = System.Console.ReadKey();
            System.Console.WriteLine("");
            
            return key.KeyChar.ToString().ToUpper() == "Y";
        }
    }
}