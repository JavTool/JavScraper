using JavScraper.Tools.Entities;
using JavScraper.Tools.Http;
using JavScraper.Tools.Tools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO.Compression;
using static JavScraper.Tools.ImageUtils;
using JavScraper.Tools.Utils;

namespace JavScraper.Tools
{
    public class NfoDataFIx
    {
        #region 声明静态配置...


        private readonly static Dictionary<string, string> avTerms = new()
            {
          { "お姉さま","姐姐"},
          { "マンコ図鑑","私部特写"},
            { "剃毛・パイパン", "白虎" },
            { "看護婦", "护士" },
            { "網タイツ","网袜"},
          { "お尻","屁股"},
            { "初登場","首次亮相"},
            { "野外・露出","野外露出"},
            { "全身ローション", "全身乳液" },
            { "顔面騎乗", "颜面骑乘" },
            { "美マン", "美穴" },
            { "女優1人", "单一女优" },
            { "角色扮演", "角色扮演" },
            { "乳液", "乳液" },
            { "潮吹", "潮吹" },
             { "潮吹き", "潮吹" },
            { "天然むすめ", "天然少女" },
            { "3P", "三人行" },
            { "打手槍", "手淫" },
            { "近親○姦", "乱伦" },
            { "浪叫", "呻吟" },
            { "立ちバック", "站姿后入" },
            { "美少女", "美少女" },
            { "女優2人", "双飞" },
            { "強烈ピストンバック", "强烈后入" },
            { "ゴム無し・ピストンバック", "无套后入" },
            { "左右口交", "双人口交" },
            { "淫語", "淫语" },
            { "モデル系", "模特" },
            { "ハメ撮り", "POV" },
            { "69", "69" },
            { "ギャル系", "辣妹" },
            { "口内発射", "口内射精" },
            { "水着", "泳装" },
            { "素人", "素人" },
            { "美白", "美白" },
            { "顔射", "颜射" },
            { "電マ", "电动按摩棒" },
            { "手コキ", "手淫" },
            { "おもちゃ", "玩具" },
            { "指マン", "手指插入" },
            { "ディルド", "假阳具" },
            { "バイブ", "振动棒" },
            { "ローター", "跳蛋" },
            { "東京熱", "东京热" },
            { "淫蕩手淫", "淫荡手淫" },

            { "寫真", "写真" },
            { "苗條", "苗条" },
            { "スレンダー", "苗条" },
            { "美腳", "美腿" },
            { "美脚", "美脚" },
            { "美尻", "美臀" },
            { "ゴム無し", "无套" },
            { "バキュームフェラ", "深喉" },
            { "痴女", "荡妇" },
            { "オナニー", "自慰" },
            { "クンニ", "舔阴" },
            { "美乳", "美乳" },
            { "美女・美人", "美女" },
            { "美肌・美白", "雪白的肌肤" },
            { "巨乳爆乳", "巨乳" },
            { "知名女優", "知名女优" },
            { "背面騎乗位", "反向骑乘位" },
            { "背後插入", "后入" },
            { "立即口交", "即尺" },
            { "生姦・ゴム無し", "无套性交" },
            { "完全無修正", "无码" },
            { "戲劇", "剧情" },
            { "深喉", "深喉" },
            { "ドラマ・ストーリー", "剧情" },
            { "中出し", "内射" },
            { "乱交", "群交" },
            { "パイズリ", "乳交" },
            { "フェラチオ", "口交" },
            { "生ハメ・生姦", "无套性交" },
            { "ぶっかけ", "颜射" },
            { "駅弁", "站立抱姿" },
            { "駅弁ファック","站抱性交"},
            { "駅弁ファック・駅弁", "站立抱姿" },
            { "アナルファック", "肛交" },
            { "アナルセックス", "肛交" },
            { "アナル舐め", "舔肛" },
            { "アナル舐め・アナルセックス", "舔肛" },
            { "アナル舐め・アナルファック", "舔肛" },
            { "アナル舐め・アナルセックス・アナルファック", "舔肛" },
            { "フェラ", "口交" },
            { "VR", "虚拟现实" },
            { "縛り", "捆绑" },
            { "ロリ", "萝莉" },
            { "SM", "SM" },
            { "イラマチオ", "强制口交" },
            { "生ハメ", "无套性交" },
            { "人妻", "人妻" },
            { "熟女", "熟女" },
            { "熟女/人妻", "人妻" },
            { "初裏", "首次无码" },
            { "中文字幕", "中文字幕" },
            { "中字", "中文字幕" },
            { "足コキ", "足交" },
            { "着エロ", "穿着性感" },
            { "セフレ", "炮友" },
            { "OL", "职业装" },
            { "むっちり", "丰满" },
            { "清楚", "清纯" },
            { "惡搞", "恶搞" },
            { "マッサージ", "按摩" },
            { "ローション", "乳液" },
            { "背部騎乘位", "反向骑乘位" },
            { "激エロオナニー", "激情自慰" },
            { "口内射精", "口内射精" },
            { "美尻・ケツがいい", "美臀" },
            { "美乳・素敵なオッパイ", "美乳" },
            { "人妻・奥様", "人妻" },
            { "細身・スレンダー", "苗条" },
            { "立即騎乘", "立即骑乘" },
            { "変態", "变态" },
            { "痴漢", "痴汉" },
            { "目隠し", "蒙眼" },
            { "アナル", "肛交" },
            { "スーツ", "职业装" },
            { "女優3人", "三飞" },
            { "長身", "高挑" },
            { "ギャル", "辣妹" },
            { "コスプレ", "角色扮演" },
            { "ぽっちゃり", "丰满" },
            { "メイド", "女仆" },
            { "微乳", "贫乳" },
            { "巨乳・爆乳・超乳", "巨乳" },
            { "痴女・淫乱", "淫乱痴女" },
            { "マニアックコスプレ", "狂热角色扮演" },
            { "着物・和服・浴衣", "和服" },
            { "メガネな女", "眼镜女" },
            { "ボンテージ", "束缚装" },
            { "猿轡", "口塞" },
            { "男のアナルを舐める", "舔肛" },
            { "アナルバック", "肛交后入" },
            { "フェチ", "恋物癖" },
            { "異物挿入", "异物插入" },
            { "ベランダ", "阳台" },
            { "ママ", "妈妈" },
            { "人妻・熟女", "人妻" },
            { "美脚・美足", "美腿" },
            { "美乳・巨乳", "美乳" },
            { "美尻・美臀", "美臀" },
            { "女優4人", "四飞" },
            { "女優5人", "五飞" },
            { "女優6人", "六飞" },
            { "女優7人", "七飞" },
            { "女優8人", "八飞" },
            { "女優9人", "九飞" },
            { "浴衣・着物","浴衣"},{"バス","巴士" },
            { "制服", "制服" },
            { "コスチューム", "服装" },
            { "コスプレ・コスチューム", "角色扮演" },
            { "コスプレ・制服", "角色扮演" },
            { "コスプレ・着物", "角色扮演" },
            { "コスプレ・浴衣", "角色扮演" },
            { "コスプレ・和服", "角色扮演" },
            { "コスプレ・メイド", "角色扮演" },
            { "コスプレ・女仆", "角色扮演" },
            {"美脚・キレイな足","美腿" },{"生中出し","无套内射" },{"女子校生","女学生" }
            , { "無套內射","无套内射"},{ "ごっくん","吞精"},
            {"精液ごっくん","吞精" },{ "女教師・家庭教師","女教师"}
            ,{ "漂亮屁股","美臀"},
            { "雪白皮膚","雪白的肌肤"},
            { "雪白的皮肤","雪白的肌肤"},
            { "騎乘位","女上位"},{ "口爆","口内射精"},
            { "淫荡手淫","手淫"},
            { "淋浴沐浴","沐浴"},
            {"口内射精ごっくん","口爆吞精" },{ "ハード系","重口味"},{"バック","后入"},{ "連続中出し","连续内射"},{ "輪姦","轮奸"},{ "漁網褲襪","渔网裤袜"},{"ナンパ", "搭讪"},  { "ランジェリー", "内衣" },
    { "パイパン", "白虎" },
    { "ハイヒール", "高跟鞋" },
    { "グラマラス", "性感丰满" },
    { "洋物", "欧美" },
    { "妄想", "幻想" },
    { "小柄", "娇小" },
    { "温泉", "温泉" },
    { "S級女優", "S 级女优" },
    { "多選提交", "多选互动" },
    { "エプロン", "围裙" },
    { "超VIP", "超 VIP" },

    { "多P", "群交" },
    { "めがね", "眼镜" },
    { "女教師", "女教师" },

    { "辱め", "凌辱" },
    { "白领", "白领" },

    { "ラフォーレ ガール", "Laforet Girl" },
    { "働きウーマン", "职业女性" },
    { "風呂", "浴室" },
    { "シャワー", "淋浴" },
    { "家", "家里" },

    { "ソファー", "沙发" },
    { "ストッキング", "长筒袜" },
    { "スパッツ", "紧身裤" },
    { "ホテル", "酒店" },
    { "リップサービス", "唇部服务" },
    { "レギンス", "打底裤" },


    { "站立位", "站立位" },
    { "パンスト", "连裤袜" },

    { "セクシー", "性感" },
    { "彼女", "女友" },
    { "淫乱", "淫乱" },
    { "車", "车震" },
    { "車内", "车震" },

    { "シックスナイン", "69" },
    { "スタイル抜群モデル級", "模特级身材" },
    { "スポーツコスプレ", "角色扮演" },
    { "フェラ抜き", "口内射精" },
    { "フェロモン", "费洛蒙" },
    { "清掃員", "清洁工" },

    { "二穴同插", "双穴齐插" },
    { "護士", "护士" },
    { "掃除", "清洁" },
    { "巨大阳具", "巨大阳具" },
    { "黒人", "黑人" },
    { "講座", "课程" },
    { "muramura", "Muramura" },
    { "美人", "美人" },
    { "美肌", "美肌" },
    { "アイドル", "偶像" },
    { "ザーメン", "精液" },
    { "再会", "重逢" },
    { "即ハメ", "即插" },
    { "旅行", "旅行" },
    { "椅子", "椅子" },
    { "絶叫", "呻吟" },
    { "不倫・浮気", "出轨" },
    { "玄関", "玄关" },
    { "宅配", "宅配" },
    { "ご近所", "邻居" },
    { "裸エプロン", "裸体围裙" },
    { "テレフォンセックス", "电话性爱" },
    { "誘惑", "诱惑" },
    { "欲求不満", "欲求不满" },
    { "面接", "面试" },
    { "レズ", "蕾丝" },
    { "マンコぶっかけ", "阴部颜射" },
    { "和室", "和室" },
    { "素股", "素股" },
    { "女医", "女医" },
    { "極上泡姫物語", "顶级泡姬物语" },
    { "拘束", "拘束" },
    { "お隣さん", "邻居" },
    { "セックスレス", "无性生活" },
    { "元モデル", "模特" },

    { "ヒミツのアルバイト", "秘密打工" },
    { "FALENO", "FALENO" },
    { "女二人でダブルフェラ", "双女口交" },
    { "熟女画報社", "熟女" },
    { "お風呂", "浴室" },
    { "リゾート", "度假村" },
    { "性奴", "性奴" },
    { "剃毛", "剃毛" },
    { "引っ越し", "搬家" },

    { "Debut", "首秀" },
    { "恍惚", "狂热" },
    { "THE 未公開", "未公开" },
    { "家事", "家务" },
    { "電球", "电灯泡" },
    { "義兄", "义兄" },
    { "ノーパン", "无内裤" },
    { "ノーブラ", "无胸罩" },
    { "女熱大陸", "女热大陆" },
    { "過激痴漢", "激进痴汉" },
    { "ナチュラルハイ", "Natural High" },
    { "ご主人様", "主人" },
    { "ご奉仕", "服侍" },
    { "バニーガール", "兔女郎" },
    { "センタービレッジ", "Center Village" },
    { "風俗", "风俗" },
    { "クリスマスイブ", "圣诞夜" },
    { "サンタ", "圣诞老人" },
    { "ソープ", "泡泡浴" },
    { "デリヘル嬢", "上门女郎" },
    { "人気女優", "人气女优" },
    { "レンタルガール", "租赁女友" },
    { "お嫁さん", "新娘" },
    { "婚紗禮服", "婚纱礼服" },
    { "家事代行", "家务代劳" },
    { "妻の女友達", "妻子闺蜜" },

    { "ときめき", "心动" },
    { "奥手", "腼腆" },
    { "Tバック", "丁字裤" },
    { "アフター6", "下班后" },
    { "痴女與M男", "痴女" },
    { "乳首マッサージ", "乳头按摩" },
    { "調教", "调教" },

    { "RUBY", "RUBY" },

    { "元アナウンサー", "主播" },
    { "喘ぎ声", "呻吟" },
    { "白昼", "白天" },
    { "年間", "年度" },
    { "企画物", "企划片" },
    { "南國度假地", "度假村" },
    { "戶外", "户外" },
            { "1v1性交","一对一性爱"},
            { "1対1セックス","一对一性爱"},
            { "ラフォーレガール","Laforet Girl"},
            { "THE未公開","未公开"},
    { "ポークテリヤキ", "Pork Teriyaki" },
    { "スタジオテリヤキ", "Studio Teriyaki" },
    { "SOD女子社員", "SOD 女员工" },
    { "SODクリエイト", "SOD Create" },
    { "トイレ", "洗手间" },
    { "ドッキリ", "恶作剧" },
    { "ファン", "粉丝感谢祭" },
    { "电动按摩器", "电动按摩棒" },
    { "精緻身材", "魔鬼身材" },
    { "メルシーボークー", "感谢祭" },
    { "ベッド", "卧室" },
    { "ソープランドプレイ", "泡泡浴" },
    { "ぶっかけ・輪姦", "群交" },
    { "中出", "内射" },
    { "３P", "三人行" },
    { "ソファ", "沙发" },
    { "Wフェラ", "双女口交" },
{ "女上位", "骑乘位" },
{ "騎乗位", "骑乘位" },
    { "打底裤", "紧身裤" }
            ,{ "浴衣和服","和服"},
    { "指法", "手指插入" },
    { "ロリータ", "萝莉" },
    { "亂倫", "乱伦" },
    { "束縛", "束缚" },
    { "蘿莉", "萝莉" },
    { "昼間", "白天" },
    { "M男", "受虐男" },
    { "ロリ系", "萝莉" }
        };
        private readonly static List<string> removeTermsList = new List<string>
            {
            "PassionXXX",
            "KARMA大全集",
            "人気シリーズ",
            "はだかの履歴書",
            "精品收藏",
            "車站性交",
                "マングリ返し",
                "総集編",
                "撮影",
                "跳",
                "独占配信",
                "そっくりさん",
                "レッド",
                "レッド突撃隊シリーズ",
                "Video",
                "ベスト・総集編",
                "VRBangers",
                "人氣標題",
                "ブルーレイ・ディスク",
                "vrbangers",
                "AV女優",
                "D罩杯+",
                "60fps",
                "総集編・オムニバス",
                "えゆの衣裳部屋",
                "淫亂S女",
                "巨大電マ責め",
                "オーロラプロジェクト○○ダイジェスト",
                "オーロラプロジェクト・アネックス",
                "1080p",
                "ネクストグループ",
                "超有名S級女優",
                "超有名女優",
                "推荐作品",
                "超人気女優",
                "超人気作品",
                "超人気シリーズ",
                "最佳合集",
                "パコパコママ",
                "カリビアンコム",
                "キャットウォーク",
                "ROOKIE",
    "シャワー・入浴映像",
    "三連発極上女優",
    "大人の日曜劇場",
    "白スーツ",
    "30代",
    "DVD已售罄",
    "40代",
    "グローリークエスト",
    "DAHLIA",
    "元有名コスプレイヤー",
    "放課後美少女ファイル",
    "ルビー年鑑",
    "カメラ目線・主観映像",
    "奧斯曼株式會社",
    "フルHD祭り",
    "OLの尻に埋もれたい",
    "年上",
    "上司の命令",
    "艶熟女温泉慕情",
    "レズビアン大乱交",
    "ゴーゴーズ",
    "GoGo’sCore",
    "クスコ",
    "色気"
            };
        #endregion
        /// <summary>
        /// 修正元数据。
        /// </summary>
        /// <remarks>
        /// 标准格式
        /// title [中字] 人生初 絶頂、その向こう側へ
        /// originalTitle SSIS-531 人生初 絶頂、その向こう側へ
        /// sortTitle SSIS-531-C SSIS-531-UC
        /// </remarks>
        public static async Task FixNfoTagsAsync(ILoggerFactory loggerFactory, string path)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            // 获取分组后的 .nfo 文件
            // var groupedNfoFiles = GetNfoFilesGroupedByDirectoryName(path);
            var groupedNfoFiles = NfoFileUtils.GetNfoFilesGroupedByDirectoryName(path);
            //// 输出分组结果
            //Console.WriteLine("按目录分组的 .nfo 文件:");
            foreach (var group in groupedNfoFiles)
            {
                Console.WriteLine($"\n目录: {group.Key}");
                // 获取该目录下的所有文件
                var nfoFiles = group.Value;
                // 优先处理 movie.nfo 文件
                var nfoFile = nfoFiles.Find(file => Path.GetFileName(file).Equals("movie.nfo", StringComparison.OrdinalIgnoreCase));

                // 判断是否为空或者文件不存在
                if (String.IsNullOrEmpty(nfoFile) || !File.Exists(nfoFile))
                {
                    // 如果没有找到 movie.nfo 文件，则查找与目录名相同的文件
                    nfoFile = nfoFiles.Find(file => Path.GetFileName(file).Equals(group.Key + ".nfo", StringComparison.OrdinalIgnoreCase));
                    // 如果同名文件不存在则使用文件列表第一个
                    if (!File.Exists(nfoFile))
                    {
                        nfoFile = nfoFiles.FirstOrDefault();
                    }
                }

                FileInfo nfoFileInfo = new FileInfo(nfoFile);
                FileAttributes attributes = File.GetAttributes(nfoFile);
                // 从文件夹名称提取元数据
                var folderName = nfoFileInfo.Directory.Name;

                // 检查文件属性
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    logger.LogInformation($"当前 nfo 文件已隐藏，跳过！");
                    Console.WriteLine($"当前 nfo 文件已隐藏，跳过！");
                    continue;
                }

