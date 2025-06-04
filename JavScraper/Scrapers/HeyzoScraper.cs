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
    public class HeyzoScraper : AbstractScraper
    {

        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";

        /// <summary>
        /// 适配器名称。
        /// </summary>
        public override string Name => "Heyzo";

        /// <summary>
        /// 初始化 <seealso cref="HeyzoScraper"/> 类的新实例。
        /// </summary>
        /// <param name="loggerFactory"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public HeyzoScraper(ILoggerFactory loggerFactory) :
            base("https://heyzo.com/", loggerFactory.CreateLogger<HeyzoScraper>())
        {
        }

        /// <summary>
        /// R18 搜索。
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public string Search(string keyword)
        {
            string searchUrl = string.Format("https://www.r18.com/common/search/searchword={0}/", keyword);
            var web = new HtmlWeb();
            var doc = web.Load(searchUrl);
            var searchResultNodes = doc.DocumentNode.SelectNodes("//*[@id='list']/li").ToList();
            string url = string.Empty;
            if (searchResultNodes.Count > 0)
            {
                url = searchResultNodes.FirstOrDefault().SelectSingleNode("//*[@id='list']/li/div/p[2]/a").Attributes["href"].Value;
            }
            return url;
        }


        public Movie Scraper(string movieCode, string url)
        {
            Movie movie = new Movie();
            var web = new HtmlWeb();
            var doc = web.Load(url);

            movie.ContentId = movieCode.Replace("-", "00").ToLower();
            movie.Code = movieCode;

            // title
            var titleNode = doc.DocumentNode.SelectNodes("//*[@itemprop='name']").FirstOrDefault();
            movie.Title = titleNode.Attributes["href"].Value;

            // duration
            var durationNode = doc.DocumentNode.SelectNodes("//*[@itemprop='duration']").FirstOrDefault();
            movie.Duration = durationNode.Attributes["href"].Value;

            // director
            var directorNode = doc.DocumentNode.SelectNodes("//*[@itemprop='director']").FirstOrDefault();
            movie.Director = durationNode.Attributes["href"].Value;

            // uploadDate
            var uploadDateNode = doc.DocumentNode.SelectNodes("//*[@itemprop='dateCreated']").FirstOrDefault();
            string uploadDate = uploadDateNode.Attributes["href"].Value;
            if (!string.IsNullOrEmpty(uploadDate))
            {
                movie.UploadDate = DateTime.Parse(uploadDate);
            }

            // maker
            var makerNode = doc.DocumentNode.SelectNodes("//*[@itemprop='productionCompany']").FirstOrDefault();
            movie.Maker = makerNode.Attributes["href"].Value;

            // preview
            var previewImageNode = doc.DocumentNode.SelectNodes("//*[@class='box01 mb10 detail-view detail-single-picture']").FirstOrDefault();
            movie.PreviewImage = previewImageNode.Attributes["href"].Value;

            // actress 
            var actorNodes = doc.DocumentNode.SelectNodes("//*[@itemprop='actors']/a");
            if (actorNodes != null && actorNodes.Count > 0)
            {
                var actresList = doc.DocumentNode.SelectNodes("//*[@itemprop='actors']/a").Select(n => n.InnerText.Trim()).ToList();
                List<Actres> actress = new List<Actres>();
                foreach (var actorNode in actresList)
                {
                    Actres actres = new Actres
                    {
                        MovieCode = movieCode,
                        Name = actorNode
                    };
                    actress.Add(actres);
                }
                movie.Actress = actress;
            }

            // categories 
            var categoryNodes = doc.DocumentNode.SelectNodes("//*[class='popup']/a").Select(n => n.InnerText.Trim()).ToList();
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

            // images
            List<MoviePicture> imagesUrls = new List<MoviePicture>();
            var imagesBlockNodes = doc.DocumentNode.SelectNodes("//*[@class='product-gallery preview-grid']/li");
            var imagesUrl = "https://pics.r18.com/digital/video/{0}/{0}jp-{1}.jpg";
            if (imagesBlockNodes != null)
            {
                var imagesNodes = imagesBlockNodes.ToList();
                for (int i = 1; i < imagesNodes.Count + 1; i++)
                {
                    MoviePicture picture = new MoviePicture
                    {
                        MovieCode = movieCode,
                        Url = string.Format(imagesUrl, movie.ContentId, i),
                        Preview = false
                    };
                    imagesUrls.Add(picture);
                }
            }
            movie.Pictures = imagesUrls;

            Console.WriteLine("完成数据采集!");
            return movie;
        }

        public async Task<Movie> ScraperMovie(string movieCode)
        {
            Movie movie = new Movie();
            var url = Search(movieCode);
            if (!string.IsNullOrEmpty(url))
            {
                movie = Scraper(movieCode, url);
            }
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
