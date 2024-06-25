using HtmlAgilityPack;
using JavScraper.Scrapers;
using JavScraper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace JavScraper.Scrapers
{
    public class DmmScraper : AbstractScraper
    {
        private readonly static string filePath = @"H:\AV\";
        private readonly static string savePath = @"H:\Test\";

        /// <summary>
        /// 适配器名称。
        /// </summary>
        public override string Name => "DMM";

        /// <summary>
        /// 初始化 <seealso cref="DmmScraper"/> 类的新实例。
        /// </summary>
        /// <param name="loggerFactory"><seealso cref="ILoggerFactory"/> 对象实例。</param>
        public DmmScraper(ILoggerFactory loggerFactory) :
            base("https://www.dmm.co.jp/", loggerFactory.CreateLogger<DmmScraper>())
        {
        }

        /// <summary>
        /// Dmm 搜索。
        /// </summary>
        /// <param name="keyword">搜索关键字。</param>
        /// <returns></returns>
        public string Search(string keyword)
        {
            string searchUrl = string.Format("https://www.dmm.co.jp/mono/dvd/-/search/=/searchstr={0}/", keyword);
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

        public Movie Scraper(string movieCode, string url)
        {
            Movie movie = new Movie();
            var web = new HtmlWeb();
            var doc = web.Load(url);

            movie.ContentId = movieCode.Replace("-", "00").ToLower();
            movie.Code = movieCode;
            // title
            var titleNode = doc.DocumentNode.SelectNodes("//*[@id='title']").FirstOrDefault();
            movie.Title = titleNode.InnerText.Trim();
            Console.WriteLine(titleNode.InnerText.Trim());

            var performerNode = doc.DocumentNode.SelectNodes("//*[@id='performer']").FirstOrDefault();
            Console.WriteLine(performerNode.InnerText.Trim());

            //var categoryNode = doc.DocumentNode.SelectSingleNode("//*[@id='mu']/div/table/tbody/tr/td[1]/table/tbody/tr[8]");
            //if (categoryNode != null)
            //{
            //    var childNodes = categoryNode.ChildNodes[1].ChildNodes;
            //}
            // images
            var previewImageNode = doc.DocumentNode.SelectNodes("//*[@name='package-image']");
            if (previewImageNode != null)
            {
                movie.PreviewImage = previewImageNode.FirstOrDefault().Attributes["href"].Value.Trim();
                //Console.WriteLine(previewImageNode.FirstOrDefault().Attributes["href"].Value.Trim());
                //DownloadImage(previewImageNode.FirstOrDefault().Attributes["href"].Value, saveDir);
            }

            // description
            var descriptionNode = doc.DocumentNode.SelectSingleNode("//*[@class='mg-b20 lh4']");
            movie.Description = descriptionNode.InnerText.Trim();
            Console.WriteLine(descriptionNode.InnerText.Trim());

            var movieInfoNodes = doc.DocumentNode.SelectNodes("//*[@class='mg-b20']/tr");

            if (movieInfoNodes != null && movieInfoNodes.Count > 0)
            {

                foreach (var movieInfoNode in movieInfoNodes)
                {
                    if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "発売日：")
                    {
                        var uploadDate = movieInfoNode.ChildNodes[3].InnerText.Trim();
                        if (!string.IsNullOrEmpty(uploadDate))
                        {
                            movie.UploadDate = DateTime.Parse(uploadDate);
                        }
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "収録時間：")
                    {
                        int.TryParse(movieInfoNode.ChildNodes[3].InnerText.Trim().Replace("分", ""), out int minutes);
                        TimeSpan timeSpan = new TimeSpan(0, minutes, 0);
                        movie.Duration = string.Format("{0}:{1}:{2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "出演者：")
                    {
                        var actorNodes = doc.DocumentNode.SelectNodes("//*[@id='performer']/a");
                        if (actorNodes != null && actorNodes.Count > 0)
                        {
                            var actresList = doc.DocumentNode.SelectNodes("//*[@id='performer']").Select(n => n.InnerText).ToList();
                            List<Actres> actress = new List<Actres>();
                            foreach (var actorNode in actresList)
                            {
                                Actres actres = new Actres
                                {
                                    MovieCode = movieCode,
                                    Name = actorNode.Trim()
                                };
                                actress.Add(actres);
                            }
                            movie.Actress = actress;
                        }
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "監督：") // 导演
                    {
                        movie.Director = movieInfoNode.ChildNodes[3].InnerText.Trim();
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "シリーズ：") // 系列
                    {
                        movie.Serie = movieInfoNode.ChildNodes[3].InnerText.Trim();
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "メーカー：：") // 制作者
                    {
                        movie.Maker = movieInfoNode.ChildNodes[3].InnerText.Trim();
                    }
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "ジャンル：")
                    {
                        var categoryNodes = movieInfoNode.ChildNodes[3].ChildNodes.Select(n => n.InnerText).Where(s => s != "&nbsp;").ToList();
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
                    else if (movieInfoNode.ChildNodes[1].InnerText.Trim() == "品番：")
                    {
                    }
                    else
                    {
                    }
                }

            }

            // images
            List<MoviePicture> imagesUrls = new List<MoviePicture>();
            var imagesBlockNodes = doc.DocumentNode.SelectNodes("//*[@name='sample-image']/img");
            var imagesUrl = "https://pics.dmm.co.jp/digital/video/{0}/{0}jp-{1}.jpg";
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
            if (!string.IsNullOrEmpty(url)) { movie = Scraper(movieCode, url); }
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
