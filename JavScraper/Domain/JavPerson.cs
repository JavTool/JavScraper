using System;
using System.Collections.Generic;

namespace JavScraper.Domain
{
    /// <summary>
    /// 演员信息
    /// </summary>
    public class JavPerson
    {
        /// <summary>
        /// 演员名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 演员主页地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 生日
        /// </summary>
        public string Birthday { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// 身高（厘米）
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// 罩杯
        /// </summary>
        public string Cup { get; set; }

        /// <summary>
        /// 胸围（厘米）
        /// </summary>
        public int? Bust { get; set; }

        /// <summary>
        /// 腰围（厘米）
        /// </summary>
        public int? Waist { get; set; }

        /// <summary>
        /// 臀围（厘米）
        /// </summary>
        public int? Hip { get; set; }

        /// <summary>
        /// 出生地
        /// </summary>
        public string Birthplace { get; set; }

        /// <summary>
        /// 爱好
        /// </summary>
        public List<string> Hobbies { get; set; }

        /// <summary>
        /// 头像图片地址
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// 写真图片地址列表
        /// </summary>
        public List<string> PhotoUrls { get; set; }

        /// <summary>
        /// 简介
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString() => Name;
    }
}