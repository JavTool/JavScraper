namespace JavScraper.Tools.Entities
{
    /// <summary>
    /// 演员名称表（多语言支持）
    /// </summary>
    public class ActorName
    {
        public int NameId { get; set; } // 主键
        public int ActorId { get; set; } // 外键，关联到 Actor 表
        public string Name { get; set; } // 演员名称
        public string LanguageCode { get; set; } // 语言代码 (ISO639-1)
        public string NameType { get; set; } // 名称类型 (primary, alias, former, stage)
        public bool IsPrimary { get; set; } // 是否为主名称
    }
} 