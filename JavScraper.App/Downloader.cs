using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace JavScraper.App
{
    public class Downloader
    {

        /// <summary>
        /// 下载。
        /// </summary>
        /// <param name="url">下载资源链接地址。</param>
        /// <param name="savePath">下载文件保存路径。</param>
        public static string Download(string url, string savePath, string fileName)
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
            httpWebRequest.KeepAlive = false;
            httpWebRequest.Timeout = 30 * 1000;
            httpWebRequest.Method = "GET";
            httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36";

            WebResponse webResponse = httpWebRequest.GetResponse();
            if (((HttpWebResponse)webResponse).StatusCode == HttpStatusCode.OK)
            {
                var fileExt = webResponse.ResponseUri.Segments.ToList().LastOrDefault().Split(".")[1];
                string saveFileName = string.Format(@"{0}\{1}.{2}", savePath, fileName, fileExt);
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
                using FileStream fileStream = new FileStream(saveFileName, FileMode.Create);
                webResponse.GetResponseStream().CopyTo(fileStream);
                return saveFileName;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