                // 备份 nfo 文件
                var destFileName = string.Format(@"{0}\{1}{2}", nfoFileInfo.DirectoryName, Path.GetFileNameWithoutExtension(nfoFile) + ".bak", nfoFileInfo.Extension);
                if (!File.Exists(destFileName))
                {
                    nfoFileInfo.CopyTo(destFileName);
                }

                Console.WriteLine($"当前处理的文件：{nfoFile}");

                // 读取 nfo 文件内容
                NfoFileManager nfoManager = new NfoFileManager(nfoFile);
                if (String.IsNullOrEmpty(nfoManager.ToString()))
                {
                    Console.WriteLine($"获取 「{nfoFile}」 番号异常，跳过执行。");
                    continue;
                }

                #region 处理 nfo 文件内容...

                // 从 nfo 文件获取元数据
                JavVideo nfoVideoInfo = nfoManager.GetJavVideo();

                #endregion

                // 解析 javId
                var javId = JavRecognizer.Parse(nfoVideoInfo.SortTitle) ?? JavRecognizer.Parse(nfoVideoInfo.OriginalTitle) ?? JavRecognizer.Parse(nfoVideoInfo.Title);

                var videoTags = nfoVideoInfo.Tags;
                var videoGenres = nfoVideoInfo.Genres;

