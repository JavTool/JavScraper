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
    public class CaribbeanScraper : AbstractScraper
    {
        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";

        private readonly static string SEARCH_URL = "https://www.caribbeancom.com/search/?q={0}";
        private readonly static string MOVIE_URL = "https://www.caribbeancom.com/moviepages/{0}/index.html";

        /// <summary>
        /// 适配器名称。
        /// </summary>
        public override string Name => "Caribbean";

        /// <summary>
        /// 初始化 <seealso cref="CaribbeanScraper"/> 类的新实例。
        /// </summary>
        /// <param name="loggerFactory"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public CaribbeanScraper(ILoggerFactory loggerFactory) :
            base("https://www.caribbeancom.com/", loggerFactory.CreateLogger<CaribbeanScraper>())
        {
        }

        /// <summary>
        /// Caribbean 搜索。
        /// </summary>
        /// <param name="keyword">搜索关键字。</param>
        /// <returns></returns>
        public string Search(string keyword)
        {
            string searchUrl = string.Format(SEARCH_URL, keyword);
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
            //var url = string.Format(SEARCH_URL, name);
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

        public async Task<Movie> ScraperMovie(string movieCode)
        {
            Movie movie = new Movie();
            movie.Code = movieCode;
            var url = string.Format(MOVIE_URL, movieCode);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //Encoding encoding = Encoding.GetEncoding("euc-jp");
            var web = new HtmlWeb();
            //var doc = await web.LoadFromWebAsync(url, encoding);
            var doc = web.Load(url);

            // title
            var titleNode = doc.DocumentNode.SelectNodes("//*[@id='moviepages']/div/div[1]/div[1]/div[2]/h1").FirstOrDefault();
            movie.Title = titleNode.InnerText.Trim();
            Console.WriteLine(titleNode.InnerText.Trim());

            // description
            var descriptionNode = doc.DocumentNode.SelectNodes("//*[@id='moviepages']/div/div[1]/div[1]/p").FirstOrDefault();
            movie.Description = descriptionNode.InnerText.Trim();
            Console.WriteLine(descriptionNode.InnerText.Trim());

            var movieInfoNode = doc.DocumentNode.SelectSingleNode("//*[@class='movie-info section']");
            if (movieInfoNode != null)
            {
                var movieDoc = new HtmlDocument();
                movieDoc.LoadHtml(movieInfoNode.InnerHtml);

                var uploadDate = movieDoc.DocumentNode.SelectSingleNode("//*[@itemprop='uploadDate']").InnerText;
                if (!string.IsNullOrEmpty(uploadDate))
                {
                    movie.UploadDate = DateTime.Parse(uploadDate);
                }
                movie.Duration = movieDoc.DocumentNode.SelectSingleNode("//*[@itemprop='duration']").InnerText;

                var actorNodes = movieDoc.DocumentNode.SelectNodes("//*[@itemprop='actor']").Select(n => n.InnerText).ToList();
                List<Actres> actress = new List<Actres>();
                foreach (var actorNode in actorNodes)
                {
                    Actres actres = new Actres
                    {
                        MovieCode = movieCode,
                        Name = actorNode
                    };
                    actress.Add(actres);
                }
                movie.Actress = actress;

                var categoryNodes = doc.DocumentNode.SelectNodes("//*[@class='spec-item']").Select(n => n.InnerText).ToList();
                //var categoryNodes = doc.DocumentNode.SelectNodes("//*[@itemprop='genre']").Select(n => n.InnerText).ToList();
                List<Category> categories = new List<Category>();
                foreach (var categoryNode in categoryNodes)
                {
                    Category category = new Category
                    {
                        MovieCode = movieCode,
                        Name = categoryNode
                    };
                    categories.Add(category);
                }
                movie.Categories = categories;

            }

            // images
            var previewUrl = string.Format("https://www.caribbeancom.com/moviepages/{0}/images/l_l.jpg", movieCode);
            movie.PreviewImage = previewUrl;
            var imagesUrl = "https://www.caribbeancom.com/moviepages/{0}/images/l/00{1}.jpg";
            List<MoviePicture> imagesUrls = new List<MoviePicture>();
            for (int i = 1; i < 6; i++)
            {
                MoviePicture picture = new MoviePicture
                {
                    MovieCode = movieCode,
                    Url = string.Format(imagesUrl, movieCode, i),
                    Preview = false
                };
                imagesUrls.Add(picture);
            }
            movie.Pictures = imagesUrls;
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

        //Task<Movie> CrawlerMovie(string movieCode);
        //{
        //    throw new NotImplementedException();
        //}
    }
}
