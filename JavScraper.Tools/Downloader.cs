using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Tools
{
    public class Downloader
    {
        /// <summary>
        /// 下载。
        /// </summary>
        /// <param name="url">下载资源链接地址。</param>
        /// <param name="savePath">下载文件保存路径。</param>
        public static async Task<string> DownloadAsync(string url, string savePath, string fileName)
        {
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var fileExt = response.Content.Headers.ContentType.MediaType.Split('/')[1];
                    string saveFileName = Path.Combine(savePath, $"{fileName}.{fileExt}");

                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }

                    using FileStream fileStream = new FileStream(saveFileName, FileMode.Create);
                    await response.Content.CopyToAsync(fileStream);

                    return saveFileName;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载失败：{ex}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 下载图片并保存为 JPG 格式。
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="fileNameWithoutExt">可选的文件名（不含扩展名），如果不传则使用 URL 中的原文件名</param>
        public static async Task<string> DownloadJpegAsync(string url, string savePath, string fileNameWithoutExt = null)
        {
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            try
            {
                using var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream); // 自动识别 WebP

                    if (!Directory.Exists(savePath))
                        Directory.CreateDirectory(savePath);

                    if (string.IsNullOrWhiteSpace(fileNameWithoutExt))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
                        fileNameWithoutExt = fileName;
                    }

                    string outputPath = Path.Combine(savePath, $"{fileNameWithoutExt}.jpg");

                    await image.SaveAsJpegAsync(outputPath, new JpegEncoder { Quality = 90 });

                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载或转换失败：{ex.Message}");
            }

            return string.Empty;
        }

        ///// <summary>
        ///// 下载图片并保存为 JPG 格式。
        ///// </summary>
        ///// <param name="url">图片地址</param>
        ///// <param name="savePath">保存路径</param>
        ///// <param name="fileNameWithoutExt">可选的文件名（不含扩展名），如果不传则使用 URL 中的原文件名</param>
        //public static async Task<string>DownloadJpegAsync(string url, string savePath, string fileNameWithoutExt = null)
        //{
        //    using HttpClient httpClient = new HttpClient();
        //    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
        //    httpClient.DefaultRequestHeaders.Add("Accept", "image/*,*/*;q=0.8");

        //    try
        //    {
        //        using HttpResponseMessage response = await httpClient.GetAsync(url);
        //        if (response.IsSuccessStatusCode)
        //        {
        //            using var memStream = new MemoryStream();
        //            await response.Content.CopyToAsync(memStream);

        //            // 用 System.Drawing 读取图片
        //            memStream.Seek(0, SeekOrigin.Begin);
        //            using var image = Image.FromStream(memStream);

        //            if (!Directory.Exists(savePath))
        //                Directory.CreateDirectory(savePath);

        //            // 如果没有传 fileNameWithoutExt，就从 URL 中提取文件名（去掉扩展名）
        //            if (string.IsNullOrWhiteSpace(fileNameWithoutExt))
        //            {
        //                string fileNameWithExt = Path.GetFileName(new Uri(url).LocalPath); // 如 1fsdss922ps.jpg
        //                fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileNameWithExt); // 如 1fsdss922ps
        //            }

        //            string saveFilePath = Path.Combine(savePath, $"{fileNameWithoutExt}.jpg");

        //            image.Save(saveFilePath, ImageFormat.Jpeg); // 转为 JPG 保存

        //            return saveFilePath;
        //        }
        //        else
        //        {
        //            Console.WriteLine($"下载失败，HTTP 状态码：{response.StatusCode}");
        //            return string.Empty;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"下载或转换失败：{ex.Message}");
        //        return string.Empty;
        //    }
        //}
    }
}