                // 整理元数据逻辑
                bool hasChineseSubtitle = false;
                bool hasUncensored = false;


                // 支持动态匹配主流分辨率与帧率
                var regex = new Regex(@"\b(?:[1-9]\d{2,}p|4k|24|30|50|60|120|240|29\.97|\d+\.?\d*fps)\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                // 处理空值并合并所有数据源
                var tagList = (videoGenres ?? Enumerable.Empty<string>())
                    .Concat(videoTags ?? Enumerable.Empty<string>())
                    .Concat(nfoVideoInfo?.Genres ?? Enumerable.Empty<string>())
                    .Concat(nfoVideoInfo?.Tags ?? Enumerable.Empty<string>())
                    .Select(item => item == "中字" ? "中文字幕" : item) // 替换文本
                    .Select(item => item == "無碼破解" ? "无码破解" : item)
                    .Select(item => item == "無碼流出" ? "无码流出" : item)
                    .Distinct() // 去重
                    .ToList();
                tagList.RemoveAll(tag => regex.IsMatch(tag));
                tagList.Remove("中字無碼破解");
                tagList.RemoveAll(t => t.Contains("/") || t.Contains("(") || t.Length > 10 || t.Contains("店長") || t.Contains("上映中") || t.Contains("動画"));

                // 添加外挂字幕标签
                // 检查多种字幕文件格式
                string[] subtitleExtensions = { "*.srt", "*.ssa", "*.ass", "*.vtt", "*.sub" };
                bool hasSubtitles = subtitleExtensions.Any(ext =>
                    Directory.GetFiles(nfoFileInfo.DirectoryName, ext, SearchOption.AllDirectories).Length > 0);

                if (hasSubtitles && !tagList.Contains("外挂字幕"))
                {
                    tagList.Add("外挂字幕");
                }

                // 检查是否包含"Un Censored"并添加"无码流出"标签
                if (nfoFileInfo.DirectoryName.Contains("Un Censored", StringComparison.OrdinalIgnoreCase))
                {
                    tagList.Add("无码流出");

                    tagList.Remove("无码破解");
                    tagList = tagList.Distinct() // 去重
                    .ToList();
                }

                // 替换特定标签
                var unknownTags = new HashSet<string>();
                for (int i = 0; i < tagList.Count; i++)
                {
                    var currentTag = tagList[i];
                    // 替换特定标签
                    if (avTerms.ContainsKey(currentTag))
                    {
                        tagList[i] = avTerms[currentTag];
                    }

                    if (!avTerms.Values.Contains(currentTag)) // 检查是否在avTerms的值中
                    {
                        // 记录未出现在avTerms的键和值中的标签
                        unknownTags.Add(currentTag);
                    }

                    // 移除不需要的标签
                    if (removeTermsList.Contains(tagList[i]))
                    {
                        tagList.RemoveAt(i);
                        i--; // 调整索引
                    }
                }

                // 将未知标签写入tag.txt文件
                if (unknownTags.Count > 0)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var tagFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"tag_{timestamp}.txt");
                    var tagContent = string.Join(",", unknownTags);
                    File.AppendAllText(tagFilePath, tagContent + Environment.NewLine);
                }

