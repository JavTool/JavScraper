using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicFile.Test
{
    public class VideoTools
    {
        protected static string ffmpegPath = "ffmpeg\\ffmpeg.exe";

        /// <summary>
        ///  从视频画面中截取一帧画面为图片
        /// </summary>
        /// <param name="videoName">视频文件 pic/guiyu.mov</param>
        /// <param name="WidthAndHeight">图片的尺寸如:240*180</param>
        /// <param name="CutTimeFrame">开始截取的时间如:"1"</param>
        /// <param name="PicName">保存的图片地址</param>
        /// <returns></returns>

        public static string GetPicFromVideo(string videoName, string cutTimeFrame, string pictureName)
        {
            //string PicName = Server.MapPath("~/video/" + Guid.NewGuid().ToString().Replace("-", "") + ".jpg");
             
            ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = " -ss " + cutTimeFrame + " -t 0.001 -i \"" + videoName + "\" -y -f image2 \"" + pictureName + "\""; 
            try
            {
                //videoTimeLength = 0;
                var process = Process.Start(startInfo);

                process.WaitForExit();

                return pictureName;
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
        /// <summary>
        /// 获取视频时长
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        public static int GetVideoDuration(string sourceFile, ref string videocode, ref string wh)
        {
            try
            {
                using (System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process())
                {
                    String duration;  // soon will hold our video's duration in the form "HH:MM:SS.UU"  
                    String result;  // temp variable holding a string representation of our video's duration  
                    StreamReader errorreader;  // StringWriter to hold output from ffmpeg  

                    // we want to execute the process without opening a shell  
                    ffmpeg.StartInfo.UseShellExecute = false;
                    //ffmpeg.StartInfo.ErrorDialog = false;  
                    ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    // redirect StandardError so we can parse it  
                    // for some reason the output comes through over StandardError  
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    // set the file name of our process, including the full path  
                    // (as well as quotes, as if you were calling it from the command-line)  
                    ffmpeg.StartInfo.FileName = ffmpegPath;

                    // set the command-line arguments of our process, including full paths of any files  
                    // (as well as quotes, as if you were passing these arguments on the command-line)  
                    ffmpeg.StartInfo.Arguments = "-i " + sourceFile;

                    // start the process  
                    ffmpeg.Start();

                    // now that the process is started, we can redirect output to the StreamReader we defined  
                    errorreader = ffmpeg.StandardError;

                    // wait until ffmpeg comes back  
                    ffmpeg.WaitForExit();

                    // read the output from ffmpeg, which for some reason is found in Process.StandardError  
                    result = errorreader.ReadToEnd();

                    // a little convoluded, this string manipulation...  
                    // working from the inside out, it:  
                    // takes a substring of result, starting from the end of the "Duration: " label contained within,  
                    // (execute "ffmpeg.exe -i somevideofile" on the command-line to verify for yourself that it is there)  
                    // and going the full length of the timestamp  

                    duration = result.Substring(result.IndexOf("Duration: ") + ("Duration: ").Length, ("00:00:00").Length);
                    string widthheight = result.Substring(result.IndexOf("Video:"), 100);
                    //视频编码，如果是 hevc 格式的，则需要转化成 mp4 格式
                    Regex regVideoCode = new Regex("Video:\\s(\\w+)\\s");
                    if (regVideoCode.IsMatch(widthheight))
                    {
                        videocode = regVideoCode.Matches(widthheight)[0].Groups[1].ToString();
                    }
                    Regex reg = new Regex("\\([, /\\w+]*\\)");
                    widthheight = reg.Replace(widthheight, "");
                    reg = new Regex("\\d+x\\d+");
                    wh = reg.Match(widthheight.Split(',')[2].Trim()).Value;
                    string[] ss = duration.Split(':');
                    int h = int.Parse(ss[0]);
                    int m = int.Parse(ss[1]);
                    int s = int.Parse(ss[2]);
                    return h * 3600 + m * 60 + s;
                }
            }
            catch (System.Exception ex)
            {
                return 60;
            }
        }

        /// <summary>
        /// 添加水印
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        public static int AddWaterMark(string sourceFile, string filetype, ref string videocode, ref string wh)
        {
            try
            {
                using (System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process())
                {
                    String duration;  // soon will hold our video's duration in the form "HH:MM:SS.UU"  
                    String result;  // temp variable holding a string representation of our video's duration  
                    StreamReader errorreader;  // StringWriter to hold output from ffmpeg  

                    // we want to execute the process without opening a shell  
                    ffmpeg.StartInfo.UseShellExecute = false;
                    //ffmpeg.StartInfo.ErrorDialog = false;  
                    ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    // redirect StandardError so we can parse it  
                    // for some reason the output comes through over StandardError  
                    ffmpeg.StartInfo.RedirectStandardError = true;
                    // set the file name of our process, including the full path  
                    // (as well as quotes, as if you were calling it from the command-line)  
                    ffmpeg.StartInfo.FileName = ffmpegPath;

                    // set the command-line arguments of our process, including full paths of any files  
                    // (as well as quotes, as if you were passing these arguments on the command-line)  
                    ffmpeg.StartInfo.Arguments = "-i " + sourceFile;

                    // start the process  
                    ffmpeg.Start();

                    // now that the process is started, we can redirect output to the StreamReader we defined  
                    errorreader = ffmpeg.StandardError;

                    // wait until ffmpeg comes back  
                    ffmpeg.WaitForExit();

                    // read the output from ffmpeg, which for some reason is found in Process.StandardError  
                    result = errorreader.ReadToEnd();

                    // a little convoluded, this string manipulation...  
                    // working from the inside out, it:  
                    // takes a substring of result, starting from the end of the "Duration: " label contained within,  
                    // (execute "ffmpeg.exe -i somevideofile" on the command-line to verify for yourself that it is there)  
                    // and going the full length of the timestamp  

                    duration = result.Substring(result.IndexOf("Duration: ") + ("Duration: ").Length, ("00:00:00").Length);
                    string widthheight = result.Substring(result.IndexOf("Video:"), 100);
                    //视频编码，如果是 hevc 格式的，则需要转化成 mp4 格式
                    Regex regVideoCode = new Regex("Video:\\s(\\w+)\\s");
                    if (regVideoCode.IsMatch(widthheight))
                    {
                        videocode = regVideoCode.Matches(widthheight)[0].Groups[1].ToString();
                    }
                    Regex reg = new Regex("\\([, /\\w+]*\\)");
                    widthheight = reg.Replace(widthheight, "");
                    reg = new Regex("\\d+x\\d+");
                    wh = reg.Match(widthheight.Split(',')[2].Trim()).Value;
                    string[] ss = duration.Split(':');
                    int h = int.Parse(ss[0]);
                    int m = int.Parse(ss[1]);
                    int s = int.Parse(ss[2]);
                    return h * 3600 + m * 60 + s;
                }
            }
            catch (System.Exception ex)
            {
                return 60;
            }
        }
        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        public static string GetFileInfo(string URL, ref string fileType, ref string fileSize)
        {
            string Results = "";
            string filetype = URL.Substring(URL.LastIndexOf(".") + 1,
            (URL.Length - URL.LastIndexOf(".") - 1));
            Results = "类型：" + filetype.ToUpper();
            string filename = URL.Substring(URL.LastIndexOf("/") + 1,
            (URL.Length - URL.LastIndexOf("/") - 1));
            Results += "|名称：" + filename;
            long ContentL = 0;
            if (URL.ToLower().StartsWith("http"))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);

                request.MaximumAutomaticRedirections = 4;
                request.MaximumResponseHeadersLength = 4;
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                ContentL = response.ContentLength;
                response.Close();

                Results += "|大小：" + GetSize(ContentL) + "|额外信息：" + response.Server;
                fileType = filetype.ToUpper();
                fileSize = GetSize(ContentL);

            }
            else if (URL.ToLower().StartsWith("ftp://"))
            {

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(URL);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.UseBinary = true;
                FtpWebResponse response = null;
                response = (FtpWebResponse)request.GetResponse();
                Stream Fs = response.GetResponseStream();
                ContentL = response.ContentLength;
                Fs.Close();
                response.Close();

                Results += "|大小：" + GetSize(ContentL) + "|额外信息：" + response.WelcomeMessage;
                fileType = filetype.ToUpper();
                fileSize = GetSize(ContentL);
            }
            else
            {
                Results += "|大小：无法检测";
            }

            return Results;
        }
        //大小转化
        private static string GetSize(long L)
        {
            float result;
            string re = "";
            if (L >= 1073741824)
            {
                result = L / 1073741824.00F;
                re = "GB";
            }
            else if (L >= 1048576)
            {
                result = L / 1048576.00F;
                re = "MB";
            }
            else
            {
                result = L / 1024.00F;
                re = "KB";
            }
            string File_Size = result.ToString("0.00");
            return (File_Size + re);
        }
        /// <summary>
        /// 压缩视频
        /// </summary>
        /// <param name="sourceFile">源文件地址</param>
        /// <param name="compFile">压缩后文件地址</param>
        /// <param name="videocode">视频编码，如果是 hevc 格式的，则需要转化成 h264 格式,html 不能识别 hevc</param>
        /// <returns></returns>
        public static int CompressionFile(string sourceFile, string compFile, string videocode)
        {
            try
            {
                using (System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process())
                {
                    String duration;  // soon will hold our video's duration in the form "HH:MM:SS.UU"  
                    String result;  // temp variable holding a string representation of our video's duration  
                    StreamReader errorreader;  // StringWriter to hold output from ffmpeg  

                    // we want to execute the process without opening a shell  
                    ffmpeg.StartInfo.UseShellExecute = true;
                    //ffmpeg.StartInfo.ErrorDialog = false;  
                    ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    // redirect StandardError so we can parse it  
                    // for some reason the output comes through over StandardError  
                    //ffmpeg.StartInfo.RedirectStandardError = true;
                    // set the file name of our process, including the full path  
                    // (as well as quotes, as if you were calling it from the command-line)  
                    ffmpeg.StartInfo.FileName = ffmpegPath;

                    // set the command-line arguments of our process, including full paths of any files  
                    // (as well as quotes, as if you were passing these arguments on the command-line)  
                    string arguments = "-i " + sourceFile + " -b:v 1000K ";
                    if (videocode.ToLower() == "hevc")
                    {//视频编码，如果是 hevc 格式的，则需要转化成 h264 格式,html 不能识别 hevc
                        arguments += "-c:v libx264 ";
                    }
                    arguments += compFile;
                    ffmpeg.StartInfo.Arguments = arguments;

                    // start the process  
                    ffmpeg.Start();

                    // now that the process is started, we can redirect output to the StreamReader we defined  
                    //errorreader = ffmpeg.StandardError;

                    // wait until ffmpeg comes back  
                    ffmpeg.WaitForExit();

                    // read the output from ffmpeg, which for some reason is found in Process.StandardError  
                    //result = errorreader.ReadToEnd();

                    // a little convoluded, this string manipulation...  
                    // working from the inside out, it:  
                    // takes a substring of result, starting from the end of the "Duration: " label contained within,  
                    // (execute "ffmpeg.exe -i somevideofile" on the command-line to verify for yourself that it is there)  
                    // and going the full length of the timestamp  

                    return 1;
                }
            }
            catch (System.Exception ex)
            {
                return 0;
            }
        }

        /// <summary>
        /// 复制视频
        /// </summary>
        /// <param name="sourceFile">源文件地址</param>
        /// <param name="compFile">压缩后文件地址</param>
        /// <param name="videocode">视频编码，如果是 hevc 格式的，则需要转化成 h264 格式,html 不能识别 hevc</param>
        /// <returns></returns>
        public static int CopyFile(string sourceFile, string compFile)
        {
            try
            {
                using (System.Diagnostics.Process ffmpeg = new System.Diagnostics.Process())
                {
                    String duration;  // soon will hold our video's duration in the form "HH:MM:SS.UU"  
                    String result;  // temp variable holding a string representation of our video's duration  
                    StreamReader errorreader;  // StringWriter to hold output from ffmpeg  

                    // we want to execute the process without opening a shell  
                    ffmpeg.StartInfo.UseShellExecute = true;
                    //ffmpeg.StartInfo.ErrorDialog = false;  
                    ffmpeg.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    // redirect StandardError so we can parse it  
                    // for some reason the output comes through over StandardError  
                    //ffmpeg.StartInfo.RedirectStandardError = true;
                    // set the file name of our process, including the full path  
                    // (as well as quotes, as if you were calling it from the command-line)  
                    ffmpeg.StartInfo.FileName = ffmpegPath;

                    // set the command-line arguments of our process, including full paths of any files  
                    // (as well as quotes, as if you were passing these arguments on the command-line)  
                    string arguments = "-y -i " + sourceFile + " -vcodec copy -acodec copy -movflags +faststart " + compFile;
                    ffmpeg.StartInfo.Arguments = arguments;

                    // start the process  
                    ffmpeg.Start();

                    // now that the process is started, we can redirect output to the StreamReader we defined  
                    //errorreader = ffmpeg.StandardError;

                    // wait until ffmpeg comes back  
                    ffmpeg.WaitForExit();

                    // read the output from ffmpeg, which for some reason is found in Process.StandardError  
                    //result = errorreader.ReadToEnd();

                    // a little convoluded, this string manipulation...  
                    // working from the inside out, it:  
                    // takes a substring of result, starting from the end of the "Duration: " label contained within,  
                    // (execute "ffmpeg.exe -i somevideofile" on the command-line to verify for yourself that it is there)  
                    // and going the full length of the timestamp  

                    return 1;
                }
            }
            catch (System.Exception ex)
            {
                return 0;
            }
        }
    }
}
