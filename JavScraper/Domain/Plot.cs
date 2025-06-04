using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace JavScraper.Domain
{
    /// <summary>
    /// 影片情节信息
    /// </summary>
    public class Plot
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
        /// 去掉下划线和横线的番号
        /// </summary>
        public string Num { get; set; }

        /// <summary>
        /// 链接地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 简介
        /// </summary>
        public string Plots { get; set; }

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