                // 根据需求将结果分配给两个变量
                videoTags = new List<string>(tagList.Distinct());
                videoGenres = new List<string>(tagList.Distinct());

                nfoVideoInfo.Title = nfoVideoInfo.Title.Replace("無碼 ", "").Replace("無修正 カリビアンコム ", "").Trim();
                nfoVideoInfo.OriginalTitle = nfoVideoInfo.OriginalTitle.Replace("無碼 ", "").Replace("無修正 カリビアンコム ", "").Trim();

                // 检查标题是否包含 "中字" 标签
                if (nfoFileInfo.DirectoryName.Contains("Un Censored"))
                {
                    if (hasSubtitles)
                    {
                        if (!nfoVideoInfo.Title.Contains("中字") && !nfoVideoInfo.Title.Contains("无码"))
                        {
                            nfoVideoInfo.Title = $"[中字无码] {nfoVideoInfo.Title}";
                        }
                        else if (!nfoVideoInfo.Title.Contains("中字") && nfoVideoInfo.Title.Contains("[无码]"))
                        {
                            nfoVideoInfo.Title = nfoVideoInfo.Title.Replace("[无码]", "[中字无码]");
                        }
                        else if (nfoVideoInfo.Title.Contains("无码"))
                        {
                            nfoVideoInfo.Title = nfoVideoInfo.Title.Replace("[无码]", "[中字无码]");
                        }
                    }
                    else if (!nfoVideoInfo.Title.Contains("无码"))
                    {
                        if (nfoVideoInfo.Title.Contains("中字"))
                        {
                            nfoVideoInfo.Title = nfoVideoInfo.Title.Replace("[中字]", "[中字无码]");
                            
                        }
                        else if (!nfoVideoInfo.Title.Contains("无码"))
                        {
                            nfoVideoInfo.Title = $"[无码] {nfoVideoInfo.Title}";
                        }
                    }
                }

                // 保存元数据
                nfoManager.SaveMetadata(nfoVideoInfo.Title, nfoVideoInfo.OriginalTitle, nfoVideoInfo.SortTitle, nfoVideoInfo.Plot, javId.Id, nfoVideoInfo.Actors, videoGenres, videoTags, nfoVideoInfo.GetYear(), nfoVideoInfo.Date);

                #region 遍历其他 nfo 文件...
                nfoFiles.Remove(nfoFile);
                if (nfoFiles.Count > 0)
                {
                    foreach (var item in nfoFiles)
                    {
                        File.Copy(nfoFile, item, true);
                    }
                }

                // 调整目录名称
                if (nfoFileInfo.DirectoryName.Contains("Un Censored"))
                {
                    // 按优先级处理标签替换
                    if (hasSubtitles)
                    {
                        if (folderName.Contains("[中字無碼破解]"))
                        {
                            folderName = folderName.Replace("[中字無碼破解]", "[中字无码流出]");
                        }
                        else if (folderName.Contains("[無碼破解]"))
                        {
                            folderName = folderName.Replace("[無碼破解]", "[中字无码流出]");
                        }
                        else if (folderName.Contains("[中字]"))
                        {
                            folderName = folderName.Replace("[中字]", "[中字无码流出]");
                        }
                        else if (!folderName.Contains("无码流出") && !folderName.Contains("中字无码流出"))
                        {
                            folderName = $"[中字无码流出] - {folderName}";
                        }
                        else if (folderName.Contains("无码流出"))
                        {
                            folderName = folderName.Replace("[无码流出]", "[中字无码流出]");
                        }
                    }
                    else
                    {
                        if (folderName.Contains("[中字無碼破解]"))
                        {
                            folderName = folderName.Replace("[中字無碼破解]", "[无码流出]");
                        }
                        else if (folderName.Contains("[無碼破解]"))
                        {
                            folderName = folderName.Replace("[無碼破解]", "[无码流出]");
                        }
                        else if (folderName.Contains("[中字]"))
                        {
                            folderName = folderName.Replace("[中字]", "[中字无码流出]");
                        }
                        else if (!folderName.Contains("无码流出"))
                        {
                            folderName = $"[无码流出] - {folderName}";
                        }
                    }

                    // 获取当前文件夹的完整路径
                    var currentDir = Path.GetDirectoryName(nfoFile);
                    var parentDir = Path.GetDirectoryName(currentDir);
                    var newFolderPath = Path.Combine(parentDir, folderName);

                    // 当前目录所有文件
                    var files = Directory.GetFiles(currentDir, "*.*", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var newFileName = fileName;
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var fileExt = Path.GetExtension(fileName);

                        // 按优先级处理文件名标签替换
                        if (hasSubtitles)
                        {
                            if (fileName.Contains("[中字無碼破解]"))
                            {
                                newFileName = fileName.Replace("[中字無碼破解]", "[中字无码流出]");
                            }
                            else if (fileName.Contains("[無碼破解]"))
                            {
                                newFileName = fileName.Replace("[無碼破解]", "[中字无码流出]");
                            }
                            else if (fileName.Contains("[中字]"))
                            {
                                newFileName = fileName.Replace("[中字]", "[中字无码流出]");
                            }
                            else if (!fileName.Contains("[无码流出]") && !fileName.Contains("[中字无码流出]") && fileName.Contains(" - "))
                            {
                                newFileName = $"[中字无码流出] - {fileNameWithoutExt}{fileExt}";
                            }
                            else if (fileName.Contains("无码流出"))
                            {
                                newFileName = newFileName.Replace("[无码流出]", "[中字无码流出]");
                            }
                        }
                        else
                        {
                            if (fileName.Contains("[中字無碼破解]"))
                            {
                                newFileName = fileName.Replace("[中字無碼破解]", "[无码流出]");
                            }
                            else if (fileName.Contains("[無碼破解]"))
                            {
                                newFileName = fileName.Replace("[無碼破解]", "[无码流出]");
                            }
                            else if (fileName.Contains("[中字]"))
                            {
                                newFileName = fileName.Replace("[中字]", "[中字无码流出]");
                            }
                            else if (!fileName.Contains("无码流出") && fileName.Contains(" - "))
                            {
                                newFileName = $"[无码流出] - {fileNameWithoutExt}{fileExt}";
                            }
                        }

                        if (fileName != newFileName)
                        {
                            var newFilePath = Path.Combine(Path.GetDirectoryName(file), newFileName);
                            if (File.Exists(newFilePath))
                                File.Delete(newFilePath);
                            File.Move(file, newFilePath);
                        }
                    }

                    // 重命名文件夹
                    if (currentDir != newFolderPath)
                    {
                        Directory.Move(currentDir, newFolderPath);
                    }
                }

