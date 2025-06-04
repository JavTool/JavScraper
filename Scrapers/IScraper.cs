using JavScraper.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Scrapers
{
    public interface IScraper
    {
        /// <summary>
        /// 采集器名称。
        /// </summary>
        public abstract string Name { get; }




        string Search(string keyword);

        Movie Scraper(string name, string url);

        Task<Movie> ScraperMovie(string movieCode);
    }
}
