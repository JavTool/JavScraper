﻿using System;
using JavScraper.Domain;

namespace JavScraper.Scrapers
{
    /// <summary>
    /// 人员索引。
    /// </summary>
    public class JavPersonIndex
    {
        /// <summary>
        /// 适配器。
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 图像类型。
        /// </summary>
        public ImageType? ImageType { get; set; }

        /// <summary>
        /// 转换为字符串。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Url}";
    }
}
