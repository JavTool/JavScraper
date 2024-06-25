using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace JavScraper.Domain
{
    [Table("Movies")]
    public class Movie
    {
        /// <summary>
        /// 唯一标识。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 编码。
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 内容标识。
        /// </summary>
        public string ContentId { get; set; }

        /// <summary>
        /// 标题。
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string Title { get; set; }

        /// <summary>
        /// 描述。
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 长度。
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// 预览图片。
        /// </summary>
        public string PreviewImage { get; set; }

        /// <summary>
        /// 图片。
        /// </summary>
        public virtual List<MoviePicture> Pictures { get; set; }

        /// <summary>
        /// 導演。
        /// </summary>
        public string Director { get; set; }

        /// <summary>
        /// 系列。
        /// </summary>
        public string Serie { get; set; }

        /// <summary>
        /// 制作。
        /// </summary>
        public string Maker { get; set; }

        /// <summary>
        /// 标签。
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 厂牌。
        /// </summary>
        public string BrandModel { get; set; }

        /// <summary>
        /// 演员。
        /// </summary>
        public virtual List<Actres> Actress { get; set; }

        /// <summary>
        /// 分类。
        /// </summary>
        public virtual List<Category> Categories { get; set; }

        /// <summary>
        /// 上传日期。
        /// </summary>
        public DateTime? UploadDate { get; set; }

    }
}
