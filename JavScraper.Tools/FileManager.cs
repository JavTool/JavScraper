using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JavScraper.Tools
{
    public class FileManager
    {
        

        /// <summary>
        ///  将现有文件复制到新文件。 允许覆盖同名的文件。
        /// </summary>
        /// <param name="sourceFileName">要复制的文件。</param>
        /// <param name="destFileName">目标文件的名称。 不能是目录。</param>
        /// <returns></returns>
        public static bool CopyFile(string sourceFileName, string destFileName)
        {
            if (File.Exists(sourceFileName))
            {
                File.Copy(sourceFileName, destFileName, true);

                if (File.Exists(destFileName))
                {
                    //File.Delete(sourceFileName);
                    return true;
                }
            }
            return false;
        }
    }
}
