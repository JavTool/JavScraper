using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JavScraper.Tools
{
    public class JavOrganization
    {
        public static void OrganizeVideo(string sourcePath, string destPath)
        {

            var files = Directory.GetFiles(sourcePath);

            foreach (var file in files)
            {

                var fileName = Path.GetFileNameWithoutExtension(file);
                var fileExt = Path.GetExtension(file);
                if (fileExt.ToLower().Contains(".ts") || fileExt.ToLower().Contains(".mp4"))
                {
                    var nameArray = fileName.Split(' ');
                    var dirName = string.Empty;
                    if (nameArray.Length > 1)
                    {
                        dirName = string.Format("{0} {1}", nameArray[0], nameArray[1]);
                    }
                    string destDirPath = string.Format("{0}/{1}", destPath, dirName);
                    if (!string.IsNullOrEmpty(dirName))
                    {
                        Console.WriteLine(destDirPath);
                    }
                }
            }
        }


        public static void OrganizeGallery(string sourcePath, string destPath)
        {
            var directories = Directory.EnumerateDirectories(sourcePath);
            foreach (var directory in directories)
            {
                var files = Directory.EnumerateFiles(directory);
                if (files.ToList().Count > 0)
                {
                    var directoryName = Path.GetFileNameWithoutExtension(directory);
                    var fileName = JavRecognizer.Parse(directoryName);
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file).ToLower().Contains(".zip") || Path.GetExtension(file).ToLower().Contains(".rar"))
                        {
                            Console.WriteLine(Path.GetFileNameWithoutExtension(file));
                            var destName = string.Format("{0}/{1}{2}", destPath, fileName, Path.GetExtension(file));
                            Console.WriteLine(destName);
                            File.Move(file, destName);
                        }

                    }
                    Console.WriteLine(fileName);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        public static void OrganizeDirectory(string sourcePath)
        {
            Dictionary<string, string> dirDict = new Dictionary<string, string>();

            FindAllDir(sourcePath, dirDict);

            foreach (var dir in dirDict)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dir.Key);

                string dirName = dir.Value.Replace("(", "[").Replace(")", "]");
                string destFullName = string.Format("{0}/{1}", dirInfo.Parent.FullName, dirName);
                Directory.Move(dirInfo.FullName, destFullName);
                Console.WriteLine(dir.Value);
            }
        }

        /// <summary>
        /// 获取全部目录，包含子目录。
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="dirDict"></param>
        public static void FindAllDir(string dirPath, Dictionary<string, string> dirDict)
        {
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            DirectoryInfo[] dirs = dir.GetDirectories(); // 获取所有文件夹

            // 获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dirInfo in dirs)
            {
                FindAllDir(dirInfo.FullName, dirDict);
                dirDict.Add(dirInfo.FullName, dirInfo.Name);
            }
        }

        /// <summary>
        /// 获取全部文件，包含子目录中的文件。
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="fileDict"></param>
        public static void FindAllFile(string dirPath, Dictionary<string, string> fileDict)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            // 获取当前目录所有文件
            FileInfo[] files = dirInfo.GetFiles();
            // 获取当前目录所有文件夹
            DirectoryInfo[] dirs = dirInfo.GetDirectories(); 
            foreach (FileInfo file in files)
            {
                // 添加文件名到列表中
                fileDict.Add(file.FullName, file.Name);
            }
            // 递归遍历子文件夹内的文件列表
            foreach (DirectoryInfo dir in dirs)
            {
                FindAllFile(dir.FullName, fileDict);
            }
        }

        /// <summary>
        /// 判断目标是文件夹还是目录（目录包括磁盘）。
        /// </summary>
        /// <param name="filepath">文件名。</param>
        /// <returns></returns>
        public static bool IsDirectory(string filepath)
        {
            FileInfo fi = new FileInfo(filepath);
            if ((fi.Attributes & FileAttributes.Directory) != 0)
                return true;
            else
            {
                return false;
            }
        }
    }
}
