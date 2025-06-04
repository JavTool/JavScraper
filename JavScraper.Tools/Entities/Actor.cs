using System;

namespace JavScraper.Tools.Entities
{
    /// <summary>
    /// 演员核心信息
    /// </summary>
    public class Actor
    {
        public int ActorId { get; set; } // 主键
        public string BirthPlace { get; set; } // 出生地
        public int BirthYear { get; set; } // 出生年份
        public string BirthDate { get; set; } // 出生日期 (ISO8601格式)
        public string Gender { get; set; } // 性别
        public string Nationality { get; set; } // 国籍
        public string Profile { get; set; } // 简介
        public int? Height { get; set; } // 身高 (cm)
        public int? Weight { get; set; } // 体重 (kg)
    }
} 