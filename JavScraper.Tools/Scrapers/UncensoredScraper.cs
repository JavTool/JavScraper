using JavScraper.Tools.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Tools.Scrapers
{
    /// <summary>
    /// 无修正视频
    /// </summary>
    public class UncensoredScraper : AbstractScraper
    {
        public UncensoredScraper(string base_url, ILogger log) : base(base_url, log)
        {
        }

        public override bool CheckKeyword(string keyword)
        {
            throw new NotImplementedException();
        }

        public override Task<List<JavVideo>> ParseList(string url)
        {
            throw new NotImplementedException();
        }

        public override Task<JavVideo> ParsePage(string url)
        {
            throw new NotImplementedException();
        }



    }
}
