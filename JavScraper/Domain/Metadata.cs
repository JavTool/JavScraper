using JavScraper.Scrapers;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace JavScraper.Domain
{
    /// <summary>
    /// 影片元数据
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// id
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; }

        /// <summary>
        /// 适配器
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 番号
        /// </summary>
        public string Num { get; set; }

        /// <summary>
        /// 链接地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public JavVideo Data { get; set; }

        /// <summary>
        /// 最后选中时间
        /// </summary>
        public DateTime? Selected { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Created { get; set; }
    }
}
