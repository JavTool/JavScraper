using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavScraper.App
{


    public class JavToolConfig
    {

        /// <summary>
        /// 代理类型，0 表示无代理。
        /// </summary>
        public int ProxyType { get; set; }
        /// <summary>
        /// HTTP 代理地址。
        /// </summary>
        public string HttpProxyUrl { get; set; }
        /// <summary>
        /// 端口号，80 表示默认端口。
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 用户名。
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// 密码。
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 窗体是否总在最前面，0 表示否，1 表示是。
        /// </summary>
        public int IsFormAlwaysOnTop { get; set; }
        /// <summary>
        /// 是否下载封面，1 表示是，0 表示否。
        /// </summary>
        public int IsDownloadCover { get; set; }
        /// <summary>
        /// 是否生成 NFO 文件，1 表示是，0 表示否。
        /// </summary>
        public int IsGenNfoFile { get; set; }
        /// <summary>
        /// 是否解析多文件，1 表示是，0 表示否。
        /// </summary>
        public int IsParseMultiFile { get; set; }
        /// <summary>
        /// 是否解析带字幕的文件，1 表示是，0 表示否。
        /// </summary>
        public int IsParseHasSubtitle { get; set; }
        /// <summary>
        /// 若目标文件夹已存在是否移动到失败文件夹，1 表示是，0 表示否。
        /// </summary>
        public int IsMoveToFailFolderAlreadyExist { get; set; }
        /// <summary>
        /// 输入文件夹路径。
        /// </summary>
        public string InputFolder { get; set; }
        /// <summary>
        /// 成功输出文件夹路径。
        /// </summary>
        public string SuccessOutputFolder { get; set; }
        /// <summary>
        /// 失败输出文件夹路径。
        /// </summary>
        public string FailOutputFolder { get; set; }
        /// <summary>
        /// 重命名规则。
        /// </summary>
        public string RenameRule { get; set; }
        /// <summary>
        /// 多文件重命名规则。
        /// </summary>
        public string MultiRenameRule { get; set; }
        /// <summary>
        /// 窗体状态值。
        /// </summary>
        public int FormWindowStateValue { get; set; }
        /// <summary>
        /// 窗体左边距。
        /// </summary>
        public int FormLeft { get; set; }
        /// <summary>
        /// 窗体上边距。
        /// </summary>
        public int FormTop { get; set; }
        /// <summary>
        /// 窗体宽度。
        /// </summary>
        public int FormWidth { get; set; }
        /// <summary>
        /// 窗体高度。
        /// </summary>
        public int FormHeight { get; set; }
        /// <summary>
        /// 文件扩展名。
        /// </summary>
        public string FileExtensionNames { get; set; }
        /// <summary>
        /// 是否使用自定义文件扩展名，1 表示是，0 表示否。
        /// </summary>
        public int IsUseCustomFileExtensionNames { get; set; }
        /// <summary>
        /// 多演员分隔符。
        /// </summary>
        public string MultiActorDelimiter { get; set; }
        /// <summary>
        /// 是否需要通知，1 表示是，0 表示否。
        /// </summary>
        public int IsNeedNotice { get; set; }
        /// <summary>
        /// 偏好的 JavWeb 顺序。
        /// </summary>
        public string PreferJavWebOrder { get; set; }
        /// <summary>
        /// 有字幕时的文本。
        /// </summary>
        public string IsHasSubtitleYesText { get; set; }
        /// <summary>
        /// 无字幕时的文本。
        /// </summary>
        public string IsHasSubtitleNoText { get; set; }
        /// <summary>
        /// 仅显示前五个演员，1 表示是，0 表示否。
        /// </summary>
        public int OnlyShowFirstFiveActor { get; set; }
        /// <summary>
        /// 演员排名文件名。
        /// </summary>
        public string ActorRankFileName { get; set; }
        /// <summary>
        /// 演员信息文件名。
        /// </summary>
        public string ActorInfoFileName { get; set; }
        /// <summary>
        /// 最大标题长度。
        /// </summary>
        public int MaxTitleLen { get; set; }
        /// <summary>
        /// 获取 JavWeb 信息的最大次数。
        /// </summary>
        public int MaxGetJavWebInfo { get; set; }
        /// <summary>
        /// 封面 Fanart 后缀名。
        /// </summary>
        public string CoverFanartPostfixName { get; set; }
        /// <summary>
        /// 封面 Poster 后缀名。
        /// </summary>
        public string CoverPosterPostfixName { get; set; }
        /// <summary>
        /// 封面 Thumb 后缀名。
        /// </summary>
        public string CoverThumbPostfixName { get; set; }
        /// <summary>
        /// 是否生成封面 Fanart，1 表示是，0 表示否。
        /// </summary>
        public int IsGenCoverFanart { get; set; }
        /// <summary>
        /// 是否生成封面 Poster，1 表示是，0 表示否。
        /// </summary>
        public int IsGenCoverPoster { get; set; }
        /// <summary>
        /// 是否生成封面 Thumb，1 表示是，0 表示否。
        /// </summary>
        public int IsGenCoverThumb { get; set; }
        /// <summary>
        /// 是否裁剪封面 Fanart，1 表示是，0 表示否。
        /// </summary>
        public int IsCutCoverFanart { get; set; }
        /// <summary>
        /// 是否裁剪封面 Poster，1 表示是，0 表示否。
        /// </summary>
        public int IsCutCoverPoster { get; set; }
        /// <summary>
        /// 是否裁剪封面 Thumb，1 表示是，0 表示否。
        /// </summary>
        public int IsCutCoverThumb { get; set; }

    }

    public class NfoOptionConfig
    {
        public int NfoOptionActorRuleIsWrite { get; set; }
        public string NfoOptionIsHasSubtitleYesText { get; set; }
        public string NfoOptionTitleRenameRule { get; set; }
        public int NfoOptionMaxTitleLen { get; set; }
    }

}
