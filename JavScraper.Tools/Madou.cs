using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace JavScraper.Tools
{
    public class Madou
    {
        private const string sourcePath = @"\\192.168.0.199\Porn\麻豆";

        //private const string destPath = @"\\192.168.0.199\Porn\麻豆";

        //private static async Task JavOrganization(ILoggerFactory loggerFactory, string sourcePath)
        //{
        //    await MadouOrganization(loggerFactory, sourcePath);
        //}

        public static async Task MadouOrganization(ILoggerFactory loggerFactory, string sourcePath)
        {
            Dictionary<string, string> javFiles = new();
            Director(sourcePath, javFiles);
            foreach (var javFile in javFiles)
            {
                var fileExt = Path.GetExtension(javFile.Key);
                var fileName = Path.GetFileName(javFile.Key).Replace(fileExt, "");
                if (fileExt.ToLower().Contains(".wmv") || fileExt.ToLower().Contains(".mp4") || fileExt.ToLower().Contains(".mkv") || fileExt.ToLower().Contains(".avi") || fileExt.ToLower().Contains(".ts"))
                {
                    if (!File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), "poster.jpg").ToLower()))
                    {

                        DirectoryInfo dirInfo = new(javFile.Key);
                        var fileDir = dirInfo.Parent.FullName;

                        var parentName = dirInfo.Parent.Name;

                        if (!parentName.ToLower().Equals(fileName))
                        {
                            File.Move(javFile.Key, string.Format(@"{0}\{1}{2}", fileDir, parentName, fileExt));
                        }
                        if (File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), fileName + ".jpg").ToLower()))
                        {
                            File.Move(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), fileName + ".jpg"), string.Format(@"{0}\{1}{2}", fileDir, parentName, ".jpg"));
                        }
                        else if (File.Exists(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), fileName.Split(" ")[0] + ".jpg").ToLower()))
                        {
                            File.Move(string.Format(@"{0}\{1}", javFile.Key.Replace(javFile.Value, ""), fileName.Split(" ")[0] + ".jpg"), string.Format(@"{0}\{1}{2}", fileDir, parentName, ".jpg"));
                        }
                        var posterFileName = string.Format("{0}/{1}{2}", fileDir, "poster", ".jpg");
                        ImageUtils.ConvertImage(string.Format(@"{0}\{1}{2}", fileDir, parentName, ".jpg"), posterFileName);
                        var fanartFileName = string.Format("{0}/{1}{2}", fileDir, "fanart", ".jpg");
                        ImageUtils.ConvertImage(string.Format(@"{0}\{1}{2}", fileDir, parentName, ".jpg"), fanartFileName);
                        var landscapeFileName = string.Format("{0}/{1}{2}", fileDir, "landscape", ".jpg");
                        ImageUtils.ConvertImage(string.Format(@"{0}\{1}{2}", fileDir, parentName, ".jpg"), landscapeFileName);


                        var javInfos = parentName.Split(' ');
                        JavVideo javVideo = new JavVideo();

                        javVideo.Number = javInfos[0];
                        if (javInfos.Length == 2)
                        {
                            javVideo.Title = javInfos[1];
                        }
                        else if (javInfos.Length == 3)
                        {
                            javVideo.Title = javInfos[1];
                            javVideo.Actors = new List<string> { javInfos[2] };
                        }
                        else if (javInfos.Length == 4)
                        {
                            javVideo.Title = javInfos[1] + " " + javInfos[2];
                            javVideo.Actors = new List<string> { javInfos[3] };
                        }
                        else if (javInfos.Length == 5)
                        {
                            javVideo.Title = javInfos[1] + " " + javInfos[2] + " " + javInfos[3];
                            javVideo.Actors = new List<string> { javInfos[4] };
                        }
                        else if (javInfos.Length == 6)
                        {
                            javVideo.Title = javInfos[1] + " " + javInfos[2] + " " + javInfos[3] + " " + javInfos[4];
                            javVideo.Actors = new List<string> { javInfos[5] };
                        }
                        javVideo.Genres = new List<string>() { "麻豆" };

                        var nfoFileName = string.Format(@"{0}\{1}", fileDir, parentName + ".nfo");
                        XmlDocument xmlDocument = NfoBuilder.GenerateNfo(javVideo, true);
                        //XmlDocument xmlDocument = new();
                        //xmlDocument.LoadXml(nfo);
                        xmlDocument.Save(nfoFileName);
                    }


                    Console.WriteLine("正在整理文件：--->{0}", javFile.Value);
                }
            }
        }

        public static void Director(string dir, Dictionary<string, string> keyValuePairs)
        {
            DirectoryInfo d = new(dir);
            FileInfo[] files = d.GetFiles();//文件
            DirectoryInfo[] directs = d.GetDirectories();//文件夹
            foreach (FileInfo f in files)
            {
                keyValuePairs.Add(f.FullName, f.Name);//添加文件名到列表中  
            }
            //获取子文件夹内的文件列表，递归遍历  
            foreach (DirectoryInfo dd in directs)
            {
                Director(dd.FullName, keyValuePairs);
            }
        }
        private static string BuilderNfo(JavVideo javVideo)
        {
            XmlDocument xml = NfoBuilder.GenerateNfo(javVideo);

            return xml.ToString();
        }
    }
}
