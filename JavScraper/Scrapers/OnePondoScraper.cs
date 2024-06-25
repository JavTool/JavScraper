using HtmlAgilityPack;
using JavScraper.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Scrapers
{
    public class OnePondoScraper : AbstractScraper
    {

        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";

        /// <summary>
        /// 适配器名称。
        /// </summary>
        public override string Name => "1Pondo";

        /// <summary>
        /// 初始化 <seealso cref="OnePondoScraper"/> 类的新实例。
        /// </summary>
        /// <param name="logManager"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public OnePondoScraper(ILoggerFactory loggerFactory) :
            base("https://www.1pondo.tv/", loggerFactory.CreateLogger<OnePondoScraper>())
        {
        }

        /// <summary>
        /// Dmm 搜索。
        /// </summary>
        /// <param name="keyword">搜索关键字。</param>
        /// <returns></returns>
        public string Search(string keyword)
        {
            string searchUrl = string.Format("https://www.1pondo.tv/movies/{0}/", keyword);
            var web = new HtmlWeb();
            var doc = web.Load(searchUrl);
            var searchResultNodes = doc.DocumentNode.SelectNodes("//*[@id='list']/li");
            string url = string.Empty;
            if (searchResultNodes != null)
            {
                var nodes = searchResultNodes.ToList();
                if (nodes.Count > 0)
                {
                    url = nodes.FirstOrDefault().SelectSingleNode("//*[@id='list']/li/div/p[2]/a").Attributes["href"].Value;
                }
            }
            return url;
        }

        public Movie Scraper(string name, string url)
        {
            Movie product = new Movie();
            //var url = "https://www.dmm.co.jp/mono/dvd/-/detail/=/cid=shkd891/?i3_ref=search&i3_ord=1";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            string saveDir = string.Format(@"{0}\{1}\", savePath, name);

            // title
            var titleNode = doc.DocumentNode.SelectNodes("//*[@id='title']").FirstOrDefault();
            Console.WriteLine(titleNode.InnerText.Trim());

            var performerNode = doc.DocumentNode.SelectNodes("//*[@id='performer']").FirstOrDefault();
            Console.WriteLine(performerNode.InnerText.Trim());

            var categoryNode = doc.DocumentNode.SelectSingleNode("//*[@id='mu']/div/table/tbody/tr/td[1]/table/tbody/tr[8]");
            if (categoryNode != null)
            {
                var childNodes = categoryNode.ChildNodes[1].ChildNodes;
            }
            // images
            var previewImageNode = doc.DocumentNode.SelectNodes("//*[@name='package-image']");
            if (previewImageNode != null)
            {
                Console.WriteLine(previewImageNode.FirstOrDefault().Attributes["href"].Value.Trim());
                //DownloadImage(previewImageNode.FirstOrDefault().Attributes["href"].Value, saveDir);
            }
            var imagesBlockNodes = doc.DocumentNode.SelectNodes("//*[@name='sample-image']/img");
            if (imagesBlockNodes != null)
            {
                var imagesNodes = imagesBlockNodes.ToList();
                foreach (var imagesNode in imagesNodes)
                {
                    Console.WriteLine(imagesNode.Attributes["src"].Value.Trim());
                    //DownloadImage(imagesNode.Attributes["src"].Value.Trim(), saveDir);
                }
            }
            Console.WriteLine("完成数据采集!");

            return product;
        }

        public Movie ScraperMovie(string movieId)
        {
            Movie movie = new Movie();
            return movie;
        }

        public override bool CheckKeyword(string key)
        {
            throw new NotImplementedException();
        }

        protected override Task<List<JavVideoIndex>> Search(List<JavVideoIndex> ls, string key)
        {
            throw new NotImplementedException();
        }

        protected override List<JavVideoIndex> ParseIndex(List<JavVideoIndex> ls, HtmlDocument doc)
        {
            throw new NotImplementedException();
        }

        public override Task<JavVideo> Get(string url)
        {
            throw new NotImplementedException();
        }
    }
}
