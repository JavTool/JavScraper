using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JavScraper.Domain
{
    [Table("MoviePictures")]
    public class MoviePicture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieCode { get; set; }
        public string Url { get; set; }

        public bool Preview { get; set; }
    }
}