                // 文件名和目录名中包含 "無碼 "、"無修正 カリビアンコム " 要去掉
                if (nfoFileInfo.DirectoryName.Contains("UnCensored") && (folderName.Contains("無碼") || folderName.Contains("無修正 カリビアンコム")))
                {
                    // 移除 "無碼" 和 "無修正 カリビアンコム" 等关键词
                    folderName = folderName.Replace("無碼 ", "").Replace("無修正 カリビアンコム ", "").Trim();

                    // 获取当前文件夹的完整路径
                    var currentDir = Path.GetDirectoryName(nfoFile);
                    var parentDir = Path.GetDirectoryName(currentDir);
                    var newFolderPath = Path.Combine(parentDir, folderName);
                    // 当前目录中 .nfo、媒体文件、字幕文件等移除 "無碼" 和 "無修正 カリビアンコム" 等关键词
                    var files = Directory.GetFiles(currentDir, "*.*", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var newFileName = fileName.Replace("無碼 ", "").Replace("無修正 カリビアンコム ", "").Trim();
                        if (fileName != newFileName)
                        {
                            var newFilePath = Path.Combine(Path.GetDirectoryName(file), newFileName);
                            if (File.Exists(newFilePath))
                                File.Delete(newFilePath);
                            File.Move(file, newFilePath);
                        }
                    }

                    // 重命名文件夹
                    if (currentDir != newFolderPath)
                    {
                        Directory.Move(currentDir, newFolderPath);
                    }
                }

                #endregion

