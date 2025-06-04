//using AngleSharp;
using HtmlAgilityPack;
using JavScraper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using JavScraper.Scrapers;
using JavScraper.Database;
using JavScraper.Service;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace JavScraper
{
    class Program
    {
        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";
        public IConfiguration Configuration { get; }
        private static ServiceProvider serviceProvider;

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging(configureLogging =>
            {
                configureLogging.AddConsole();
            })
            .ConfigureAppConfiguration(appConfiguration =>
            {
                appConfiguration.AddInMemoryCollection().
                SetBasePath(Directory.GetCurrentDirectory()).
                AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<JavScraperApplication>();
            });


        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();

            //var hostBuilder = CreateHostBuilder(args);
            //var host = hostBuilder.Build();

            //host.Run();
            //var services = new ServiceCollection();

            //ServiceRegister.Register(services);

            //serviceProvider = services.BuildServiceProvider();
            //var builder = new ConfigurationBuilder().AddInMemoryCollection() // 将配置文件的数据加载到内存中
            //    .SetBasePath(Directory.GetCurrentDirectory()) // 指定配置文件所在的目录
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 编译成对象  

            //var config = builder.Build();
            //serviceProvider = services.BuildServiceProvider();
            //JavScraperApplication javScraperApplication = new JavScraperApplication(hostBuilder);
            //configure console logging
            //serviceProvider.GetService<ILoggerFactory>().AddConsole(LogLevel.Debug);
            //var context = new PornCrawlerContext();
            //PornCrawlerDbContextSeeder.Seed(context);


            //CaribbeanCrawler();
            //DmmCrawler();
            //Download();

            Console.WriteLine("完成数据采集!");
        }

        private static async void CaribbeanCrawler()
        {
            string[] movieCodes = { "010115-772", "052615-885", "041115-851", "012715-793", "010115-772", "010115-001", "121014-001", "111814-738", "111114-733" };
            foreach (var movieCode in movieCodes)
            {
                CaribbeanScraper crawler = new CaribbeanScraper();
                var movie = await crawler.ScraperMovie(movieCode);
                MovieService service = new MovieService();
                //var flag = await service.Insert(movie);
                //if (flag)
                //{
                //    Console.WriteLine("采集 {0} 影片数据完成!", movieCode);
                //}
            }
        }

        private static async void DmmCrawler()
        {
            string[] movieCodes = { "SSNI-516", "SSNI-644", "AVVR-440", "IPX-439", "SSNI-603", "SSNI-598", "SSNI-604", "SSNI-618", "SSNI-683" };
            foreach (var movieCode in movieCodes)
            {
                DmmScraper crawler = new DmmScraper();
                var movie = await crawler.ScraperMovie(movieCode);
                MovieService service = new MovieService();

                if (!string.IsNullOrEmpty(movie.ContentId))
                {
                    //var flag = await service.Insert(movie);
                    //if (flag)
                    //{
                    //    Console.WriteLine("采集 {0} 影片数据完成!", movieCode);
                    //}
                }
            }
        }

        private static async void REighteenCrawler()
        {
            string[] movieCodes = { "SSNI-516", "SSNI-644", "AVVR-440", "IPX-439", "SSNI-603", "SSNI-598", "SSNI-604", "SSNI-618", "SSNI-683" };
            foreach (var movieCode in movieCodes)
            {
                REighteenScraper crawler = new REighteenScraper();
                var movie = await crawler.ScraperMovie(movieCode);
                MovieService service = new MovieService();

                if (!string.IsNullOrEmpty(movie.ContentId))
                {
                    //var flag = await service.Insert(movie);
                    //if (flag)
                    //{
                    //    Console.WriteLine("采集 {0} 影片数据完成!", movieCode);
                    //}
                }
            }
        }

        private static void Download()
        {
            for (int i = 10; i < 22; i++)
            {
                string url = string.Format("https://en.heyzo.com/contents/3000/0469/gallery/0{0}.jpg", i);
                Downloader.Download(url, @"f:\0469\gallery");
            }
        }

        private void Test()
        {


            ////Dictionary<string, string> dict = new Dictionary<string, string>();
            //////string[] files = Directory.GetFiles(@"H:\VR", "*.*", SearchOption.AllDirectories);
            ////var files = Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp4") || s.EndsWith(".rmvb") || s.EndsWith(".wmv"));
            ////foreach (string file in files)
            ////{
            ////    FileInfo fileInfo = new FileInfo(file);
            ////    string regex = @"[A-Za-z0-9]{3,9}-[0-9]\d*";
            ////    MatchCollection matchCollection = Regex.Matches(fileInfo.Name, regex);
            ////    Match match = matchCollection.FirstOrDefault();
            ////    if (match != null)
            ////    {
            ////        string fileName = match.Value;
            ////        if (!dict.ContainsKey(fileName))
            ////        {
            ////            dict.Add(fileName, file);
            ////        }
            ////    }
            ////}
            ////DmmCrawler dmmCrawler = new DmmCrawler();
            ////// 遍历
            ////foreach (var item in dict)
            ////{
            ////    string url = dmmCrawler.Search(item.Key);
            ////    if (!string.IsNullOrEmpty(url))
            ////    {
            ////        dmmCrawler.Crawler(item.Key, url);
            ////    }
            ////}
        }



        private static Dictionary<string, string> Traversing(string directoryPath)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            var files = directory.GetFiles();
            var directories = directory.GetDirectories().ToList();
            //遍历文件夹
            foreach (DirectoryInfo directoryInfo in directory.GetDirectories())
            {
                dict.Add(directoryInfo.Name, directoryInfo.Name);
            }

            //遍历文件
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                dict.Add(fileInfo.Name, fileInfo.Name);
            }
            Console.WriteLine("开始数据采集!");


            return dict;
        }

        /// <summary>
        /// 下载图片。
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        private static void DownloadImage(string url, string savePath)
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

    }
}
