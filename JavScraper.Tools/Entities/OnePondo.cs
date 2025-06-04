using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.Tools.Entities
{
    public class OnePondoVideo
    {
        public string MovieID { get; set; }
        public string Title { get; set; }
        public string TitleEn { get; set; }
        public string Actor { get; set; }
        public string Release { get; set; }
        public double AvgRating { get; set; }
        public string Desc { get; set; }
        public string DescEn { get; set; }
        public string ThumbHigh { get; set; }
        public string MovieThumb { get; set; }
        public int Duration { get; set; }
        public List<string> UCNAME { get; set; }
        public List<VideoFile> MemberFiles { get; set; }
        public List<VideoFile> SampleFiles { get; set; }
        public Dictionary<string, ActressInfo> ActressesList { get; set; }

    }
    public class ActressInfo
    {
        public string NameJa { get; set; }
        public string NameEn { get; set; }
        public string Sizes { get; set; }
        public int Age { get; set; }
    }
    public class VideoFile
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string URL { get; set; }
    }
}