                Console.WriteLine($"-----------------修正后的元数据-----------------");
                Console.WriteLine($"fileName -> {nfoFileInfo.Name}");
                Console.WriteLine($"videoId -> {javId.Id}");
                Console.WriteLine($"videoTitle -> {nfoVideoInfo.Title}");
                Console.WriteLine($"videoOriginalTitle -> {nfoVideoInfo.OriginalTitle}");
                Console.WriteLine($"videoSortTitle -> {nfoVideoInfo.SortTitle}");
                Console.WriteLine($"tags -> {String.Join(",", videoTags)}");
                Console.WriteLine($"genres -> {String.Join(",", videoGenres)}");
                Console.WriteLine($"==============================================");
            }

        }


        /// <summary>
        /// 修正元数据。
        /// </summary>
        /// <remarks>
        /// 标准格式
        /// title [中字] 人生初 絶頂、その向こう側へ
        /// originalTitle SSIS-531 人生初 絶頂、その向こう側へ
        /// sortTitle SSIS-531-C SSIS-531-UC
        /// </remarks>
        public static async Task FixNfoDataAsync(ILoggerFactory loggerFactory, string path)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            // 获取分组后的 .nfo 文件
            // var groupedNfoFiles = GetNfoFilesGroupedByDirectoryName(path);
            var groupedNfoFiles = NfoFileUtils.GetNfoFilesGroupedByDirectoryName(path);
            //// 输出分组结果
            //Console.WriteLine("按目录分组的 .nfo 文件:");
            foreach (var group in groupedNfoFiles)
            {
                Console.WriteLine($"\n目录: {group.Key}");
                // 获取该目录下的所有文件
                var nfoFiles = group.Value;
                // 优先处理 movie.nfo 文件
                var nfoFile = nfoFiles.Find(file => Path.GetFileName(file).Equals("movie.nfo", StringComparison.OrdinalIgnoreCase));

                // 判断是否为空或者文件不存在
                if (String.IsNullOrEmpty(nfoFile) || !File.Exists(nfoFile))
                {
                    // 如果没有找到 movie.nfo 文件，则查找与目录名相同的文件
                    nfoFile = nfoFiles.Find(file => Path.GetFileName(file).Equals(group.Key + ".nfo", StringComparison.OrdinalIgnoreCase));
                    // 如果同名文件不存在则使用文件列表第一个
                    if (!File.Exists(nfoFile))
                    {
                        nfoFile = nfoFiles.FirstOrDefault();
                    }
                }

                FileInfo nfoFileInfo = new FileInfo(nfoFile);
                FileAttributes attributes = File.GetAttributes(nfoFile);
                // 从文件夹名称提取元数据
                var folderName = nfoFileInfo.Directory.Name;

                // 检查文件属性
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    logger.LogInformation($"当前 nfo 文件已隐藏，跳过！");
                    Console.WriteLine($"当前 nfo 文件已隐藏，跳过！");
                    continue;
                }

                // 备份 nfo 文件
                var destFileName = string.Format(@"{0}\{1}{2}", nfoFileInfo.DirectoryName, Path.GetFileNameWithoutExtension(nfoFile) + ".bak", nfoFileInfo.Extension);
                if (!File.Exists(destFileName))
                {
                    nfoFileInfo.CopyTo(destFileName);
                }

                Console.WriteLine($"当前处理的文件：{nfoFile}");

                // 读取 nfo 文件内容
                NfoFileManager nfoManager = new NfoFileManager(nfoFile);
                if (String.IsNullOrEmpty(nfoManager.ToString()))
                {
                    Console.WriteLine($"获取 「{nfoFile}」 番号异常，跳过执行。");
                    continue;
                }

                #region 处理 nfo 文件内容...

                // 从 nfo 文件获取元数据
                JavVideo nfoVideoInfo = nfoManager.GetJavVideo();

                #endregion

                // 解析 javId
                var javId = JavRecognizer.Parse(nfoVideoInfo.SortTitle) ?? JavRecognizer.Parse(nfoVideoInfo.OriginalTitle) ?? JavRecognizer.Parse(nfoVideoInfo.Title);
                if (javId == null)
                {
                    javId = new JavId();
                    // 处理视频目录名称
                    if (VideoParser.IsValidNameFormat(group.Key))
                    {
                        // 从视频目录名称中提取视频基础信息
                        var videoInfo = VideoParser.ParseMetadataFormName(group.Key);
                        if (videoInfo != null)
                        {
                            javId.Id = videoInfo.Number;
                            //videoTitle = videoInfo.Title;
                            //hasUncensored = videoInfo.IsUncensored;
                            //hasChineseSubtitle = videoInfo.HasChineseSubtitle;
                            //videoSortTitle = videoInfo.SortTitle;
                        }
                    }
                    if (String.IsNullOrEmpty(javId.Id))
                    {
                        logger.LogInformation($"修正元数据：---> {path} 获取 「{nfoFile}」 番号异常，跳过执行");
                        Console.WriteLine($"获取 「{nfoFile}」 番号异常，跳过执行。");
                        continue;
                    }
                }

                // 从互联网获取视频信息
                VideoInfoFetcher videoInfoFetcher = new VideoInfoFetcher(loggerFactory);
                var javVideo = new JavVideo();
                if (javId.Type == JavIdType.Censored)
                {
                    javVideo = await videoInfoFetcher.FetchCensoredVideo(javId.Id);
                }
                else if (javId.Type == JavIdType.Uncensored)
                {
                    javVideo = await videoInfoFetcher.FetchUncensoredVideo(javId);
                }
                else if (javId.Type == JavIdType.None)
                {
                    Console.WriteLine($"获取 「{nfoFile}」 番号异常，跳过执行。");
                    continue;
                }
                else
                {
                    Console.WriteLine($"获取 「{nfoFile}」 番号异常，跳过执行。");
                    continue;
                }

                if (javVideo == null)
                {
                    logger.LogInformation($"修正元数据：---> {path} 获取 「{nfoFile}」 信息异常，跳过执行");
                    Console.WriteLine($"获取 「{nfoFile}」 信息异常，跳过执行。");
                    continue;
                }

                // 定义标准视频元数据
                var videoId = javId.Id.ToUpper();
                var videoDate = javVideo.Date;
                var videoYear = javVideo.GetYear();
                var videoTitle = javVideo.Title.Trim();
                var videoOriginalTitle = javVideo.OriginalTitle.Trim();
                var videoSortTitle = videoId;
                var videoPlot = javVideo.Plot;
                var videoActors = javVideo.Actors;
                var videoTags = javVideo.Tags;
                var videoGenres = javVideo.Genres;

                // 整理元数据逻辑
                bool hasChineseSubtitle = false;
                bool hasUncensored = false;

                // 处理视频目录名称
                if (VideoParser.IsValidNameFormat(nfoVideoInfo.Title))
                {
                    // 从视频目录名称中提取视频基础信息
                    var videoInfo = VideoParser.ParseMetadataFormName(nfoVideoInfo.Title);
                    if (videoInfo != null)
                    {
                        videoTitle = videoInfo.Title;
                        hasUncensored = videoInfo.IsUncensored;
                        hasChineseSubtitle = videoInfo.HasChineseSubtitle;
                        videoSortTitle = videoInfo.SortTitle;
                    }
                }

                // 处理 nfo 中标题，防止修改后的信息被替换
                if (VideoParser.IsValidTitleFormat(nfoVideoInfo.Title))
                {
                    // 从视频标题中提取视频信息
                    var videoInfo = VideoParser.ParseMetadataFormTitle(nfoVideoInfo.Title);
                    if (videoInfo != null)
                    {
                        videoTitle = videoInfo.Title;
                        hasUncensored = videoInfo.IsUncensored;
                        hasChineseSubtitle = videoInfo.HasChineseSubtitle;
                        videoSortTitle = videoInfo.SortTitle;
                    }
                }

                // 比较 从互联网中获取的标题 和 nfo 文件的视频标题
                int matchingChars = videoTitle.Intersect(javVideo.Title).Count();
                double similarityRatio = (double)matchingChars / Math.Max(videoTitle.Length, javVideo.Title.Length);

                // 如果相似度过低，则以 nfo 文件的视频标题 为准
                if (similarityRatio < 0.5 && !(javVideo.Title.Contains(videoTitle) || videoTitle.Contains(javVideo.Title))) // 50% 的相似度阈值
                {
                    videoTitle = nfoVideoInfo.Title;
                }
                else
                {
                    videoTitle = javVideo.Title;
                }

                // 确保原始标题格式为 "IPZZ-485 标题"
                videoOriginalTitle = $"{videoId} {javVideo.OriginalTitle}";
                // 支持动态匹配主流分辨率与帧率
                var regex = new Regex(@"\b(?:[1-9]\d{2,}p|4k|24|30|50|60|120|240|29\.97|\d+\.?\d*fps)\b",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);
                // 处理空值并合并所有数据源
                var tagList = (videoGenres ?? Enumerable.Empty<string>())
                    .Concat(videoTags ?? Enumerable.Empty<string>())
                    .Concat(nfoVideoInfo?.Genres ?? Enumerable.Empty<string>())
                    .Concat(nfoVideoInfo?.Tags ?? Enumerable.Empty<string>())
                    .Select(item => item == "中字" ? "中文字幕" : item) // 替换文本
                    .Select(item => item == "無碼破解" ? "无码破解" : item)
                    .Select(item => item == "無碼流出" ? "无码流出" : item)
                    .Distinct() // 去重
                    .ToList();
                tagList.RemoveAll(tag => regex.IsMatch(tag));
                tagList.Remove("中字無碼破解");
                tagList.RemoveAll(t => t.Contains("/") || t.Contains("(") || t.Length > 10 || t.Contains("店長") || t.Contains("上映中") || t.Contains("動画"));

                // 添加外挂字幕标签
                // 检查多种字幕文件格式
                string[] subtitleExtensions = { "*.srt", "*.ssa", "*.ass", "*.vtt", "*.sub" };
                bool hasSubtitles = subtitleExtensions.Any(ext =>
                    Directory.GetFiles(nfoFileInfo.DirectoryName, ext, SearchOption.AllDirectories).Length > 0);

                if (hasSubtitles && !tagList.Contains("外挂字幕"))
                {
                    tagList.Add("外挂字幕");
                }

                // 根据不同情况设置 videoTitle、videoOriginalTitle、videoSortTitle、tags 和 genres，只针对 标题解析生效
                if (hasChineseSubtitle && hasUncensored)
                {
                    videoTitle = $"[中字无码] {videoTitle}";
                    videoSortTitle = $"{videoId}-UC";
                    tagList = new List<string> { "中文字幕", "无码破解" }.Concat(tagList).Distinct().ToList();
                }
                else if (hasChineseSubtitle || tagList.Contains("中字"))
                {
                    videoTitle = $"[中字] {videoTitle}";
                    videoSortTitle = $"{videoId}-C";
                    tagList = new List<string> { "中文字幕" }.Concat(tagList).Distinct().ToList();
                }
                else if (hasUncensored)
                {
                    videoTitle = $"[无码] {videoTitle}";
                    videoSortTitle = $"{videoId}-U";
                    tagList = new List<string> { "无码破解" }.Concat(tagList).Distinct().ToList();
                }
                else
                {
                    videoTitle = javVideo.Title;
                    videoSortTitle = videoId;
                }

                // 根据需求将结果分配给两个变量
                videoTags = new List<string>(tagList);
                videoGenres = new List<string>(tagList);

                // 处理标签
                if ((tagList.Contains("無碼流出") || tagList.Contains("無碼破解") || tagList.Contains("中字") || tagList.Contains("中文字幕") || tagList.Contains("无码破解") || tagList.Contains("无码流出")) && (!hasChineseSubtitle && !hasUncensored))
                {
                    if ((tagList.Contains("中字") || tagList.Contains("中文字幕")) && (tagList.Contains("無碼破解") || tagList.Contains("無碼流出") || tagList.Contains("无码破解") || tagList.Contains("无码流出")))
                    {
                        videoTitle = $"[中字无码] {videoTitle}";
                        videoSortTitle = $"{videoId}-UC";
                    }
                    else if ((tagList.Contains("中字") || tagList.Contains("中文字幕")) && !tagList.Contains("無碼破解") && !tagList.Contains("無碼流出") && !tagList.Contains("无码破解") && !tagList.Contains("无码流出"))
                    {
                        videoTitle = $"[中字] {videoTitle}";
                        videoSortTitle = $"{videoId}-C";
                    }
                    else if (tagList.Contains("無碼破解") || tagList.Contains("無碼流出") || tagList.Contains("无码破解") || tagList.Contains("无码流出"))
                    {
                        videoTitle = $"[无码] {videoTitle}";
                        videoSortTitle = $"{videoId}-U";
                    }
                }

                // 处理演员
                if (videoActors != null && videoActors.Count > 0)
                {
                    for (int i = 0; i < videoActors.Count; i++)
                    {
                        Match match = Regex.Match(videoActors[i], @"（([^）]*)）");
                        if (match.Success)
                        {
                            videoActors[i] = match.Groups[1].Value;
                            Console.WriteLine(videoActors[i]); // 输出：花沢ひまり
                        }
                    }
                }

                #region 下载封面和缩略图...
                try
                {
                    // 下载封面
                    if (!String.IsNullOrEmpty(javVideo.Cover))
                    {
                        Dictionary<string, string> coverDict = new Dictionary<string, string>();
                        // 有码的直接在 DMM 下载原图
                        if (javId.Type == JavIdType.Censored)
                        {
                            var picUrls = DMMImageUtils.GetDMMBigImages(javVideo.Cover);
                            if (picUrls != null && picUrls.Count > 0)
                            {
                                foreach (var picUrl in picUrls)
                                {
                                    if (await HttpUtils.IsUrlAvailableAsync(picUrl.Value))
                                    {
                                        var cover = await Downloader.DownloadJpegAsync(picUrl.Value, nfoFileInfo.DirectoryName);
                                        if (cover != null)
                                        {
                                            coverDict.Add(picUrl.Key, cover);
                                        }
                                    }
                                }
                            }
                            // 无码的下载获取到的封面
                            else if (javId.Type == JavIdType.Uncensored)
                            {
                                var cover = await Downloader.DownloadJpegAsync(javVideo.Cover, nfoFileInfo.DirectoryName, javId.Id.ToUpper());
                                if (cover != null)
                                {
                                    coverDict.Add("fanart", cover);
                                }
                            }

                            var fileName = $"fanart"; // 命名规则
                            var savePath = Path.Combine(nfoFileInfo.DirectoryName, fileName);
                            var saveName = $"{savePath}.jpg";
                            if (coverDict != null && coverDict.Count > 0)
                            {
                                File.Copy(coverDict["fanart"], saveName, true);
                            }
                            else
                            {
                                await Downloader.DownloadJpegAsync(javVideo.Cover, nfoFileInfo.DirectoryName, savePath);
                            }
                            // 备份现有图片文件
                            try
                            {
                                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                                var backupPath = Path.Combine(nfoFileInfo.DirectoryName, $"images_backup_{timestamp}.zip");

                                // 获取所有图片文件，排除已存在的备份文件
                                var imageFiles = Directory.GetFiles(nfoFileInfo.DirectoryName, "*.jpg")
                                    .Concat(Directory.GetFiles(nfoFileInfo.DirectoryName, "*.png"))
                                    .Concat(Directory.GetFiles(nfoFileInfo.DirectoryName, "*.webp"))
                                    .Where(f => !f.Contains("images_backup_"))
                                    .ToList();

                                if (imageFiles.Any())
                                {
                                    // 创建临时目录
                                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                                    Directory.CreateDirectory(tempDir);
                                    try
                                    {
                                        // 复制文件到临时目录
                                        foreach (var file in imageFiles)
                                        {
                                            File.Copy(file, Path.Combine(tempDir, Path.GetFileName(file)));
                                        }
                                        // 从临时目录创建 zip
                                        ZipFile.CreateFromDirectory(tempDir, backupPath);
                                        logger.LogInformation($"已创建图片备份：{backupPath}");

                                        // 删除旧的备份文件
                                        var oldBackups = Directory.GetFiles(nfoFileInfo.DirectoryName, "images_backup_*.zip")
                                            .Where(f => f != backupPath)
                                            .OrderByDescending(f => File.GetCreationTime(f))
                                            .Skip(2) // 保留最新的2个备份
                                            .ToList();

                                        foreach (var oldBackup in oldBackups)
                                        {
                                            try
                                            {
                                                File.Delete(oldBackup);
                                                logger.LogInformation($"已删除旧备份：{oldBackup}");
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.LogError($"删除旧备份失败：{ex.Message}");
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        // 清理临时目录
                                        if (Directory.Exists(tempDir))
                                        {
                                            Directory.Delete(tempDir, true);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"创建图片备份失败：{ex.Message}");
                            }

                            if (File.Exists(saveName))
                            {
                                var thumbPicture = Path.Combine(nfoFileInfo.DirectoryName, "thumb.jpg");
                                File.Copy(saveName, thumbPicture, true);
                                var targetRatio = 2f / 3f;
                                var folderPicture = Path.Combine(nfoFileInfo.DirectoryName, "folder.jpg");
                                var posterPicture = Path.Combine(nfoFileInfo.DirectoryName, "poster.jpg");
                                if (javId.Type == JavIdType.Censored)
                                {
                                    if (coverDict != null && coverDict.Count > 0)
                                    {
                                        // 检查 poster 图片分辨率
                                        var posterPath = coverDict["poster"];
                                        using (var image = System.Drawing.Image.FromFile(posterPath))
                                        {
                                            if (image.Height < 400 || image.Width < 300)
                                            {
                                                // 如果分辨率太低，使用 fanart 裁切
                                                ImageUtils.CropImage(saveName, folderPicture, targetRatio, CropMode.Right);
                                                File.Copy(folderPicture, posterPicture, true);
                                            }
                                            else
                                            {
                                                // 分辨率符合要求，直接使用 poster
                                                File.Copy(coverDict["poster"], folderPicture, true);
                                                File.Copy(coverDict["poster"], posterPicture, true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ImageUtils.CropImage(saveName, folderPicture, targetRatio, CropMode.Right);
                                        File.Copy(folderPicture, posterPicture, true);
                                    }
                                }
                                else
                                {
                                    File.Copy(saveName, folderPicture, true);
                                    File.Copy(saveName, posterPicture, true);
                                }

                                // 替换 JavHelper 获取的带水印的封面和缩略图 -fanart.jpg、-poster.jpg 和 -thumb.jpg

                                var javHelperFanart = Path.Combine(nfoFileInfo.DirectoryName, $"{nfoFileInfo.Directory.Name}-fanart.jpg");
                                var javHelperThumb = Path.Combine(nfoFileInfo.DirectoryName, $"{nfoFileInfo.Directory.Name}-thumb.jpg");
                                var javHelperPoster = Path.Combine(nfoFileInfo.DirectoryName, $"{nfoFileInfo.Directory.Name}-poster.jpg");

                                // 使用 fanart 替换对应文件
                                if (File.Exists(javHelperFanart))
                                {
                                    File.Copy(saveName, javHelperFanart, true);
                                    File.Copy(saveName, javHelperThumb, true);
                                }

                                // 使用 folderPicture 替换 poster
                                if (File.Exists(javHelperPoster))
                                {
                                    File.Copy(folderPicture, javHelperPoster, true);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"下载封面图异常：---> {ex.Message}");
                }
                // 获取当前目录下的所有 .jpg 文件
                string[] sampleImages = Directory.GetFiles(nfoFileInfo.DirectoryName, "backdrop*.jpg", SearchOption.AllDirectories);
                if (sampleImages.Count() < 2)
                {
                    if (javVideo.Samples != null && javVideo.Samples.Count > 0)
                    {
                        // 下载样品图片
                        for (int i = 0; i < javVideo.Samples.Count; i++)
                        {
                            if (javId.Type == JavIdType.Censored)
                            {
                                if (i > 0)
                                {
                                    var sampleUrl = javVideo.Samples[i];
                                    var fullImageUrl = String.Empty;
                                    if (sampleUrl.Contains("dmm.co.jp"))
                                    {
                                        // 处理缩略图地址，添加 "jp" 前缀
                                        fullImageUrl = sampleUrl.Contains("jp-") ? sampleUrl : Regex.Replace(sampleUrl, @"-", "jp-"); // 只在最后一个 "-" 前添加 "jp"
                                    }
                                    else
                                    {
                                        fullImageUrl = sampleUrl;
                                    }
                                    var fileName = $"backdrop{i}"; // 命名规则
                                    var savePath = Path.Combine(nfoFileInfo.DirectoryName, fileName);
                                    var backdrop = await Downloader.DownloadJpegAsync(fullImageUrl, nfoFileInfo.DirectoryName, fileName);
                                    // 如果第一张图是竖版图片
                                    if (i == 1 && IsVerticalLongImage(backdrop))
                                    {
                                        var folderPicture = Path.Combine(nfoFileInfo.DirectoryName, "folder.jpg");
                                        var posterPicture = Path.Combine(nfoFileInfo.DirectoryName, "poster.jpg");
                                        // 复制竖版图片替换 folder.jpg 和 poster.jpg
                                        if (File.Exists(backdrop))
                                        {
                                            File.Copy(backdrop, folderPicture, true);
                                            File.Copy(backdrop, posterPicture, true);
                                        }
                                        Console.WriteLine("替换默认封面图");
                                    }
                                }
                            }
                            else
                            {
                                var fileName = $"backdrop{i + 1}"; // 命名规则
                                var savePath = Path.Combine(nfoFileInfo.DirectoryName, fileName);
                                await Downloader.DownloadJpegAsync(javVideo.Samples[i], nfoFileInfo.DirectoryName, fileName);
                            }
                        }
                    }
                }
                #endregion

                // 保存元数据
                nfoManager.SaveMetadata(videoTitle, videoOriginalTitle, videoSortTitle, videoPlot, javId.Id, videoActors, videoGenres, videoTags, videoYear, videoDate);

                #region 遍历其他 nfo 文件...
                nfoFiles.Remove(nfoFile);
                if (nfoFiles.Count > 0)
                {
                    foreach (var item in nfoFiles)
                    {
                        File.Copy(nfoFile, item, true);
                    }
                }
                #endregion

                Console.WriteLine($"-----------------修正后的元数据-----------------");
                Console.WriteLine($"fileName -> {nfoFileInfo.Name}");
                Console.WriteLine($"videoId -> {videoId}");
                Console.WriteLine($"videoTitle -> {videoTitle}");
                Console.WriteLine($"videoOriginalTitle -> {videoOriginalTitle}");
                Console.WriteLine($"videoSortTitle -> {videoSortTitle}");
                Console.WriteLine($"tags -> {String.Join(",", videoTags)}");
                Console.WriteLine($"genres -> {String.Join(",", videoGenres)}");
                Console.WriteLine($"==============================================");
            }

        }

        /// <summary>
        /// 将 DMM 番号缩略图 URL 转换为包含横版和竖版高清图的字典
        /// </summary>
        /// <param name="url">缩略图 URL（如 https://pics.dmm.co.jp/mono/movie/adult/xxxx/xxxxps.jpg）</param>
        /// <returns>字典：key 为 "poster" 和 "fanart"，value 为高清图地址</returns>
        public static Dictionary<string, string> GetDMMBigImages(string url)
        {
            var result = new Dictionary<string, string>();
            Console.WriteLine($"原封面图地址 -> {url}");
            // https://pics.dmm.co.jp/mono/movie/adult/xxxx/xxxxps.jpg
            if (url.Contains("pics.dmm.co.jp/mono/movie/adult"))
            {
                // 使用正则匹配番号部分：adult/番号/番号ps.jpg
                var match = Regex.Match(url, @"adult/([a-zA-Z0-9]+)/\1p[sl]\.jpg");
                if (!match.Success)
                {
                    Console.WriteLine("URL 格式不正确，无法提取番号");
                    return result;
                }

                // 提取到的原始番号，如：1fsdss922、meyd992
                string originalId = match.Groups[1].Value;

                // 将番号拆分为字母前缀和数字部分，例如：meyd + 992
                var idMatch = Regex.Match(originalId, @"([a-zA-Z0-9]+?)(\d+)$");
                if (!idMatch.Success)
                {
                    Console.WriteLine("番号格式不正确，无法拆分前缀和数字");
                    return result;
                }

                // 获取字母部分（如 meyd）
                string prefix = idMatch.Groups[1].Value;

                // 获取数字部分并左侧补零到5位（如 992 -> 00992）
                string number = idMatch.Groups[2].Value.PadLeft(5, '0');

                // 构造新番号（如 meyd00992）
                string newId = prefix + number;

                // 构建高清图的基础路径（不含文件名）
                string baseUrl = $"https://awsimgsrc.dmm.co.jp/pics_dig/digital/video/{newId}/";

                // 添加竖版高清图（ps）
                result["poster"] = $"{baseUrl}{newId}ps.jpg";

                // 添加横版高清图（pl）
                result["fanart"] = $"{baseUrl}{newId}pl.jpg";
            }
            else
            {
                // pics.dmm.co.jp 
                // http://pics.dmm.co.jp//digital/video/meyd00992/meyd00992pl.jpg
                // 使用正则匹配番号部分：digital/video/番号/番号ps.jpg
                var match = Regex.Match(url, @"digital/video/([a-zA-Z0-9]+)/\1p[sl]\.jpg");
                if (!match.Success)
                {
                    Console.WriteLine("URL 格式不正确，无法提取番号");
                    return result;
                }

                // 提取到的原始番号，如：1fsdss922、meyd992
                string originalId = match.Groups[1].Value;

                // 将番号拆分为字母前缀和数字部分，例如：meyd + 992
                var idMatch = Regex.Match(originalId, @"([a-zA-Z0-9]+?)(\d+)$");
                if (!idMatch.Success)
                {
                    Console.WriteLine("番号格式不正确，无法拆分前缀和数字");
                    return result;
                }

                // 获取字母部分（如 meyd）
                string prefix = idMatch.Groups[1].Value;

                // 获取数字部分并左侧补零到5位（如 992 -> 00992）
                string number = idMatch.Groups[2].Value.PadLeft(5, '0');

                // 构造新番号（如 meyd00992）
                string newId = prefix + number;
                // 构建高清图的基础路径（不含文件名）
                string baseUrl = $"https://awsimgsrc.dmm.co.jp/pics_dig/digital/video/{newId}/";

                // 添加竖版高清图（ps）
                result["poster"] = $"{baseUrl}{newId}ps.jpg";

                // 添加横版高清图（pl）
                result["fanart"] = $"{baseUrl}{newId}pl.jpg";
            }
            foreach (var item in result)
            {
                Console.WriteLine($"{item.Key} -> {item.Value}");
            }
            // 返回包含两个高清图链接的字典
            return result;
        }
        /// <summary>
        /// 遍历指定目录及子目录，获取所有的 .nfo 文件，并按目录名称分组。
        /// </summary>
        /// <param name="rootDirectory">根目录路径。</param>
        /// <returns>按目录名称分组的 .nfo 文件路径字典。</returns>
        public static Dictionary<string, List<string>> GetNfoFilesGroupedByDirectoryName(string rootDirectory)
        {
            // 用于存储按目录名称分组的 .nfo 文件
            Dictionary<string, List<string>> groupedFiles = new Dictionary<string, List<string>>();

            // 递归获取所有 .nfo 文件
            try
            {
                // 获取当前目录下的所有 .nfo 文件
                string[] files = Directory.GetFiles(rootDirectory, "*.nfo", SearchOption.AllDirectories);

                // 遍历所有找到的 .nfo 文件
                foreach (var file in files)
                {
                    // 获取文件名
                    string fileName = Path.GetFileName(file);

                    // 跳过以 "bak.nfo" 结尾的文件
                    if (fileName.EndsWith("bak.nfo"))
                    {
                        continue; // 跳过该文件
                    }

                    // 获取文件所在的目录名称（而不是完整路径）
                    string directoryName = Path.GetFileName(Path.GetDirectoryName(file));

                    // 如果该目录名称还没有在字典中，先初始化一个新的 List
                    if (!groupedFiles.ContainsKey(directoryName))
                    {
                        groupedFiles[directoryName] = new List<string>();
                    }

                    // 将当前文件路径加入对应目录名称的文件列表
                    groupedFiles[directoryName].Add(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }

            return groupedFiles;
        }

    }
}
