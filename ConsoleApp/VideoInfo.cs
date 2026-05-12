using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class VideoInfo
    {
        public List<string> Actors { get; set; } = new List<string>();

        public string Number { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string CoverUrl { get; set; }
        public string Magnet { get; set; }
        public string SourceUrl { get; set; }
    }
}
