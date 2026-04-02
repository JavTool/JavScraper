using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App.Models
{
    /// <summary>
    /// 程序配置类，用于存储和管理应用程序的配置信息。
    /// 包含文件路径、下载选项、重命名规则以及各种界面和 NFO 相关的设置。
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 输入目录路径，用于监控或批处理的来源文件夹。
        /// 但在代码中可能用于不同场景。
        /// 类型：文件夹路径（字符串）。
        /// </summary>
        public string InputFolder { get; set; }

        /// <summary>
        /// 处理成功后输出的目标目录。
        /// 用于保存已成功处理的文件或副本。
        /// 类型：文件夹路径（字符串）。
        /// </summary>
        public string SuccessfulOutputFolder { get; set; }

        /// <summary>
        /// 处理失败时输出的目标目录。
        /// 用于保存处理失败的文件以便人工检查。
        /// 类型：文件夹路径（字符串）。
        /// </summary>
        public string FailedOutputFolder { get; set; }

        /// <summary>
        /// 是否下载封面（Poster/Thumb/Fanart）。
        /// true 表示启用封面下载，false 表示禁用。
        /// </summary>
        public bool DownCover { get; set; }

        /// <summary>
        /// 是否下载画廊（如果适用）。
        /// 画廊通常为多张图片集合，true 表示下载，false 表示不下载。
        /// </summary>
        public bool DownGallery { get; set; }

        /// <summary>
        /// 是否生成 NFO 文件（用于媒体管理器如 Kodi 等）。
        /// true 表示生成，false 表示不生成。
        /// </summary>
        public bool GenerateNFO { get; set; }

        /// <summary>
        /// 封面类型索引（用于 UI 下拉/选择）。
        /// 具体含义由程序 UI/逻辑定义，例如 0=Poster, 1=Fanart 等。
        /// </summary>
        public int CoverTypeIndex { get; set; }

        /// <summary>
        /// 裁剪模式索引（用于控制封面裁剪或缩放策略）。
        /// 具体映射由程序实现决定。
        /// </summary>
        public int CropModeIndex { get; set; }

        /// <summary>
        /// 代表某个滑动条的滚动值（可能用于缩放或进度指示）。
        /// 保留给 UI 状态恢复或设置使用。
        /// </summary>
        public int TrackBarScroll { get; set; }

        /// <summary>
        /// 代表某个滑动条的宽度（像素）。
        /// 可用于保存 UI 控件布局相关的数值。
        /// </summary>
        public int TrackBarWidth { get; set; }

        /// <summary>
        /// 单文件/默认重命名规则模板。
        /// 可包含占位符，例如 %Name%、%ProductNumber%、%Title% 等，程序将在重命名时替换占位符。
        /// </summary>
        public string NamingRule { get; set; }

        /// <summary>
        /// 多文件（合集）重命名规则模板。
        /// 与 <see cref="NamingRule"/> 作用相似，但用于包含多个媒体文件的情况。
        /// </summary>
        public string MultipleNamingRule { get; set; }

        /// <summary>
        /// 代理类型（枚举值，0 表示不使用代理）。
        /// 默认：0
        /// </summary>
        public int ProxyType { get; set; } = 0;

        /// <summary>
        /// HTTP 代理地址（例如 http://127.0.0.1 或带端口的地址）。
        /// 默认：空字符串（不使用）。
        /// </summary>
        public string HttpProxyUrl { get; set; } = string.Empty;

        /// <summary>
        /// 代理端口或默认端口值。
        /// 默认：80
        /// </summary>
        public int Port { get; set; } = 80;

        /// <summary>
        /// 代理或其他需要的用户名（可为空）。
        /// 默认：空字符串
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 代理或其他需要的密码（可为空）。
        /// 默认：空字符串
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 窗口是否总在最前（true 表示总在最前）。
        /// 对应原配置值 IsFormAlwaysOnTop（0/1），此处用 bool 表示。
        /// 默认：false
        /// </summary>
        public bool IsFormAlwaysOnTop { get; set; } = false;

        /// <summary>
        /// 是否下载封面资源（等价于 config 中 IsDownloadCover）。
        /// 默认：true
        /// </summary>
        public bool IsDownloadCover { get; set; } = true;

        /// <summary>
        /// 是否生成 NFO 文件（等价于 config 中 IsGenNfoFile）。
        /// 默认：true
        /// </summary>
        public bool IsGenNfoFile { get; set; } = true;

        /// <summary>
        /// 是否解析多文件（合集）格式（等价于 IsParseMultiFile）。
        /// 默认：true
        /// </summary>
        public bool IsParseMultiFile { get; set; } = true;

        /// <summary>
        /// 是否解析包含 UC 标识的影片（等价于 IsParseHasUC）。
        /// 默认：true
        /// </summary>
        public bool IsParseHasUC { get; set; } = true;

        /// <summary>
        /// 是否解析包含 U 标识的影片（等价于 IsParseHasU）。
        /// 默认：true
        /// </summary>
        public bool IsParseHasU { get; set; } = true;

        /// <summary>
        /// 是否解析是否带有字幕信息（等价于 IsParseHasSubtitle）。
        /// 默认：true
        /// </summary>
        public bool IsParseHasSubtitle { get; set; } = true;

        /// <summary>
        /// 如果目标文件夹已存在，是否将失败的文件移动到失败文件夹（等价于 IsMoveToFailFolderAlreadyExist）。
        /// 默认：true
        /// </summary>
        public bool IsMoveToFailFolderAlreadyExist { get; set; } = true;

        /// <summary>
        /// 重命名规则（主规则），使用占位符进行格式化。
        /// 默认值与 config.json 保持一致。
        /// </summary>
        public string RenameRule { get; set; } = "%Name%\\%HasSubtitle%[%ReleaseDate%] - [%ProductNumber%] - [%Title%]\\%HasSubtitle%[%ReleaseDate%] - [%ProductNumber%] - [%Title%]";

        /// <summary>
        /// 多文件重命名规则（合集），使用占位符进行格式化。
        /// 默认值与 config.json 保持一致。
        /// </summary>
        public string MultiRenameRule { get; set; } = "%Name%\\%HasSubtitle%[%ReleaseDate%] - [%ProductNumber%] - [%Title%]\\%HasSubtitle%[%ReleaseDate%] - [%ProductNumber%] - [%Title%]";

        /// <summary>
        /// 窗口状态的整型表示（保存窗口最小化/最大化/正常等状态）。
        /// 默认：0
        /// </summary>
        public int FormWindowStateValue { get; set; } = 0;

        /// <summary>
        /// 窗口左边距（像素），用于窗口位置恢复。
        /// 默认：218
        /// </summary>
        public int FormLeft { get; set; } = 218;

        /// <summary>
        /// 窗口上边距（像素），用于窗口位置恢复。
        /// 默认：96
        /// </summary>
        public int FormTop { get; set; } = 96;

        /// <summary>
        /// 窗口宽度（像素），用于窗口大小恢复。
        /// 默认：1118
        /// </summary>
        public int FormWidth { get; set; } = 1118;

        /// <summary>
        /// 窗口高度（像素），用于窗口大小恢复。
        /// 默认：695
        /// </summary>
        public int FormHeight { get; set; } = 695;

        /// <summary>
        /// 支持的文件扩展名列表，使用分号分隔。
        /// 默认包含常见视频格式，例如 .mp4 .mkv 等。
        /// </summary>
        public string FileExtensionNames { get; set; } = ".avi;.wmv;.asf;.mpg;.mpeg;.dat;.vob;.ogm;.mp4;.3gp;.mkv;.rm;.rmvb;.flv;.swf;.mov;.m4v";

        /// <summary>
        /// 是否使用自定义扩展名列表（如果为 false，将使用内置默认列表）。
        /// 默认：false
        /// </summary>
        public bool IsUseCustomFileExtensionNames { get; set; } = false;

        /// <summary>
        /// 多演员分隔符（用于解析演员字符串），默认是空格。
        /// </summary>
        public string MultiActorDelimiter { get; set; } = " ";

        /// <summary>
        /// 是否需要通知（例如处理完成的桌面或系统通知）。
        /// 默认：false
        /// </summary>
        public bool IsNeedNotice { get; set; } = false;

        /// <summary>
        /// 首选爬取网站顺序，以逗号分隔（例如 "JavBusWeb,JapanArzonWeb,JavDBWeb"）。
        /// 程序在爬取时会按序尝试这些来源。
        /// </summary>
        public string PreferJavWebOrder { get; set; } = "JavBusWeb,JapanArzonWeb,JavDBWeb";

        /// <summary>
        /// 表示带有 UC（无码破解）标签时用于显示/写入的文本。
        /// </summary>
        public string IsHasUCText { get; set; } = "<[中字无码破解] - >";

        /// <summary>
        /// 表示带有 U（无码）标签时用于显示/写入的文本。
        /// </summary>
        public string IsHasUText { get; set; } = "<[无码破解] - >";

        /// <summary>
        /// 表示带有字幕时用于显示/写入的文本（是的情况）。
        /// </summary>
        public string IsHasSubtitleYesText { get; set; } = "<[中字] - >";

        /// <summary>
        /// 表示不带字幕时用于显示/写入的文本（否的情况）。
        /// </summary>
        public string IsHasSubtitleNoText { get; set; } = string.Empty;

        /// <summary>
        /// 是否仅显示前五个演员（在 UI 或生成的文本中）。
        /// 默认：true
        /// </summary>
        public bool OnlyShowFirstFiveActor { get; set; } = true;

        /// <summary>
        /// 演员排行榜文件名（用于本地缓存或排序数据），默认：ActorRank.txt
        /// </summary>
        public string ActorRankFileName { get; set; } = "ActorRank.txt";

        /// <summary>
        /// 演员信息文件名（用于本地缓存或扩展信息），默认：ActorInfo.txt
        /// </summary>
        public string ActorInfoFileName { get; set; } = "ActorInfo.txt";

        /// <summary>
        /// 标题最大长度限制（字符数），超过时可截断或忽略。
        /// 默认：150
        /// </summary>
        public int MaxTitleLen { get; set; } = 150;

        /// <summary>
        /// 从每个 Jav 网站最多获取的信息数量（并行或重试次数相关）。
        /// 默认：2
        /// </summary>
        public int MaxGetJavWebInfo { get; set; } = 2;

        /// <summary>
        /// 封面 fanart 文件后缀名（用于生成文件名），默认：-fanart.jpg
        /// </summary>
        public string CoverFanartPostfixName { get; set; } = "-fanart.jpg";

        /// <summary>
        /// 封面 poster 文件后缀名（用于生成文件名），默认：-poster.jpg
        /// </summary>
        public string CoverPosterPostfixName { get; set; } = "-poster.jpg";

        /// <summary>
        /// 封面 thumb 文件后缀名（用于生成文件名），默认：-thumb.jpg
        /// </summary>
        public string CoverThumbPostfixName { get; set; } = "-thumb.jpg";

        /// <summary>
        /// 是否生成 fanart（用于媒体管理器），默认：true
        /// </summary>
        public bool IsGenCoverFanart { get; set; } = true;

        /// <summary>
        /// 是否生成 poster，默认：true
        /// </summary>
        public bool IsGenCoverPoster { get; set; } = true;

        /// <summary>
        /// 是否生成 thumb，默认：true
        /// </summary>
        public bool IsGenCoverThumb { get; set; } = true;

        /// <summary>
        /// 是否裁剪 fanart（以匹配特定尺寸或比例），默认：true
        /// </summary>
        public bool IsCutCoverFanart { get; set; } = true;

        /// <summary>
        /// 是否裁剪 poster，默认：true
        /// </summary>
        public bool IsCutCoverPoster { get; set; } = true;

        /// <summary>
        /// 是否裁剪 thumb，默认：true
        /// </summary>
        public bool IsCutCoverThumb { get; set; } = true;

        /// <summary>
        /// 是否在片号中添加 "-C"（或类似后缀），默认：false
        /// </summary>
        public bool IsAddDashCToProductNumber { get; set; } = false;

        /// <summary>
        /// NFO 写入时是否写入演员规则（Actor Rule）。
        /// 默认：true
        /// </summary>
        public bool NfoOptionActorRuleIsWrite { get; set; } = true;

        /// <summary>
        /// NFO 中用于表示 "有 UC（中字无码破解）" 的文本标记。
        /// 默认："< [中字无码破解] >"
        /// </summary>
        public string NfoOptionIsHasUCText { get; set; } = "< [中字无码破解] >";

        /// <summary>
        /// NFO 中用于表示 "有 U（无码破解）" 的文本标记。
        /// 默认："< [无码破解] >"
        /// </summary>
        public string NfoOptionIsHasUText { get; set; } = "< [无码破解] >";

        /// <summary>
        /// NFO 中用于表示 "有字幕"（是）的文本标记。
        /// 默认："< [中字] >"
        /// </summary>
        public string NfoOptionIsHasSubtitleYesText { get; set; } = "< [中字] >";

        /// <summary>
        /// NFO 中用于生成标题的重命名模板（仅 NFO 使用）。
        /// 默认："[%ProductNumber%]%HasSubtitle%%Title%"
        /// </summary>
        public string NfoOptionTitleRenameRule { get; set; } = "[%ProductNumber%]%HasSubtitle%%Title%";

        /// <summary>
        /// NFO 中允许的最大标题长度（字符数），默认：150
        /// </summary>
        public int NfoOptionMaxTitleLen { get; set; } = 150;
    }
}
