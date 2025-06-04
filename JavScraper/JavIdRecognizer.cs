using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JavScraper
{
    /// <summary>
    /// 番号识别。
    /// </summary>
    public static class JavIdRecognizer
    {
        private static RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        private static Func<string, JavId>[] funcs = new Func<string, JavId>[] {
            Carib,Heyzo,
            FC2,Musume,
            OnlyNumber
        };

        /// <summary>
        /// 移除视频编码 1080p,720p 2k 之类的
        /// </summary>
        private static Regex p1080p = new Regex(@"(^|[^\d])(?<p>[\d]{3,5}p|[\d]{1,2}k)($|[^a-z])", options);

        /// <summary>
        /// 转换。
        /// </summary>
        /// <param name="name">文件名称。</param>
        /// <returns></returns>
        public static JavId Parse(string name)
        {
            name = name.Replace("_", "-").Replace(" ", "-").Replace(".", "-");

            var m = p1080p.Match(name);
            while (m.Success)
            {
                name = name.Replace(m.Groups["p"].Value, "");
                m = m.NextMatch();
            }

            foreach (var func in funcs)
            {
                var r = func(name);
                if (r != null)
                    return r;
            }

            name = Regex.Replace(name, @"ts6[\d]+", "", options);
            name = Regex.Replace(name, @"-*whole\d*", "", options);
            name = Regex.Replace(name, @"-*full$", "", options);
            name = name.Replace("tokyo-hot", "", StringComparison.OrdinalIgnoreCase);
            name = name.TrimEnd("-C").TrimEnd("-HD", "-full", "full").TrimStart("HD-").TrimStart("h-");
            name = Regex.Replace(name, @"\d{2,4}-\d{1,2}-\d{1,2}", "", options); //日期
            name = Regex.Replace(name, @"(.*)(00)(\d{3})", "$1-$3", options); //FANZA cid AAA00111
            // 标准 AAA-111
            m = Regex.Match(name, @"(^|[^a-z0-9])(?<id>[a-z0-9]{2,10}-[\d]{2,8})($|[^\d])", options);
            if (m.Success && m.Groups["id"].Value.Length >= 4)
                return m.Groups["id"].Value;
            // 第二段带字母 AAA-B11
            m = Regex.Match(name, @"(^|[^a-z0-9])(?<id>[a-z]{2,10}-[a-z]{1,5}[\d]{2,8})($|[^\d])", options);
            if (m.Success && m.Groups["id"].Value.Length >= 4)
                return m.Groups["id"].Value;
            // 没有横杠的 AAA111
            m = Regex.Match(name, @"(^|[^a-z0-9])(?<id>[a-z]{1,10}[\d]{2,8})($|[^\d])", options);
            if (m.Success && m.Groups["id"].Value.Length >= 4)
                return m.Groups["id"].Value;

            return null;
        }

        private static Regex[] regexMusume = new Regex[] {
            new Regex(@"(?<id>[\d]{4,8}-[\d]{1,6})-(10mu)",options),
            new Regex(@"(10Musume)-(?<id>[\d]{4,8}-[\d]{1,6})",options)
        };

        private static JavId Musume(string name)
        {
            foreach (var regex in regexMusume)
            {
                var m = regex.Match(name);
                if (m.Success)
                    return new JavId()
                    {
                        Matcher = nameof(Musume),
                        Type = JavIdType.Amateur,
                        Id = m.Groups["id"].Value.Replace("_", "-")
                    };
            }
            return null;
        }

        private static Regex[] regexCarib = new Regex[] {
            new Regex(@"(?<id>[\d]{4,8}-[\d]{1,6})-(1pon|carib|paco|mura)",options),
            new Regex(@"(1Pondo|Caribbean|Pacopacomama|muramura)-(?<id>[\d]{4,8}-[\d]{1,8})($|[^\d])",options)
        };

        private static JavId Carib(string name)
        {
            foreach (var regex in regexCarib)
            {
                var m = regex.Match(name);
                if (m.Success)
                    return new JavId()
                    {
                        Matcher = nameof(Carib),
                        Type = JavIdType.Uncensored,
                        Id = m.Groups["id"].Value.Replace("-", "_")
                    };
            }
            return null;
        }

        private static Regex regexHeyzo = new Regex(@"Heyzo(|-| |.com)(HD-|)(?<id>[\d]{2,8})($|[^\d])", options);

        private static JavId Heyzo(string name)
        {
            var m = regexHeyzo.Match(name);
            if (m.Success == false)
                return null;
            var id = $"heyzo-{m.Groups["id"]}";
            return new JavId()
            {
                Matcher = nameof(Heyzo),
                Id = id,
                Type = JavIdType.Uncensored
            };
        }

        private static Regex regexFC2 = new Regex(@"FC2-*(PPV|)[^\d]{1,3}(?<id>[\d]{2,10})($|[^\d])", options);

        /// <summary>
        /// 识别 FC2 番号。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static JavId FC2(string name)
        {
            var m = regexFC2.Match(name);
            if (m.Success == false)
                return null;
            var id = $"fc2-ppv-{m.Groups["id"]}";
            return new JavId()
            {
                Id = id,
                Matcher = nameof(FC2),
                Type = JavIdType.Amateur
            };
        }

        private static Regex regexNumber = new Regex(@"(?<id>[\d]{6,8}-[\d]{1,6})", options);

        private static JavId OnlyNumber(string name)
        {
            var m = regexNumber.Match(name);
            if (m.Success == false)
                return null;
            var id = m.Groups["id"].Value;
            return new JavId()
            {
                Matcher = nameof(OnlyNumber),
                Id = id
            };
        }

        // DoctorAdventures.19.12.11
        private static Regex regexWestern = new Regex(@"(?<id>[a-zA-Z]+.[\d]{2}.[\d]{2}.[\d]{2})", options);
        /// <summary>
        /// 匹配欧美
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static JavId Western(string name)
        {
            var m = regexWestern.Match(name);
            if (m.Success == false)
                return null;
            var id = m.Groups["id"].Value;
            return new JavId()
            {
                Matcher = nameof(Western),
                Id = id
            };
        }
    }

    /// <summary>
    /// 番号。
    /// </summary>
    public class JavId
    {
        /// <summary>
        /// 类型。
        /// </summary>
        public JavIdType Type { get; set; }

        /// <summary>
        /// 解析到的 Id。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// 匹配器。
        /// </summary>
        public string Matcher { get; set; }

        /// <summary>
        /// 转换为字符串。
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Id;

        /// <summary>
        /// 转换。
        /// </summary>
        /// <param name="id"></param>
        public static implicit operator JavId(string id) => new JavId() { Id = id };

        /// <summary>
        /// 转换。
        /// </summary>
        /// <param name="id"></param>
        public static implicit operator string(JavId id) => id?.Id;

        /// <summary>
        /// 识别。
        /// </summary>
        /// <param name="file">文件路径。</param>
        /// <returns></returns>
        public static JavId Parse(string file)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var id = JavIdRecognizer.Parse(name);
            if (id != null)
                id.File = file;
            return id;
        }
    }

    /// <summary>
    /// 类型。
    /// </summary>
    public enum JavIdType
    {
        /// <summary>
        /// 不确定。
        /// </summary>
        None,
        /// <summary>
        /// 审查。
        /// </summary>
        Censored,
        /// <summary>
        /// 未经审查。
        /// </summary>
        Uncensored,
        /// <summary>
        /// 素人。
        /// </summary>
        Amateur
    }
}
