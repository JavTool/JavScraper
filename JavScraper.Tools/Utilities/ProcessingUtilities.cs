using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JavScraper.Tools.Models;

namespace JavScraper.Tools.Utilities
{
    /// <summary>
    /// JAV ID 提取器。
    /// </summary>
    public static class JavIdExtractor
    {
        private static readonly Regex[] JavIdPatterns = new[]
        {
            new Regex(@"([A-Z]{2,10}-\d{3,5})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"([A-Z]{2,10}\d{3,5})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(\d{6}[-_]\d{3})", RegexOptions.Compiled),
            new Regex(@"([A-Z]+[-_]\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(\w+[-_]\w+[-_]\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private static readonly Regex NfoJavIdPattern = new(
            @"<num>([^<]+)</num>|<id>([^<]+)</id>|<uniqueid[^>]*>([^<]+)</uniqueid>", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 从字符串中提取 JAV ID。
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>提取的 JAV ID，如果未找到返回空字符串</returns>
        public static string ExtractJavId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // 清理输入字符串
            var cleanInput = CleanInputString(input);

            // 尝试各种模式
            foreach (var pattern in JavIdPatterns)
            {
                var match = pattern.Match(cleanInput);
                if (match.Success)
                {
                    var javId = match.Groups[1].Value.ToUpperInvariant();
                    if (IsValidJavId(javId))
                    {
                        return javId;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 从 NFO 内容中提取 JAV ID。
        /// </summary>
        /// <param name="nfoContent">NFO 文件内容</param>
        /// <returns>提取的 JAV ID，如果未找到返回空字符串</returns>
        public static string ExtractJavIdFromNfoContent(string nfoContent)
        {
            if (string.IsNullOrWhiteSpace(nfoContent))
            {
                return string.Empty;
            }

            var matches = NfoJavIdPattern.Matches(nfoContent);
            foreach (Match match in matches)
            {
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (match.Groups[i].Success)
                    {
                        var javId = match.Groups[i].Value.Trim().ToUpperInvariant();
                        if (IsValidJavId(javId))
                        {
                            return javId;
                        }
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 清理输入字符串。
        /// </summary>
        private static string CleanInputString(string input)
        {
            // 移除常见的前缀和后缀
            var cleanInput = input
                .Replace("[中字]", "")
                .Replace("[无码]", "")
                .Replace("[中字无码]", "")
                .Replace("Un Censored", "")
                .Replace("Uncensored", "")
                .Trim();

            return cleanInput;
        }

        /// <summary>
        /// 验证 JAV ID 是否有效。
        /// </summary>
        private static bool IsValidJavId(string javId)
        {
            if (string.IsNullOrWhiteSpace(javId))
            {
                return false;
            }

            // 长度检查
            if (javId.Length < 3 || javId.Length > 15)
            {
                return false;
            }

            // 必须包含字母和数字
            if (!javId.Any(char.IsLetter) || !javId.Any(char.IsDigit))
            {
                return false;
            }

            // 排除明显无效的模式
            var invalidPatterns = new[]
            {
                @"^\d+$", // 纯数字
                @"^[A-Z]+$", // 纯字母
                @"^.{1,2}$" // 太短
            };

            return !invalidPatterns.Any(pattern => Regex.IsMatch(javId, pattern));
        }
    }

    /// <summary>
    /// 标题清理器。
    /// </summary>
    public static class TitleCleaner
    {
        private static readonly string[] JapaneseTermsToRemove = new[]
        {
            "お姉さま", "マンコ図鑑", "剃毛・パイパン", "看護婦", "網タイツ", "お尻",
            "初登場", "野外・露出", "全身ローション", "顔面騎乗", "美マン", "女優1人",
            "角色扮演", "乳液", "潮吹", "潮吹き", "天然むすめ", "3P", "打手槍",
            "近親○姦", "浪叫", "立ちバック", "美少女", "女優2人", "強烈ピストンバック",
            "ゴム無し・ピストンバック", "左右口交", "淫語", "モデル系", "ハメ撮り",
            "69", "ギャル系", "口内発射", "水着", "素人", "美白", "顔射", "電マ",
            "手コキ", "おもちゃ", "指マン", "ディルド", "バイブ", "ローター", "東京熱",
            "淫蕩手淫", "寫真", "苗條", "スレンダー", "美腳", "美脚", "美尻",
            "ゴム無し", "バキュームフェラ", "痴女", "オナニー", "クンニ", "美乳",
            "美女・美人", "美肌・美白", "巨乳爆乳", "知名女優", "背面騎乗位",
            "背後插入", "立即口交", "生姦・ゴム無し", "完全無修正", "戲劇", "深喉",
            "ドラマ・ストーリー", "中出し", "乱交", "パイズリ", "フェラチオ",
            "生ハメ・生姦", "ぶっかけ", "駅弁", "駅弁ファック", "駅弁ファック・駅弁",
            "アナルファック", "アナルセックス", "アナル舐め", "フェラ", "VR", "縛り",
            "ロリ", "SM", "イラマチオ", "生ハメ", "人妻", "熟女", "熟女/人妻",
            "初裏", "中文字幕", "中字", "足コキ", "着エロ", "セフレ", "OL",
            "むっちり", "清楚", "惡搞", "マッサージ", "ローション", "背部騎乘位",
            "激エロオナニー", "口内射精", "美尻・ケツがいい", "美乳・素敵なオッパイ",
            "人妻・奥様", "細身・スレンダー", "立即騎乘", "変態", "痴漢", "目隠し",
            "アナル", "スーツ", "女優3人", "長身", "ギャル", "コスプレ", "ぽっちゃり",
            "メイド", "微乳", "巨乳・爆乳・超乳", "痴女・淫乱", "マニアックコスプレ",
            "着物・和服・浴衣", "メガネな女", "ボンテージ", "猿轡", "男のアナルを舐める",
            "アナルバック", "フェチ", "異物挿入", "ベランダ", "ママ", "人妻・熟女",
            "美脚・美足", "美乳・巨乳", "美尻・美臀", "女優4人", "女優5人",
            "女優6人", "女優7人", "女優8人", "女優9人", "浴衣・着物", "バス",
            "制服", "コスチューム", "コスプレ・コスチューム", "コスプレ・制服",
            "コスプレ・着物", "コスプレ・浴衣", "コスプレ・和服", "コスプレ・メイド",
            "コスプレ・女仆", "美脚・キレイな足", "生中出し", "女子校生", "無套內射",
            "ごっくん", "精液ごっくん", "女教師・家庭教師", "漂亮屁股", "雪白皮膚",
            "雪白的皮肤", "騎乘位", "口爆", "淫荡手淫", "淋浴沐浴", "口内射精ごっくん",
            "ハード系", "バック", "連続中出し", "輪姦", "漁網褲襪", "ナンパ",
            "ランジェリー", "パイパン", "ハイヒール", "グラマラス", "洋物", "妄想",
            "小柄", "温泉", "S級女優", "多選提交", "エプロン", "超VIP", "多P",
            "めがね", "女教師", "辱め", "白领", "働きウーマン", "風呂", "シャワー",
            "家", "ソファー", "ストッキング", "スパッツ", "ホテル", "リップサービス",
            "レギンス", "站立位", "パンスト", "セクシー", "彼女", "淫乱", "車",
            "車内", "シックスナイン", "スタイル抜群モデル級", "スポーツコスプレ",
            "フェラ抜き", "フェロモン", "清掃員", "二穴同插", "護士", "掃除",
            "巨大阳具", "黒人", "講座", "美人", "美肌", "アイドル", "ザーメン",
            "再会", "即ハメ", "旅行", "椅子", "絶叫", "不倫・浮気", "玄関",
            "宅配", "ご近所", "裸エプロン", "テレフォンセックス", "誘惑", "欲求不満",
            "面接", "レズ", "マンコぶっかけ", "和室", "素股", "女医", "極上泡姫物語",
            "拘束", "お隣さん", "セックスレス", "元モデル", "ヒミツのアルバイト",
            "女二人でダブルフェラ", "お風呂", "リゾート", "性奴", "剃毛", "引っ越し",
            "恍惚", "家事", "電球", "義兄", "ノーパン", "ノーブラ", "過激痴漢",
            "ご主人様", "ご奉仕", "バニーガール", "風俗", "クリスマスイブ", "サンタ",
            "ソープ", "デリヘル嬢", "人気女優", "レンタルガール", "お嫁さん",
            "婚紗禮服", "家事代行", "妻の女友達", "ときめき", "奥手", "Tバック",
            "アフター6", "痴女與M男", "乳首マッサージ", "調教", "元アナウンサー",
            "喘ぎ声", "白昼", "年間", "企画物", "南國度假地", "戶外", "1v1性交",
            "1対1セックス", "トイレ", "ドッキリ", "ファン", "电动按摩器", "精緻身材",
            "ベッド", "ソープランドプレイ", "ぶっかけ・輪姦", "中出", "３P", "ソファ",
            "Wフェラ", "女上位", "騎乗位", "打底裤", "浴衣和服", "指法", "ロリータ",
            "亂倫", "束縛", "蘿莉", "昼間", "M男", "ロリ系"
        };

        /// <summary>
        /// 移除标题中的日文术语。
        /// </summary>
        /// <param name="title">原始标题</param>
        /// <returns>清理后的标题</returns>
        public static string RemoveJapaneseTerms(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            var cleanedTitle = title;

            foreach (var term in JapaneseTermsToRemove)
            {
                cleanedTitle = cleanedTitle.Replace(term, "", StringComparison.OrdinalIgnoreCase);
            }

            // 清理多余的空格和标点
            cleanedTitle = Regex.Replace(cleanedTitle, @"\s+", " ").Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, @"[\s]*[,，;；]+[\s]*", ", ");
            cleanedTitle = cleanedTitle.Trim(' ', ',', '，', ';', '；');

            return cleanedTitle;
        }

        /// <summary>
        /// 清理文件名中的无效字符。
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <returns>清理后的文件名</returns>
        public static string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            // 移除或替换无效字符
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanedName = fileName;

            foreach (var invalidChar in invalidChars)
            {
                cleanedName = cleanedName.Replace(invalidChar, '_');
            }

            // 移除日文术语
            cleanedName = RemoveJapaneseTerms(cleanedName);

            // 清理多余的空格和下划线
            cleanedName = Regex.Replace(cleanedName, @"[\s_]+", "_").Trim('_');

            return cleanedName;
        }
    }

    /// <summary>
    /// NFO 文件工具类。
    /// </summary>
    public static class NfoFileUtilities
    {
        /// <summary>
        /// 获取按目录分组的 NFO 文件。
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="searchPattern">搜索模式，默认为 "*.nfo"</param>
        /// <returns>分组的 NFO 文件列表</returns>
        public static List<NfoFileGroup> GetNfoFilesGroupedByDirectory(string rootPath, string searchPattern = "*.nfo")
        {
            var groups = new List<NfoFileGroup>();

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return groups;
            }

            try
            {
                var nfoFiles = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories)
                    .Where(f => !Path.GetFileName(f).EndsWith("bak.nfo", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var groupedFiles = nfoFiles
                    .GroupBy(f => Path.GetDirectoryName(f))
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToList();

                foreach (var group in groupedFiles)
                {
                    var directoryPath = group.Key!;
                    var directoryName = Path.GetFileName(directoryPath);
                    var files = group.ToList();

                    var fileGroup = new NfoFileGroup
                    {
                        DirectoryName = directoryName,
                        DirectoryPath = directoryPath,
                        NfoFiles = files
                    };

                    // 查找主要的 NFO 文件
                    var movieNfo = files.FirstOrDefault(f => 
                        Path.GetFileName(f).Equals("movie.nfo", StringComparison.OrdinalIgnoreCase));
                    
                    fileGroup.PrimaryNfoFile = movieNfo ?? files.FirstOrDefault() ?? string.Empty;

                    groups.Add(fileGroup);
                }
            }
            catch (Exception)
            {
                // 忽略异常，返回空列表
            }

            return groups.OrderBy(g => g.DirectoryName).ToList();
        }

        /// <summary>
        /// 检查 NFO 文件是否已被处理过。
        /// </summary>
        /// <param name="nfoFilePath">NFO 文件路径</param>
        /// <returns>是否已被处理</returns>
        public static bool IsNfoFileProcessed(string nfoFilePath)
        {
            if (string.IsNullOrWhiteSpace(nfoFilePath) || !File.Exists(nfoFilePath))
            {
                return false;
            }

            try
            {
                var content = File.ReadAllText(nfoFilePath);
                
                // 检查是否包含处理标记
                var processedMarkers = new[]
                {
                    "<!-- Processed by JavScraper -->",
                    "<processed>true</processed>",
                    "[中字无码]",
                    "[无码]"
                };

                return processedMarkers.Any(marker => 
                    content.Contains(marker, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取高分辨率图片 URL。
        /// </summary>
        /// <param name="baseUrl">基础 URL</param>
        /// <returns>高分辨率图片 URL 列表</returns>
        public static List<string> GetHighResolutionImageUrls(string baseUrl)
        {
            var urls = new List<string>();

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return urls;
            }

            try
            {
                // 从 URL 中提取视频 ID
                var videoIdMatch = Regex.Match(baseUrl, @"/([A-Z0-9-]+)/", RegexOptions.IgnoreCase);
                if (!videoIdMatch.Success)
                {
                    return urls;
                }

                var videoId = videoIdMatch.Groups[1].Value;
                var baseUrlPart = baseUrl.Substring(0, baseUrl.LastIndexOf('/'));

                // 生成不同分辨率的图片 URL
                var resolutions = new[] { "1920x1080", "1280x720", "800x600", "640x480" };
                var formats = new[] { "jpg", "jpeg", "png" };

                foreach (var resolution in resolutions)
                {
                    foreach (var format in formats)
                    {
                        urls.Add($"{baseUrlPart}/{videoId}_{resolution}.{format}");
                        urls.Add($"{baseUrlPart}/{videoId}-{resolution}.{format}");
                    }
                }
            }
            catch (Exception)
            {
                // 忽略异常
            }

            return urls;
        }
    }
}