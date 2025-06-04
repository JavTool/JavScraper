using JavScraper.Scrapers;
using JavScraper.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace JavScraper.Test
{
    class Program
    {
        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";
        public readonly static IConfiguration configuration;
        private static ServiceProvider serviceProvider;

        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //    .ConfigureLogging(configureLogging =>
        //    {
        //        configureLogging.AddConsole();
        //    })
        //    .ConfigureAppConfiguration(appConfiguration =>
        //    {
        //        appConfiguration.AddInMemoryCollection().
        //        SetBasePath(Directory.GetCurrentDirectory()).
        //        AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        //    })
        //    .ConfigureServices((hostContext, services) =>
        //    {
        //        services.AddHostedService<JavScraperApplication>();
        //    });


        static void Main(string[] args)
        {

            var hostBuilder = Host.CreateDefaultBuilder(args);

            //var host = Host.CreateDefaultBuilder(args)
            //.ConfigureLogging(configureLogging =>
            //{
            //    configureLogging.AddConsole();
            //})
            //.ConfigureAppConfiguration(appConfiguration =>
            //{
            //    appConfiguration.AddInMemoryCollection().
            //    SetBasePath(Directory.GetCurrentDirectory()).
            //    AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            //})
            //.ConfigureServices((hostContext, services) =>
            //{
            //    ServiceRegister.Register(services);

            //});

            var services = new ServiceCollection();

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            services.AddLogging();

            services.AddSingleton(loggerFactory);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());
            //.AddJsonFile($"appsettings.json");
            var configuration = builder.Build();

            services.AddSingleton<IConfiguration>(configuration);

            //IHostEnvironment hostEnvironment = new HostEnvironmentEnvExtensions
       //     Host.CreateDefaultBuilder(args)
       //.ConfigureHostConfiguration(configHost =>
       //{
       //    configHost.SetBasePath(Directory.GetCurrentDirectory());
       //    configHost.AddJsonFile("hostsettings.json", optional: true);
       //    configHost.AddEnvironmentVariables(prefix: "PREFIX_");
       //    configHost.AddCommandLine(args);
       //});

            HostBuilderContext hostBuilderContext = new HostBuilderContext(hostBuilder.Properties);
            var hostEnvironment = hostBuilderContext.HostingEnvironment;
            var javScraper = JavScraperContext.Create();
            var serviceProvider = javScraper.ConfigureServices(services, configuration);


            //hostBuilder.ConfigureServices((hostContext, services) =>
            //{
            //    ServiceRegister.Register(services);
            //});



            var host = hostBuilder.Build();


            //log application start
            var log = javScraper.Resolve<ILoggerFactory>();
            var logger = log.CreateLogger<Program>();
            logger.LogInformation("Application started");


            var scraper = javScraper.Resolve<AbstractScraper>();
            logger.LogInformation(message: $"{scraper.Name} Load.");

            var scrapers = javScraper.Scrapers.FirstOrDefault(o => o.Name == "AVSOX");

            host.Run();
            //ServiceRegister.Register(services);

            //var builder = new ConfigurationBuilder()
            //    .AddInMemoryCollection() // 将配置文件的数据加载到内存中
            //    .SetBasePath(Directory.GetCurrentDirectory()) // 指定配置文件所在的目录
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); // 编译成对象  
            //var config = builder.Build();


            //CreateHostBuilder(args).Build().Run();

            //ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            //     builder.AddSimpleConsole(options =>
            //     {
            //         options.IncludeScopes = true;
            //         options.SingleLine = true;
            //         options.TimestampFormat = "hh:mm:ss ";
            //     }));

            //ILogger<Program> logger = loggerFactory.CreateLogger<Program>();
            ////logger.BeginScope("[scope is enabled]");


            //logger.LogInformation("Hello World!");
            //logger.LogInformation("Logs contain timestamp and log level.");
            //logger.LogInformation("Each log message is fit in a single line.");


            //services.Configure<DataServiceConfig>(config.GetSection("DataServiceConfig"));
            //services.AddOptions().Configure<DataServiceConfig>(config.GetSection("DataServiceConfig"));

            //serviceProvider = services.BuildServiceProvider();

            //configure console logging
            //var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //var logger = loggerFactory.CreateLogger<Program>();

            //logger.LogInformation("Program starting.");
            //CreateHostBuilder(args).Build().Run();
            //IJavScraperApplication javScraperApplication = JavScraperApplication.Create();

            //var instance = JavScraperApplication.Instance;
            //var scrapers = instance.Scrapers;


            //var services = new ServiceCollection();

            //ServiceRegister.Register(services);

            //javScraperApplication.ConfigureServices(services, config);
            //var engine = EngineContext.Create();
            //var serviceProvider = engine.ConfigureServices(services, configuration, nopConfig);

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


        ///// <summary>
        ///// Add services to the application and configure service provider
        ///// </summary>
        ///// <param name="services">Collection of service descriptors</param>
        //public IServiceProvider ConfigureServices(IServiceCollection services)
        //{
        //    return services.ConfigureApplicationServices(_configuration, _hostingEnvironment);
        //}


        private static async void CaribbeanCrawler()
        {
            string[] movieCodes = { "010115-772", "052615-885", "041115-851", "012715-793", "010115-772", "010115-001", "121014-001", "111814-738", "111114-733" };
            foreach (var movieCode in movieCodes)
            {
                //CaribbeanScraper crawler = new CaribbeanScraper();
                //var movie = await crawler.ScraperMovie(movieCode);
                //MovieService service = new MovieService();
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
                //DmmScraper crawler = new DmmScraper();
                //var movie = await crawler.ScraperMovie(movieCode);
                //MovieService service = new MovieService();

                //if (!string.IsNullOrEmpty(movie.ContentId))
                //{
                //    //var flag = await service.Insert(movie);
                //    //if (flag)
                //    //{
                //    //    Console.WriteLine("采集 {0} 影片数据完成!", movieCode);
                //    //}
                //}
            }
        }

        private static async void REighteenCrawler()
        {
            string[] movieCodes = { "SSNI-516", "SSNI-644", "AVVR-440", "IPX-439", "SSNI-603", "SSNI-598", "SSNI-604", "SSNI-618", "SSNI-683" };
            foreach (var movieCode in movieCodes)
            {
                //HeyzoScraper crawler = new HeyzoScraper();
                //var movie = await crawler.ScraperMovie(movieCode);
                //MovieService service = new MovieService();

                //if (!string.IsNullOrEmpty(movie.ContentId))
                //{
                //    //var flag = await service.Insert(movie);
                //    //if (flag)
                //    //{
                //    //    Console.WriteLine("采集 {0} 影片数据完成!", movieCode);
                //    //}
                //}
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
        }    /// <summary>
             /// 格式化并替换字符。
             /// </summary>
             /// <param name="input">要搜索匹配项的字符串。</param>
             /// <returns></returns>
        public static string ReplaceName(string input)
        {

            //input = input.Replace(" (", "「").Replace("(", "「").Replace(")", "」").Replace("（", "「").Replace("）", "」").Replace(" [", "「").Replace("[", "「").Replace("]", "」");

            string pattern = "([\u0800-\ud7ff]+\\.)";
            var match = Regex.Match(input, pattern);
            //string output = Regex.Replace(input, pattern, "");

            string output = match.Value + input.Replace(match.Value, "");

            //string pattern2 = "([\u0800-\ud7ff]+\\.)([a-zA-Z\\.]+\\.)([0-9]+)";
            //var match2 = Regex.Match(output, pattern2);
            //output = Regex.Replace(output, "([\u0800-\ud7ff_a-zA-Z]+)([0-9])", "$1 $2");
            //output = output.Replace(" (", "「").Replace("(", "「").Replace(")", "」");
            return output;
        }

        public static string ReplaceDirName(string input)
        {
            string pattern = "([\u0800-\ud7ff]+\\.)([a-zA-Z\\.]+\\.)([0-9]+)";
            var match = Regex.Match(input, pattern);

            string output = match.Value;
            return output;
        }

    }
}
