using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace JavScraper
{
    public class Downloader
    {
        /// <summary>
        /// 下载。
        /// </summary>
        /// <param name="url">下载资源链接地址。</param>
        /// <param name="savePath">下载文件保存路径。</param>
        public static void Download(string url, string savePath)
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
                var fileName = webResponse.ResponseUri.Segments.ToList().LastOrDefault();
                string saveFileName = string.Format(@"{0}\{1}", savePath, fileName);
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
                using FileStream fileStream = new FileStream(saveFileName, FileMode.Create);
                webResponse.GetResponseStream().CopyTo(fileStream);
            }
        }

        public void Download()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                UseProxy = false // 不加这个会非常慢
            };

            using (HttpClient httpClient = new HttpClient(handler))
            {
                //httpClient.BaseAddress = new Uri(dataServiceConfig.BaseAddress_Idui1);
                //WebClientUtil webClientUtil = new WebClientUtil(httpClient);
                //webClientUtil.AddHeader("accesstoken", dataServiceConfig.Idui1Token);
                //string apiUrl = string.Format("{0}{1}{2}", dataServiceConfig.BaseAddress_Idui1, "/question/GetSyllabuss/", questionId);
                //var data = await webClientUtil.GetAsync<List<QuestionSyllabusResServiceModel>>(apiUrl);
                //stopwatchUtil.Stop();
                //if (stopwatchUtil.GreaterTotalSeconds(10))
                //{
                //    _logger.LogError("QuestionService.GetQuestionSyllabuss：" + stopwatchUtil.ElapsedString() + "\r\nid：" + questionId);
                //}
                //return data;
            }
        }

        #region 字段...

        /// <summary>
        /// 用户多线程同步的对象。
        /// </summary>
        private static object syncObject = new object();

        /// <summary>
        /// 用于计算速度的临时变量。
        /// </summary>
        private long downloadedBytes = 0;

        /// <summary>
        /// 用户下载的通信对象。
        /// </summary>
        private readonly WebClient client;

        /// <summary>
        /// 每次读取的字节数。
        /// </summary>
        private readonly int readBytes = 100 * 1024;

        /// <summary>
        /// 用户主动终止下载。
        /// </summary>
        private bool StopDownload = false;

        #endregion

        public event DownloadFinishedHandler FileDownloadComplete;

        #region 属性...

        /// <summary>
        /// 下载地址。
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 已下载的字节数。
        /// </summary>
        public long DownloadBytes { get; set; }

        /// <summary>
        /// 要下载的字节总数（文件大小）。
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 是否正在下载。
        /// </summary>
        public bool Downloading { get; set; }

        /// <summary>
        /// 当前进度。
        /// </summary>
        public double Progress
        {
            get { return DownloadBytes * 100.0 / TotalBytes; }
        }

        /// <summary>
        /// 即时速度。
        /// </summary>
        public double Speed { get; set; }

        /// <summary>
        /// 是否已完成。
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// 保存在本地的文件名称。
        /// </summary>
        public string FileName { get; set; }

        #endregion

        #region 构造器...

        public Downloader(string downloadUrl)
        {
            DownloadUrl = downloadUrl;
            client = new WebClient();
            client.DownloadProgressChanged += DownloadProgressChanged_Client;
            client.DownloadFileCompleted += DownloadFileCompleted_Client;
            FileName = string.Format($"{Path.GetTempFileName()}");
            StartDownload();
        }



        #endregion

        /// <summary>
        /// 下载完成出发事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DownloadFileCompleted_Client(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Downloading = false;
            Speed = 0;
            if (!e.Cancelled)
            {
                Completed = true;
            }
        }

        /// <summary>
        /// 开始下载。
        /// </summary>
        public void StartDownload()
        {
            Downloading = true;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(DownloadUrl);
            if (DownloadBytes > 0)
            {
                request.AddRange(DownloadBytes);
            }
            request.BeginGetResponse(ar =>
            {
                try
                {
                    var response = request.EndGetResponse(ar);
                    if (TotalBytes == 0)
                    {
                        TotalBytes = response.ContentLength;
                    }
                    using var writer = new FileStream(FileName, FileMode.OpenOrCreate);
                    using var stream = response.GetResponseStream();
                    while (Downloading)
                    {
                        byte[] data = new byte[readBytes];
                        int readNumber = stream.Read(data, 0, data.Length);
                        if (readNumber > 0)
                        {
                            writer.Write(data, 0, readNumber);
                            DownloadBytes += readNumber;
                        }

                        int downloadingSpeed = Convert.ToInt32((DownloadBytes * 100 / TotalBytes));
                        if (DownloadBytes == TotalBytes)
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    //// 记录日志
                    //Log4NetHelper.Error("下载试题出错：", ex);

                    StopDownload = true;
                }
                Complete();
            }, null);
        }

        public void Complete()
        {
            Completed = true;
            Downloading = false;
            Speed = 0;
            client.Dispose();

            if (!StopDownload)
            {
                FileDownloadComplete?.Invoke();
            }
        }

        public void PauseDownload()
        {
            Downloading = false;
            Speed = 0;
        }

        /// <summary>
        /// 删除下载的文件。
        /// </summary>
        public void DeleteFile()
        {
            File.Delete(FileName);
        }

        /// <summary>
        /// 下载进度变化时触发的方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DownloadProgressChanged_Client(object sender, DownloadProgressChangedEventArgs e)
        {
            TotalBytes = e.TotalBytesToReceive;
            DownloadBytes = e.BytesReceived;
        }
    }
    public delegate void DownloadFinishedHandler();
}
