using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Domain
{
    /// <summary>
    /// 图片人脸中心点位置
    /// </summary>
    public class ImageFaceCenterPoint
    {
        /// <summary>
        /// url 地址
        /// </summary>
        [BsonId]
        public string Url { get; set; }

        /// <summary>
        /// 中心点位置
        /// </summary>
        public double Point { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Created { get; set; }
    }
}
